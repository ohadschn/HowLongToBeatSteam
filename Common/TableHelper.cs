using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public static class TableHelper
    {
        private const int MaxBatchOperations = 100;
        private static readonly string TableStorageConnectionString = ConfigurationManager.ConnectionStrings["Hltbs"].ConnectionString;
        private static readonly string SteamToHltbTableName = ConfigurationManager.AppSettings["SteamToHltbTableName"];

        //segmentHandler will be called synchronously for each bucket but parallel across buckets
        public static async Task QueryAllGames(Action<TableQuerySegment<GameEntity>, int> segmentHandler) //int = bucket
        {
            Util.TraceInformation("Querying table concurrently...");
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);

            var tasks = new Task[GameEntity.Buckets];
            for (int bucket = 0; bucket < GameEntity.Buckets; bucket++)
            {
                var query = new TableQuery<GameEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, bucket.ToString(CultureInfo.InvariantCulture)));

                tasks[bucket] = QueryWithAllSegmentsAsync(table, query, segmentHandler, bucket);
            }

            await Task.WhenAll(tasks);
        }

        private static async Task QueryWithAllSegmentsAsync(
            CloudTable table, 
            TableQuery<GameEntity> query,
            Action<TableQuerySegment<GameEntity>, int> segmentHandler, //int = bucket
            int bucket)
        {
            TableQuerySegment<GameEntity> currentSegment = null;
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
        }

        public static Task InsertOrReplace(IEnumerable<GameEntity> games)
        {
            var tasks = new List<Task>();
            var batchOperations = new TableBatchOperation[GameEntity.Buckets];
            for (int i = 0; i < batchOperations.Length; i++)
            {
                batchOperations[i] = new TableBatchOperation();
            }
            var bucketCount = new int[GameEntity.Buckets];

            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            foreach (var gameEntity in games)
            {
                var bucket = gameEntity.PartitionKeyInt;
                var batchOperation = batchOperations[bucket];

                batchOperation.Add(TableOperation.InsertOrReplace(gameEntity));
                if (batchOperation.Count % MaxBatchOperations != 0)
                {
                    continue;
                }

                Util.TraceInformation("Updating bucket {0} / batch {1}...", bucket, ++bucketCount[bucket]);
                tasks.Add(table.ExecuteBatchAsync(batchOperation));

                batchOperations[bucket] = new TableBatchOperation();
            }

            for (int bucket = 0; bucket < GameEntity.Buckets; bucket++)
            {
                var batchOperation = batchOperations[bucket];
                if (batchOperation.Count % MaxBatchOperations != 0)
                {
                    Util.TraceInformation("Updating bucket {0} / batch {1} (final)...", bucket, ++bucketCount[bucket]);
                    tasks.Add(table.ExecuteBatchAsync(batchOperation));
                }
            }

            return Task.WhenAll(tasks);
        }

        public static async Task ResetTable()
        {
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            table.DeleteIfExists();
            await Task.Delay(TimeSpan.FromMinutes(1));
            table.CreateIfNotExists();
        }
    }
}