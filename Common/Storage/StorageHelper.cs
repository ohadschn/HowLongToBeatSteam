using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Util;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Storage
{
    public static class StorageHelper
    {
        public const string PartitionKey = "PartitionKey";
        public const string RowKey = "RowKey";
        public const string Timestamp = "Timestamp";
        private const int MaxBatchOperations = 100;

        public const string SlabLogsTableName = "SLABLogsTable";
        public static readonly string JobDataBlobContainerName = SiteUtil.GetOptionalValueFromConfig("JobDataBlobContainerName", "jobdata");
        public static readonly string SteamToHltbTableName = SiteUtil.GetOptionalValueFromConfig("SteamToHltbTableName", "steamToHltb");
        public static readonly string GenreStatsTableName = SiteUtil.GetOptionalValueFromConfig("GenreStatsTableName", "genreStats");

        public static readonly string AzureStorageTablesConnectionString =
            SiteUtil.GetMandatoryCustomConnectionStringFromConfig(SiteUtil.GetOptionalValueFromConfig("TablesConnectionStringKey", "HltbsTables"));
        public static readonly string AzureStorageBlobConnectionString = 
            SiteUtil.GetMandatoryCustomConnectionStringFromConfig(SiteUtil.GetOptionalValueFromConfig("BlobsConnectionStringKey", "HltbsBlobs"));

        private static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromSeconds(4);

        public static readonly DateTime MinEdmDate = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime MaxEdmDate = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        public static bool IsValid(DateTime dateTime)
        {
            return dateTime >= MinEdmDate && dateTime <= MaxEdmDate;
        }

        //https://msdn.microsoft.com/en-us/library/azure/dd179338.aspx
        public static string CleanStringForTableKey(string text)
        {
            var disallowedChars = new HashSet<char>(new[] { '/', '\\', '#', '?' });

            for (char i = (char)0; i <= 0x1F; i++)
            {
                disallowedChars.Add(i);
            }

            for (char i = (char)0x7F; i <= 0x9F; i++)
            {
                disallowedChars.Add(i);
            }

            return SiteUtil.CleanString(text, disallowedChars);
        }

        public static string GetAppSummary([NotNull] IEnumerable<AppEntity> apps)
        {
            if (apps == null) throw new ArgumentNullException(nameof(apps));

            return String.Join(Environment.NewLine,
                apps.Select(ae => String.Format(CultureInfo.InvariantCulture, "{0} ({1})", ae.SteamName, ae.SteamAppId)));
        }

        public static Task<ConcurrentBag<AppEntity>> GetAllApps(string rowFilter = null, int retries = -1)
        {
            return GetAllSteamToHltbEntities<AppEntity>(rowFilter ?? AppEntity.AppEntityFilter, AppEntity.GetPartitions(), retries);
        }

        public static Task<ConcurrentBag<SuggestionEntity>> GetAllSuggestions(string rowFilter = null, int retries = -1)
        {
            return GetAllSteamToHltbEntities<SuggestionEntity>(rowFilter ?? SuggestionEntity.SuggestionFilter, SuggestionEntity.GetPartitions(), retries);
        }

        public static Task<ConcurrentBag<ProcessedSuggestionEntity>> GetAllProcessedSuggestions(string rowFilter = null, int retries = -1)
        {
            return GetAllSteamToHltbEntities<ProcessedSuggestionEntity>(
                rowFilter ?? ProcessedSuggestionEntity.ProcessedSuggestionFilter, ProcessedSuggestionEntity.GetPartitions(), retries);
        }

        private static async Task<ConcurrentBag<T>> GetAllSteamToHltbEntities<T>(string rowFilter, ICollection<string> partitions, int retries = -1) where T : ITableEntity, new()
        {
            CommonEventSource.Log.QueryAllEntitiesStart(typeof(T).FullName, rowFilter);
            var entities = await QueryAllTableEntities<T>(SteamToHltbTableName, partitions, rowFilter, retries).ConfigureAwait(false);
            CommonEventSource.Log.QueryAllEntitiesStop(typeof(T).FullName, rowFilter, entities.Count);

            return entities;
        }

        public static async Task<ConcurrentBag<GenreStatsEntity>> GetAllGenreStats(string rowFilter = "", int retries = -1)
        {
            const string entitiesType = "genre stats";

            CommonEventSource.Log.QueryAllEntitiesStart(entitiesType, rowFilter);
            
            var genreStats = await QueryAllTableEntities<GenreStatsEntity>(GenreStatsTableName, GenreStatsEntity.GetPartitions(), rowFilter, retries)
                .ConfigureAwait(false);

            CommonEventSource.Log.QueryAllEntitiesStop(entitiesType, rowFilter, genreStats.Count);

            return genreStats;
        }

        public static Task<int> DeleteOldEntities(
            [NotNull] string tableName, DateTime threshold, [NotNull] string description, int retries = -1)
        {
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return DeleteOldEntitiesInternal(tableName, threshold, description, retries);
        }

        private static async Task<int> DeleteOldEntitiesInternal(
            [NotNull] string tableName, DateTime threshold, [NotNull] string description, int retries)
        {
            CommonEventSource.Log.DeleteOldEntitiesStart(description);

            var table = await GetTable(tableName, retries).ConfigureAwait(false);
            var query = new TableQuery<TableEntity>().Where(TimestampFilter(threshold));
            int deleteCount = 0;

            await ProcessAllSegments(table, query,
                batch => CommonEventSource.Log.RetrieveOldLogEntriesStart(description, batch),
                batch => CommonEventSource.Log.RetrieveOldLogEntriesStop(description, batch),
                segment =>
                {
                    Interlocked.Add(ref deleteCount, segment.Results.Count);
                    return Delete(segment, "Deleting old entities of type " + description, tableName, retries);
                })
                .ConfigureAwait(false);

            CommonEventSource.Log.DeleteOldEntitiesStop(description);

            return deleteCount;
        }

        private static async Task<ConcurrentBag<T>> QueryAllTableEntities<T>(
            string tableName, ICollection<string> partitionKeys, string rowFilter = "", int retries = -1)
            where T : ITableEntity, new()
        {
            var allEntities = new ConcurrentBag<T>();

            var table = await GetTable(tableName, retries).ConfigureAwait(false);

            await partitionKeys.ForEachAsync(partitionKeys.Count, async partition =>
            {
                var partitionFilter = 
                    TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partition.ToString(CultureInfo.InvariantCulture));

                var filter = String.IsNullOrWhiteSpace(rowFilter)
                    ? partitionFilter
                    : TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

                var query = new TableQuery<T>().Where(filter);

                await ProcessAllSegments(table, query,
                    batch => CommonEventSource.Log.RetrievePartitionBatchMappingsStart(partition, batch),
                    batch => CommonEventSource.Log.RetrievePartitionBatchMappingsStop(partition, batch),
                    segment => allEntities.AddRange(segment)).ConfigureAwait(false);

            }).ConfigureAwait(false);

            return allEntities;
        }

        private static Task ProcessAllSegments<T>(CloudTable table, TableQuery<T> query,
            Action<int> segmentStartLogger, Action<int> segmentStopLogger,
            Action<TableQuerySegment<T>> segmentProcessor)
            where T : ITableEntity, new()
        {
            return ProcessAllSegments(table, query, segmentStartLogger, segmentStopLogger, segment =>
            {
                segmentProcessor(segment);
                return Task.FromResult(0);
            });
        }

        private static async Task ProcessAllSegments<T>(CloudTable table, TableQuery<T> query, 
            Action<int> segmentStartLogger, Action<int> segmentStopLogger, Func<TableQuerySegment<T>, Task> segmentProcessor)
            where T : ITableEntity, new()
        {
            int batch = 1;
            TableQuerySegment<T> currentSegment = null;
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                segmentStartLogger(batch);
                currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment?.ContinuationToken)
                        .ConfigureAwait(false);
                segmentStopLogger(batch);
                
                await segmentProcessor(currentSegment).ConfigureAwait(false);
                batch++;
            }
        }

        public static Task Replace<T>([NotNull] IEnumerable<T> entities, [NotNull] string description, string tableName = null, int retries = -1) 
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return ExecuteOperations(entities, e => new[] { TableOperation.Replace(e) }, tableName ?? SteamToHltbTableName, description, retries);
        }

        public static Task Insert<T>([NotNull] IEnumerable<T> entities, [NotNull] string description, string tableName = null, int retries = -1) 
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return ExecuteOperations(entities, e => new[] { TableOperation.Insert(e) }, tableName ?? SteamToHltbTableName, description, retries);
        }

        public static Task InsertOrReplace<T>([NotNull] IEnumerable<T> entities, [NotNull] string description, string tableName = null, int retries = -1) 
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return ExecuteOperations(entities, e => new[] { TableOperation.InsertOrReplace(e) }, tableName ?? SteamToHltbTableName, description, retries);
        }

        public static Task Delete<T>([NotNull] IEnumerable<T> entities, [NotNull] string description, [NotNull] string tableName, int retries = -1)
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));

            return ExecuteOperations(entities, e => new[] { TableOperation.Delete(e) }, tableName, description, retries);
        }

        public static Task ExecuteOperations<T>(
            [NotNull] IEnumerable<T> entities, 
            [NotNull] Func<T, TableOperation[]> operationGenerator, 
            [NotNull] string tableName, 
            [NotNull] string description, 
            int retries = -1)
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (operationGenerator == null) throw new ArgumentNullException(nameof(operationGenerator));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return ExecuteOperationsInternal(entities, operationGenerator, tableName, description, retries);
        }

        private static async Task ExecuteOperationsInternal<T>(
            [NotNull] IEnumerable<T> entities,
            [NotNull] Func<T, TableOperation[]> operationGenerator,
            [NotNull] string tableName,
            [NotNull] string description,
            int retries)
            where T : ITableEntity
        {
            CommonEventSource.Log.ExecuteOperationsStart(description);
            var table = await GetTable(tableName, retries).ConfigureAwait(false);

            await SplitToBatchOperations(entities, operationGenerator).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async tboi =>
            {
                var final = tboi.Final ? "(final)" : String.Empty;
                CommonEventSource.Log.ExecutePartitionBatchOperationStart(tboi.Partition, tboi.Batch, final);
                await ExecuteBatchCore(table, tboi.Operation);
                CommonEventSource.Log.ExecutePartitionBatchOperationStop(tboi.Partition, tboi.Batch, final);
            }, false).ConfigureAwait(false);
            CommonEventSource.Log.ExecuteOperationsStop(description);
        }

        public static Task ExecuteBatchOperation(
            [NotNull] IEnumerable<TableOperation> operations,
            [NotNull] string tableName,
            [NotNull] string description,
            int retries = -1)
        {
            if (operations == null) throw new ArgumentNullException(nameof(operations));
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));
            if (description == null) throw new ArgumentNullException(nameof(description));

            return ExecuteBatchOperationInternal(operations, tableName, description, retries);
        }

        private static async Task ExecuteBatchOperationInternal(
            [NotNull] IEnumerable<TableOperation> operations,
            [NotNull] string tableName,
            [NotNull] string description,
            int retries)
        {
            CommonEventSource.Log.ExecuteBatchOperationStart(description);

            var batchOperation = new TableBatchOperation();
            foreach (var operation in operations)
            {
                batchOperation.Add(operation);
            }
            var table = await GetTable(tableName, retries).ConfigureAwait(false);
            await ExecuteBatchCore(table, batchOperation);

            CommonEventSource.Log.ExecuteBatchOperationStop(description);
        }

        private static async Task ExecuteBatchCore(CloudTable table, TableBatchOperation tableBatchOperation)
        {
            try
            {
                await table.ExecuteBatchAsync(tableBatchOperation).ConfigureAwait(false);
            }
            catch (StorageException e)
            {
                var batchContents = String.Join(Environment.NewLine,
                    tableBatchOperation.Select((o, i) => String.Format(CultureInfo.InvariantCulture,
                        "[{0}] Type: {1} Partition: {2} Row: {3}", i, o.OperationType, o.GetPartitionKey(), o.GetRowKey())));

                CommonEventSource.Log.ErrorExecutingBatchOperation(
                    e,
                    e.RequestInformation?.HttpStatusCode ?? 0,
                    e.RequestInformation?.ExtendedErrorInformation?.ErrorCode ?? "unknown",
                    e.RequestInformation?.ExtendedErrorInformation?.ErrorMessage ?? "unknown",
                    batchContents);

                throw new StorageException(String.Format(CultureInfo.InvariantCulture,
                    "Error executing batch operation: {0}. Status code: {1}. Batch contents: {2}",
                    e.RequestInformation?.ExtendedErrorInformation?.ErrorMessage ?? "unknown", e.RequestInformation?.HttpStatusCode, batchContents), e);
            }
        }

        public static Task InsertSuggestion(SuggestionEntity suggestion, int retries = -1)
        {
            if (suggestion == null) throw new ArgumentNullException(nameof(suggestion));

            return InsertSuggestionInternal(suggestion, retries);
        }

        private static async Task InsertSuggestionInternal(SuggestionEntity suggestion, int retries)
        {
            var table = await GetTable(SteamToHltbTableName, retries).ConfigureAwait(false);

            CommonEventSource.Log.InsertSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.InsertSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }


        public static Task DeleteSuggestion([NotNull] SuggestionEntity suggestion, AppEntity app = null, int retries = -1)
        {
            if (suggestion == null) throw new ArgumentNullException(nameof(suggestion));

            return DeleteSuggestionInternal(suggestion, app, retries);
        }

        private static async Task DeleteSuggestionInternal([NotNull] SuggestionEntity suggestion, AppEntity app, int retries)
        {
            var batchOperation = new TableBatchOperation
            {
                TableOperation.Delete(suggestion),
                TableOperation.InsertOrReplace(new ProcessedSuggestionEntity(suggestion))
            };

            if (app != null) //this means we want to update the app to be a verified game and stop future non-game suggestions
            {
                app.VerifiedGame = true;
                batchOperation.Replace(app);
            }

            var table = await GetTable(SteamToHltbTableName, retries).ConfigureAwait(false);

            CommonEventSource.Log.DeleteSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
            CommonEventSource.Log.DeleteSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }


        //assumes app has been already modified to contain the updated HLTB info
        public static Task AcceptSuggestion([NotNull] AppEntity app, [NotNull] SuggestionEntity suggestion, int retries = -1)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (suggestion == null) throw new ArgumentNullException(nameof(suggestion));

            return AcceptSuggestionInternal(app, suggestion, retries);
        }

        private static async Task AcceptSuggestionInternal([NotNull] AppEntity app, [NotNull] SuggestionEntity suggestion, int retries)
        {
            var batchOperation = new TableBatchOperation
            {
                TableOperation.Delete(suggestion),
                TableOperation.InsertOrReplace(new ProcessedSuggestionEntity(suggestion))
            };

            if (suggestion.IsRetype)
            {
                batchOperation.Delete(app);
                batchOperation.Insert(new AppEntity(app.SteamAppId, app.SteamName, suggestion.AppType));
            }
            else
            {
                batchOperation.Replace(app);
            }

            var table = await GetTable(SteamToHltbTableName, retries).ConfigureAwait(false);

            CommonEventSource.Log.AcceptSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
            CommonEventSource.Log.AcceptSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }


        public static Task UpdateProcessedSuggestions([NotNull] ProcessedSuggestionEntity processedSuggestion, int retries = -1)
        {
            if (processedSuggestion == null) throw new ArgumentNullException(nameof(processedSuggestion));

            return UpdateProcessedSuggestionsInternal(processedSuggestion, retries);
        }

        private static async Task UpdateProcessedSuggestionsInternal([NotNull] ProcessedSuggestionEntity processedSuggestion, int retries)
        {
            var table = await GetTable(SteamToHltbTableName, retries).ConfigureAwait(false);

            CommonEventSource.Log.UpdateProcessedSuggestionStart(processedSuggestion.SteamAppId, processedSuggestion.HltbId);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(processedSuggestion));
            CommonEventSource.Log.UpdateProcessedSuggestionStop(processedSuggestion.SteamAppId, processedSuggestion.HltbId);
        }

        private static IEnumerable<TableBatchOperationInfo> SplitToBatchOperations<T>(
            IEnumerable<T> entities, Func<T, TableOperation[]> operationGenerator)
            where T: ITableEntity
        {
            var allOperations = new Dictionary<string, List<TableBatchOperationInfo>>();
            foreach (var appGroup in entities.GroupBy(ae => ae.PartitionKey))
            {
                string partition = appGroup.Key;
                int batch = 1;
                var batchOperation = new TableBatchOperation();
                foreach (var appEntity in appGroup)
                {
                    var operations = operationGenerator(appEntity);
                    if (operations.Length > MaxBatchOperations)
                    {
                        throw new ArgumentOutOfRangeException(nameof(operationGenerator),
                            String.Format(CultureInfo.InvariantCulture, "The operationGenerator func must return at most {0} operations", MaxBatchOperations));
                    }

                    if (batchOperation.Count + operations.Length > MaxBatchOperations)
                    {
                        allOperations.GetOrCreate(partition).Add(new TableBatchOperationInfo(partition, batch++, false, batchOperation));
                        batchOperation = new TableBatchOperation();
                    }

                    foreach (var operation in operations)
                    {
                        batchOperation.Add(operation);
                    }
                }

                if (batchOperation.Count != 0)
                {
                    allOperations.GetOrCreate(partition).Add(new TableBatchOperationInfo(partition, batch, true, batchOperation));
                }
            }
            return allOperations.Values.Interleave();
        }

        public static string StartsWithFilter([NotNull] string propertyName, [NotNull] string value)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, value),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, IncrementLastChar(value)));
        }

        public static string DoesNotStartWithFilter([NotNull] string propertyName, [NotNull] string value)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, value),
                TableOperators.Or,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, IncrementLastChar(value)));
        }

        public static string OrFilter(string filter1, string filter2)
        {
            return TableQuery.CombineFilters(filter1, TableOperators.Or, filter2);
        }

        public static string AndFilter(string filter1, string filter2)
        {
            return TableQuery.CombineFilters(filter1, TableOperators.And, filter2);
        }

        private static string TimestampFilter(DateTime latest)
        {
            return TableQuery.GenerateFilterConditionForDate(Timestamp, QueryComparisons.LessThan, latest);
        }

        private static CloudTableClient GetCloudTableClient(int retries = -1)
        {
            var cloudTableClient = CloudStorageAccount.Parse(AzureStorageTablesConnectionString).CreateCloudTableClient();
            if (retries >= 0)
            {
                cloudTableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(DefaultDeltaBackoff, retries);
            }
            return cloudTableClient;
        }

        private static async Task<CloudTable> GetTable(string tableName, int retries)
        {
            var table = GetCloudTableClient(retries).GetTableReference(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        public static async Task<int> DeleteOldBlobs(string containerName, DateTime threshold, int retries = -1)
        {
            int blobsDeleted = 0;

            await ProcessContainerBlobs(containerName, blobs =>
                blobs.OfType<ICloudBlob>().ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async blob =>
                {
                    await blob.FetchAttributesAsync().ConfigureAwait(false);
                    if (blob.Properties.LastModified < threshold)
                    {
                        await blob.DeleteAsync().ConfigureAwait(false);
                        Interlocked.Increment(ref blobsDeleted);
                    }
                }), "deleting old blobs", retries).ConfigureAwait(false);

            return blobsDeleted;
        }

        public static async Task ProcessContainerBlobs(string containerName, Func<IEnumerable<IListBlobItem>, Task> processor, string description, int retries = -1)
        {
            CommonEventSource.Log.ProcessContainerBlobsStart(description);

            var container = GetCloudBlobClient(retries).GetContainerReference(containerName);

            int blobCount = 0;
            int batch = 0;
            BlobContinuationToken continuationToken = null;
            do
            {
                batch++;

                CommonEventSource.Log.RetrieveContainerBlobBatchStart(description, batch);
                var listingResult = await container.ListBlobsSegmentedAsync(continuationToken).ConfigureAwait(false);
                CommonEventSource.Log.RetrieveContainerBlobBatchStop(description, batch);

                continuationToken = listingResult.ContinuationToken;
                var results = listingResult.Results.ToArray();
                blobCount += results.Length;

                CommonEventSource.Log.ProcessContainerBlobBatchStart(description, batch, results.Length);
                await processor(results).ConfigureAwait(false);
                CommonEventSource.Log.ProcessContainerBlobBatchStop(description, batch, results.Length);
            }
            while (continuationToken != null);

            CommonEventSource.Log.ProcessContainerBlobsStop(description, blobCount);
        }

        public static CloudBlobClient GetCloudBlobClient(int retries = -1)
        {
            var cloudBlobClient = CloudStorageAccount.Parse(AzureStorageBlobConnectionString).CreateCloudBlobClient();
            if (retries >= 0)
            {
                cloudBlobClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(DefaultDeltaBackoff, retries);
            }
            return cloudBlobClient;
        }

        private static string IncrementLastChar(string str)
        {
            char lastChar = str[str.Length-1];
            return lastChar == Char.MaxValue
                ? str + (char) 0
                : str.Remove(str.Length - 1, 1) + (char) (lastChar + 1);
        }

        private class TableBatchOperationInfo
        {
            public string Partition { get; }
            public int Batch { get; }
            public bool Final { get; }
            public TableBatchOperation Operation { get; }

            public TableBatchOperationInfo(string partition, int batch, bool final, TableBatchOperation operation)
            {
                Partition = partition;
                Batch = batch;
                Final = final;
                Operation = operation;
            }
        }
    }
}