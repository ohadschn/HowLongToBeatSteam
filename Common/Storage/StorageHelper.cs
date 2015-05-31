using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        private const int MaxBatchOperations = 100;

        public static readonly string SteamToHltbTableName = SiteUtil.GetOptionalValueFromConfig("SteamToHltbTableName", "steamToHltb");
        public static readonly string GenreStatsTableName = SiteUtil.GetOptionalValueFromConfig("GenreStatsTableName", "genreStats");
        public static readonly string AzureStorageTablesConnectionString = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("HltbsTables");
        public static readonly string AzureStorageBlobConnectionString = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("HltbsBlobs");        
        private static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromSeconds(4);

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
            if (apps == null) throw new ArgumentNullException("apps");

            return String.Join(Environment.NewLine,
                apps.Select(ae => String.Format(CultureInfo.InvariantCulture, "{0} ({1})", ae.SteamName, ae.SteamAppId)));
        }

        public static async Task<ConcurrentBag<AppEntity>> GetAllApps(string rowFilter = null, int retries = -1)
        {
            const string entitiesType = "apps";
            rowFilter = rowFilter ?? SuggestionEntity.NonSuggestionFilter;

            CommonEventSource.Log.QueryAllEntitiesStart(entitiesType, rowFilter);
            var knownSteamIds = await QueryAllTableEntities<AppEntity>(SteamToHltbTableName, AppEntity.GetPartitions(), rowFilter, retries)
                .ConfigureAwait(false);
            CommonEventSource.Log.QueryAllEntitiesStop(entitiesType, rowFilter, knownSteamIds.Count);
            
            return knownSteamIds;
        }

        public static async Task<ConcurrentBag<SuggestionEntity>> GetAllSuggestions(string rowFilter = null, int retries = -1)
        {
            const string entitiesType = "suggestions";
            rowFilter = rowFilter ?? SuggestionEntity.SuggestionFilter;

            CommonEventSource.Log.QueryAllEntitiesStart(entitiesType, rowFilter);
            var suggestions = await QueryAllTableEntities<SuggestionEntity>(SteamToHltbTableName, SuggestionEntity.GetPartitions(), rowFilter, retries)
                .ConfigureAwait(false);
            CommonEventSource.Log.QueryAllEntitiesStop(entitiesType, rowFilter, suggestions.Count);

            return suggestions;
        }

        public static async Task<ConcurrentBag<GenreStatsEntity>> GetAllGenreStats(string rowFilter = "", int retries = -1)
        {
            const string entitiesType = "genre stats";

            CommonEventSource.Log.QueryAllEntitiesStart(entitiesType, rowFilter);
            var genreStats = await QueryAllTableEntities<GenreStatsEntity>(GenreStatsTableName, GenreStatsEntity.GetPartitions(), rowFilter, retries);
            CommonEventSource.Log.QueryAllEntitiesStop(entitiesType, rowFilter, genreStats.Count);

            return genreStats;
        }

        private static async Task<ConcurrentBag<T>> QueryAllTableEntities<T>(
            string tableName, ICollection<string> partitionKeys, string rowFilter = "", int retries = -1)
            where T : ITableEntity, new()
        {
            var allEntities = new ConcurrentBag<T>();

            var table = await GetTable(tableName, retries);

            await partitionKeys.ForEachAsync(partitionKeys.Count, async partition =>
            {
                var partitionFilter = 
                    TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, partition.ToString(CultureInfo.InvariantCulture));

                var filter = String.IsNullOrWhiteSpace(rowFilter)
                    ? partitionFilter
                    : TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

                var query = new TableQuery<T>().Where(filter);

                TableQuerySegment<T> currentSegment = null;
                int batch = 1;
                while (currentSegment == null || currentSegment.ContinuationToken != null)
                {
                    CommonEventSource.Log.RetrievePartitionBatchMappingsStart(partition, batch);
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null).ConfigureAwait(false);
                    CommonEventSource.Log.RetrievePartitionBatchMappingsStop(partition, batch);

                    CommonEventSource.Log.ProcessPartitionBatchStart(partition, batch);
                    allEntities.AddRange(currentSegment);
                    CommonEventSource.Log.ProcessPartitionBatchStop(partition, batch);

                    batch++;
                }
            }).ConfigureAwait(false);

            return allEntities;
        }

        public static Task Replace<T>([NotNull] IEnumerable<T> entities, int retries = -1, string tableName = null) where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException("entities");

            return ExecuteOperations(entities, e => new[] { TableOperation.Replace(e) }, tableName ?? SteamToHltbTableName, retries);
        }

        public static Task Insert<T>([NotNull] IEnumerable<T> entities, int retries = -1, string tableName = null) where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException("entities");

            return ExecuteOperations(entities, e => new[] { TableOperation.Insert(e) }, tableName ?? SteamToHltbTableName, retries);
        }

        public static Task InsertOrReplace<T>([NotNull] IEnumerable<T> entities, int retries = -1, string tableName = null) where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException("entities");

            return ExecuteOperations(entities, e => new[] { TableOperation.InsertOrReplace(e) }, tableName ?? SteamToHltbTableName, retries);
        }

        public static async Task ExecuteOperations<T>(
            [NotNull] IEnumerable<T> entities,
            [NotNull] Func<T, TableOperation[]> operationGenerator, 
            [NotNull] string tableName, 
            int retries = -1)
            where T : ITableEntity
        {
            if (entities == null) throw new ArgumentNullException("entities");
            if (operationGenerator == null) throw new ArgumentNullException("operationGenerator");
            if (tableName == null) throw new ArgumentNullException("tableName");

            CommonEventSource.Log.ExecuteOperationsStart();
            var table = await GetTable(tableName, retries);

            await SplitToBatchOperations(entities, operationGenerator).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async tboi =>
            {
                var final = tboi.Final ? "(final)" : String.Empty;

                CommonEventSource.Log.ExecutePartitionBatchOperationStart(tboi.Partition, tboi.Batch, final);
                try
                {
                    await table.ExecuteBatchAsync(tboi.Operation).ConfigureAwait(false);
                }
                catch (StorageException e)
                {
                    CommonEventSource.Log.ErrorExecutingPartitionBatchOperation(
                        e, 
                        e.RequestInformation.HttpStatusCode, 
                        e.RequestInformation.ExtendedErrorInformation.ErrorCode,
                        e.RequestInformation.ExtendedErrorInformation.ErrorMessage, 
                        tboi.Operation);
                    throw;
                }
                CommonEventSource.Log.ExecutePartitionBatchOperationStop(tboi.Partition, tboi.Batch, final);

            }, false).ConfigureAwait(false);
            CommonEventSource.Log.ExecuteOperationsStop();
        }

        public static async Task InsertSuggestion(SuggestionEntity suggestion, int retries = -1)
        {
            if (suggestion == null) throw new ArgumentNullException("suggestion");

            var table = await GetTable(SteamToHltbTableName, retries);

            CommonEventSource.Log.InsertSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.InsertSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        public static async Task DeleteSuggestion([NotNull] SuggestionEntity suggestion, int retries = -1)
        {
            if (suggestion == null) throw new ArgumentNullException("suggestion");

            var table = await GetTable(SteamToHltbTableName, retries);

            CommonEventSource.Log.DeleteSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.Delete(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.DeleteSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        //assumes app has been already modified to contain the updated HLTB info
        public static async Task AcceptSuggestion([NotNull] AppEntity app, [NotNull] SuggestionEntity suggestion, int retries = -1)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (suggestion == null) throw new ArgumentNullException("suggestion");

            var table = await GetTable(SteamToHltbTableName, retries);

            CommonEventSource.Log.AcceptSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteBatchAsync(new TableBatchOperation {TableOperation.Replace(app), TableOperation.Delete(suggestion)});
            CommonEventSource.Log.AcceptSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        private static IEnumerable<TableBatchOperationInfo> SplitToBatchOperations<T>(
            IEnumerable<T> entities, Func<T, TableOperation[]> operationGenerator)
            where T: ITableEntity
        {
            var allOperations = new Dictionary<string, List<TableBatchOperationInfo>> ();
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
                        throw new ArgumentOutOfRangeException("operationGenerator",
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
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            if (value == null) throw new ArgumentNullException("value");

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, value),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, IncrementLastChar(value)));
        }

        public static string DoesNotStartWithFilter([NotNull] string propertyName, [NotNull] string value)
        {
            if (propertyName == null) throw new ArgumentNullException("propertyName");
            if (value == null) throw new ArgumentNullException("value");

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, value),
                TableOperators.Or,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, IncrementLastChar(value)));
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

        class TableBatchOperationInfo
        {
            public string Partition { get; private set; }
            public int Batch { get; private set; }
            public bool Final { get; private set; }
            public TableBatchOperation Operation { get; private set; }

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