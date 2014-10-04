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

        public static async Task<IEnumerable<T>> GetAllApps<T>(Func<AppEntity, T> selector, string rowFilter = null, int retries = -1)
        {
            var knownSteamIds = new ConcurrentBag<T>();
            await QueryAllApps((segment, bucket) =>
            {
                foreach (var game in segment)
                {
                    knownSteamIds.Add(selector(game));
                }
            }, rowFilter, retries).ConfigureAwait(false);
            Util.TraceInformation("Finished getting apps. Count: {0}", knownSteamIds.Count);
            return knownSteamIds;
        }

        //segmentHandler(segment, bucket) will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllApps(Action<TableQuerySegment<AppEntity>, int> segmentHandler, string rowFilter = null, int retries = -1)
        {
            Util.TraceInformation("Getting all apps with filter: {0}", rowFilter ?? "(none)");
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);

            Util.TraceInformation("Querying table concurrently...");
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
                    Util.TraceInformation("Retrieving mappings for bucket {0} / batch {1}...", bucket, batch);
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null).ConfigureAwait(false);

                    Util.TraceInformation("Processing bucket {0} / batch {1}...", bucket, batch);
                    segmentHandler(currentSegment, bucket);

                    Util.TraceInformation("Finished processing bucket {0} / batch {1}", bucket, batch);
                    batch++;
                }
            }).ConfigureAwait(false);

            Util.TraceInformation("Finished querying table");
        }

        public static Task Replace(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteOperations(games, TableOperation.Replace, retries);
        }

        public static Task Insert(IEnumerable<AppEntity> games, int retries = -1)
        {
            return ExecuteOperations(games, TableOperation.Insert, retries);
        }

        private static async Task ExecuteOperations(IEnumerable<AppEntity> games, Func<AppEntity, TableOperation> operation, int retries = -1)
        {
            var table = GetCloudTableClient(retries).GetTableReference(SteamToHltbTableName);

            await games.GroupBy(ae => ae.PartitionKeyInt).ForEachAsync(AppEntity.Buckets, async ag =>
            {
                int bucket = ag.Key;
                int batch = 1;
                var batchOperation = new TableBatchOperation();
                foreach (var gameEntity in ag)
                {
                    batchOperation.Add(operation(gameEntity));
                    if (batchOperation.Count < MaxBatchOperations)
                    {
                        continue;
                    }

                    Util.TraceInformation("Updating bucket {0} / batch {1}...", bucket, batch++);
                    await table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);

                    batchOperation = new TableBatchOperation();
                }
                
                if (batchOperation.Count != 0)
                {
                    Util.TraceInformation("Updating bucket {0} / batch {1} (final)...", bucket, batch);
                    await table.ExecuteBatchAsync(batchOperation).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
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
    }
}