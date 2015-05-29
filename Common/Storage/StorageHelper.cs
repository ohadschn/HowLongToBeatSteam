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

        private static readonly string SteamToHltbTableName = SiteUtil.GetMandatoryValueFromConfig("SteamToHltbTableName");
        public static readonly string AzureStorageTablesConnectionString = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("HltbsTables");
        public static readonly string AzureStorageBlobConnectionString = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("HltbsBlobs");        
        private static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromSeconds(4);

        public static async Task<ConcurrentBag<T>> GetAllApps<T>(Func<AppEntity, T> selector, string rowFilter = null, int retries = -1)
        {
            rowFilter = rowFilter ?? SuggestionEntity.NotSuggestionFilter;
            var knownSteamIds = new ConcurrentBag<T>();

            CommonEventSource.Log.QueryAllAppsStart(rowFilter ?? "(none)");
            await QueryAllTableEntities<AppEntity>(
                (segment, bucket) => knownSteamIds.AddRange(segment.Select(selector)), AppEntity.Buckets, rowFilter, retries).ConfigureAwait(false);
            CommonEventSource.Log.QueryAllAppsStop(rowFilter ?? "(none)", knownSteamIds.Count);
            
            return knownSteamIds;
        }

        public static async Task<ConcurrentBag<SuggestionEntity>> GetAllSuggestions(int retries = -1)
        {
            var suggestions = new ConcurrentBag<SuggestionEntity>();

            CommonEventSource.Log.QueryAllSuggestionsStart(SuggestionEntity.SuggestionFilter);
            await QueryAllTableEntities<SuggestionEntity>(
                (segment, bucket) => suggestions.AddRange(segment), SuggestionEntity.Buckets, SuggestionEntity.SuggestionFilter, retries)
                .ConfigureAwait(false);
            CommonEventSource.Log.QueryAllSuggestionsStop(SuggestionEntity.SuggestionFilter, suggestions.Count);

            return suggestions;
        }

        //segmentHandler(segment, bucket) will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllTableEntities<T>(Action<TableQuerySegment<T>, int> segmentHandler, int buckets, string rowFilter = null, int retries = -1)
            where T : ITableEntity, new()
        {
            rowFilter = rowFilter ?? SuggestionEntity.NotSuggestionFilter;
            var table = await GetSteamToHltbTable(retries);

            await Enumerable.Range(0, buckets).ForEachAsync(buckets, async bucket =>
            {
                var partitionFilter = 
                    TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, bucket.ToString(CultureInfo.InvariantCulture));

                var filter = String.IsNullOrWhiteSpace(rowFilter)
                    ? partitionFilter
                    : TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

                var query = new TableQuery<T>().Where(filter);

                TableQuerySegment<T> currentSegment = null;
                int batch = 1;
                while (currentSegment == null || currentSegment.ContinuationToken != null)
                {
                    CommonEventSource.Log.RetrieveBucketBatchMappingsStart(bucket, batch);
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null).ConfigureAwait(false);
                    CommonEventSource.Log.RetrieveBucketBatchMappingsStop(bucket, batch);

                    CommonEventSource.Log.ProcessBucketBatchStart(bucket, batch);
                    segmentHandler(currentSegment, bucket);
                    CommonEventSource.Log.ProcessBucketBatchStop(bucket, batch);

                    batch++;
                }
            }).ConfigureAwait(false);

        }

        public static Task ReplaceApps(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteAppOperations(games, e => new [] {TableOperation.Replace(e)}, retries);
        }

        public static Task InsertApps(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteAppOperations(games, e => new [] {TableOperation.Insert(e)}, retries);
        }

        public static Task InsertOrReplaceApps(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteAppOperations(games, e => new [] {TableOperation.InsertOrReplace(e)}, retries);
        }

        public static async Task ExecuteAppOperations(IEnumerable<AppEntity> apps, Func<AppEntity, TableOperation[]> operationGenerator, int retries = -1)
        {
            CommonEventSource.Log.ExecuteOperationsStart();
            var table = await GetSteamToHltbTable(retries);

            await SplitToBatchOperations(apps, operationGenerator).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async tboi =>
            {
                var final = tboi.Final ? "(final)" : String.Empty;

                CommonEventSource.Log.ExecuteBucketBatchOperationStart(tboi.Bucket, tboi.Batch, final);
                await table.ExecuteBatchAsync(tboi.Operation).ConfigureAwait(false);
                CommonEventSource.Log.ExecuteBucketBatchOperationStop(tboi.Bucket, tboi.Batch, final);

            }, false).ConfigureAwait(false);
            CommonEventSource.Log.ExecuteOperationsStop();
        }

        public static async Task InsertSuggestion(SuggestionEntity suggestion, int retries = -1)
        {
            if (suggestion == null)
            {
                throw new ArgumentNullException("suggestion");
            }

            var table = await GetSteamToHltbTable(retries);

            CommonEventSource.Log.InsertSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.InsertSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        public static async Task DeleteSuggestion([NotNull] SuggestionEntity suggestion, int retries = -1)
        {
            if (suggestion == null) throw new ArgumentNullException("suggestion");

            var table = await GetSteamToHltbTable(retries);

            CommonEventSource.Log.DeleteSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.Delete(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.DeleteSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        public static async Task AcceptSuggestion([NotNull] AppEntity app, [NotNull] SuggestionEntity suggestion, int retries = -1)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (suggestion == null) throw new ArgumentNullException("suggestion");

            var table = await GetSteamToHltbTable(retries);

            CommonEventSource.Log.AcceptSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            app.HltbId = suggestion.HltbId;
            await table.ExecuteBatchAsync(new TableBatchOperation {TableOperation.Replace(app), TableOperation.Delete(suggestion)});
            CommonEventSource.Log.AcceptSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
        }

        private static IEnumerable<TableBatchOperationInfo> SplitToBatchOperations(
            IEnumerable<AppEntity> apps, Func<AppEntity, TableOperation[]> operationGenerator)
        {
            var allOperations = SiteUtil.GenerateInitializedArray(AppEntity.Buckets, i => new List<TableBatchOperationInfo>());
            foreach (var appGroup in apps.GroupBy(ae => ae.Bucket))
            {
                int bucket = appGroup.Key;
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
                        allOperations[bucket].Add(new TableBatchOperationInfo(bucket, batch++, false, batchOperation));
                        batchOperation = new TableBatchOperation();
                    }

                    foreach (var operation in operations)
                    {
                        batchOperation.Add(operation);
                    }
                }

                if (batchOperation.Count != 0)
                {
                    allOperations[bucket].Add(new TableBatchOperationInfo(bucket, batch, true, batchOperation));
                }
            }
            return allOperations.Interleave();
        }

        public static string StartsWithFilter(string propertyName, string value)
        {
            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, value),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, IncrementLastChar(value)));
        }

        public static string DoesNotStartWithFilter(string propertyName, string value)
        {
            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, value),
                TableOperators.Or,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, IncrementLastChar(value)));
        }

        private static CloudTableClient GetCloudTableClient(int retries)
        {
            var cloudTableClient = CloudStorageAccount.Parse(AzureStorageTablesConnectionString).CreateCloudTableClient();
            if (retries >= 0)
            {
                cloudTableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(DefaultDeltaBackoff, retries);
            }
            return cloudTableClient;
        }

        private static async Task<CloudTable> GetSteamToHltbTable(int retries)
        {
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);
            return table;
        }

        public static CloudBlobClient GetCloudBlobClient(int retries)
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
            public int Bucket { get; private set; }
            public int Batch { get; private set; }
            public bool Final { get; private set; }
            public TableBatchOperation Operation { get; private set; }

            public TableBatchOperationInfo(int bucket, int batch, bool final, TableBatchOperation operation)
            {
                Bucket = bucket;
                Batch = batch;
                Final = final;
                Operation = operation;
            }
        }
    }
}