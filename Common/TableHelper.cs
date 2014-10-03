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

        public static async Task<IEnumerable<T>> GetAllApps<T>(Func<AppEntity, T> selector, string rowFilter = null, IRetryPolicy retryPolicy = null)
        {
            Util.TraceInformation("Getting all known apps...");
            var knownSteamIds = new ConcurrentBag<T>();
            await QueryAllApps((segment, bucket) =>
            {
                foreach (var game in segment)
                {
                    knownSteamIds.Add(selector(game));
                }
            }, rowFilter, retryPolicy);
            Util.TraceInformation("Finished getting all apps. Count: {0}", knownSteamIds.Count);
            return knownSteamIds;
        }

        //segmentHandler will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllApps(Action<TableQuerySegment<AppEntity>, int> segmentHandler, string rowFilter = null, IRetryPolicy retryPolicy = null) //int = bucket
        {
            Util.TraceInformation("Querying table concurrently...");
            var cloudTableClient = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient();
            if (retryPolicy != null)
            {
                cloudTableClient.DefaultRequestOptions.RetryPolicy = retryPolicy;
            }
            var table = cloudTableClient.GetTableReference(SteamToHltbTableName);

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
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null);

                    Util.TraceInformation("Processing bucket {0} / batch {1}...", bucket, batch);
                    segmentHandler(currentSegment, bucket);

                    Util.TraceInformation("Finished processing bucket {0} / batch {1}", bucket, batch);
                    batch++;
                }
            });

            Util.TraceInformation("Finished querying table");
        }

        public static Task InsertOrReplace(IEnumerable<AppEntity> games)
        {
            return ExecuteOperations(games, TableOperation.InsertOrReplace);
        }

        public static Task Delete(IEnumerable<AppEntity> games)
        {
            return ExecuteOperations(games, TableOperation.Delete);
        }

        public static async Task ExecuteOperations(IEnumerable<AppEntity> games, Func<AppEntity, TableOperation> operation)
        {
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);

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
                    await table.ExecuteBatchAsync(batchOperation);

                    batchOperation = new TableBatchOperation();
                }
                
                if (batchOperation.Count != 0)
                {
                    Util.TraceInformation("Updating bucket {0} / batch {1} (final)...", bucket, batch);
                    await table.ExecuteBatchAsync(batchOperation);
                }
            });
        }

        public static async Task ResetTable()
        {
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            await table.DeleteIfExistsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1));
            await table.CreateIfNotExistsAsync();
        }

        public static string StartsWithFilter(string propertyName, string value)
        {
            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.GreaterThanOrEqual, value),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(propertyName, QueryComparisons.LessThan, IncrementLastChar(value)));
        }

        private static string IncrementLastChar(string str)
        {
            char last = str[str.Length-1];

            if (last == Char.MaxValue)
            {
                return str + (char)0;
            }
            
            return str.Remove(str.Length - 1, 1) + (char)(last + 1);
        }
    }
}