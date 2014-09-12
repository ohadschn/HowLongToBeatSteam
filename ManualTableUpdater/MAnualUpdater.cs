using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ManualTableUpdater
{
    class ManualUpdater
    {
        //TODO move to configuration
        private const string TableStorageConnectionString =
            @"DefaultEndpointsProtocol=https;AccountName=hltbs;AccountKey=XhDjB312agakHZdi2+xFCe5Dd3j2KAcl+yTtAiCyinCIOYuAKphKjaP0psCm83t/+iLKdMii/uxmUhMetZ7Hiw==";

        private const string SteamToHltbTableName = "steamToHltb";
        
        static void Main()
        {
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            //table.DeleteIfExists(); return;
            table.CreateIfNotExists();

            var tasks = new List<Task>();

            var batchOperations = new TableBatchOperation[GameEntity.Buckets];
            for (int i = 0; i < batchOperations.Length; i++)
            {
                batchOperations[i] = new TableBatchOperation();
            }

            var bucketCount = new int[GameEntity.Buckets];

            foreach (var line in File.ReadLines(@"F:\Downloads\steamHltb.csv"))
            {
                var parts = line.Split(',');
                if (parts.Length != 3)
                {
                    continue; //too few to worry about these, we'll get them properly with the web job
                }

                string name = parts[0];
                int appId = int.Parse(parts[1]);
                int hltbId = int.Parse(parts[2]);

                var gameEntity = new GameEntity(appId, name, hltbId);
                var batchOperation = batchOperations[gameEntity.PartitionKeyInt];
                
                batchOperation.Add(TableOperation.InsertOrReplace(gameEntity));
                if (batchOperation.Count%100 != 0)
                {
                    continue;
                }

                Console.WriteLine("Updating bucket {0} / batch {1}...", gameEntity.PartitionKeyInt, ++bucketCount[gameEntity.PartitionKeyInt]);
                tasks.Add(table.ExecuteBatchAsync(batchOperation));
                batchOperations[gameEntity.PartitionKeyInt] = new TableBatchOperation();
            }

            for (int i = 0; i < batchOperations.Length; i++)
            {
                if (batchOperations[i].Count % 100 != 0)
                {
                    Console.WriteLine("Updating final batch ({0}) for bucket {1}...", bucketCount[i], i);
                   tasks.Add(table.ExecuteBatchAsync(batchOperations[i]));
                }
            }

            Console.WriteLine("Waiting for updates to finish...");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("All done!");
            Console.ReadLine();
        }
    }
}
