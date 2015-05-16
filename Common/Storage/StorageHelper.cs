using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Util;
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
            await QueryAllApps((segment, bucket) =>
            {
                foreach (var game in segment)
                {
                    knownSteamIds.Add(selector(game));
                }
            }, rowFilter, retries).ConfigureAwait(false);
            CommonEventSource.Log.QueryAllAppsStop(rowFilter ?? "(none)", knownSteamIds.Count);
            
            return knownSteamIds;
        }

        //segmentHandler(segment, bucket) will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllApps(Action<TableQuerySegment<AppEntity>, int> segmentHandler, string rowFilter = null, int retries = -1)
        {
            rowFilter = rowFilter ?? SuggestionEntity.NotSuggestionFilter;
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            await Enumerable.Range(0, AppEntity.Buckets).ForEachAsync(AppEntity.Buckets, async bucket =>
            {
                var partitionFilter = 
                    TableQuery.GenerateFilterCondition(PartitionKey, QueryComparisons.Equal, bucket.ToString(CultureInfo.InvariantCulture));

                var filter = String.IsNullOrWhiteSpace(rowFilter)
                    ? partitionFilter
                    : TableQuery.CombineFilters(partitionFilter, TableOperators.And, rowFilter);

                var query = new TableQuery<AppEntity>().Where(filter);

                TableQuerySegment<AppEntity> currentSegment = null;
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

        public static async Task ExecuteAppOperations(IEnumerable<AppEntity> apps, Func<AppEntity, TableOperation[]> operationGenerator, int retries = -1)
        {
            CommonEventSource.Log.ExecuteOperationsStart();
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

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

            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            CommonEventSource.Log.InsertSuggestionStart(suggestion.SteamAppId, suggestion.HltbId);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(suggestion)).ConfigureAwait(false);
            CommonEventSource.Log.InsertSuggestionStop(suggestion.SteamAppId, suggestion.HltbId);
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