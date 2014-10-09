using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public static class TableHelper
    {
        public const string PartitionKey = "PartitionKey";
        public const string RowKey = "RowKey";

        private const int MaxBatchOperations = 100;
        private static readonly string TableStorageConnectionString = ConfigurationManager.ConnectionStrings["Hltbs"].ConnectionString;
        private static readonly string SteamToHltbTableName = ConfigurationManager.AppSettings["SteamToHltbTableName"];
        
        private static readonly TimeSpan DefaultDeltaBackoff = TimeSpan.FromSeconds(4);

        public static async Task<ConcurrentBag<T>> GetAllApps<T>(Func<AppEntity, T> selector, string rowFilter = null, int retries = -1)
        {
            var knownSteamIds = new ConcurrentBag<T>();
            await QueryAllApps((segment, bucket) =>
            {
                foreach (var game in segment)
                {
                    knownSteamIds.Add(selector(game));
                }
            }, rowFilter, retries).ConfigureAwait(false);
            SiteUtil.TraceInformation("Finished getting apps. Count: {0}", knownSteamIds.Count);
            return knownSteamIds;
        }

        //segmentHandler(segment, bucket) will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllApps(Action<TableQuerySegment<AppEntity>, int> segmentHandler, string rowFilter = null, int retries = -1)
        {
            SiteUtil.TraceInformation("Getting all apps with filter: {0}", rowFilter ?? "(none)");
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);

            SiteUtil.TraceInformation("Querying table concurrently...");
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
                    SiteUtil.TraceInformation("Retrieving mappings for bucket {0} / batch {1}...", bucket, batch);
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null).ConfigureAwait(false);

                    SiteUtil.TraceInformation("Processing bucket {0} / batch {1}...", bucket, batch);
                    segmentHandler(currentSegment, bucket);

                    SiteUtil.TraceInformation("Finished processing bucket {0} / batch {1}", bucket, batch);
                    batch++;
                }
            }).ConfigureAwait(false);

            SiteUtil.TraceInformation("Finished querying table");
        }

        public static Task Replace(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteOperations(games, e => new [] {TableOperation.Replace(e)}, retries);
        }

        public static Task Insert(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteOperations(games, e => new [] {TableOperation.Insert(e)}, retries);
        }

        public static Task ExecuteOperations(IEnumerable<AppEntity> apps, Func<AppEntity, TableOperation[]> operationGenerator, int retries = -1)
        {
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);
            return SplitToBatchOperations(apps, operationGenerator).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, tboi =>
            {
                SiteUtil.TraceInformation("Executing batch operation for bucket {0} / batch {1} {2}...", 
                    tboi.Bucket, tboi.Batch, tboi.Final ? "(final)" : String.Empty);
                
                return table.ExecuteBatchAsync(tboi.Operation);
            });
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

        private static CloudTableClient GetCloudTableClient(int retries)
        {
            var cloudTableClient = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient();
            if (retries >= 0)
            {
                cloudTableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(DefaultDeltaBackoff, retries);
            }
            return cloudTableClient;
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