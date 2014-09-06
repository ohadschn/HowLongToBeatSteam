using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ManualTableUpdater
{
    class Program
    {
        //TODO move to configuration
        private const string TableStorageConnectionString =
            @"DefaultEndpointsProtocol=https;AccountName=hltbs;AccountKey=XhDjB312agakHZdi2+xFCe5Dd3j2KAcl+yTtAiCyinCIOYuAKphKjaP0psCm83t/+iLKdMii/uxmUhMetZ7Hiw==";

        private const string SteamToHltbTableName = "steamToHltb";
        
        static void Main()
        {
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            table.CreateIfNotExists();

            var tasks = new List<Task>();
            int batchCount = 0;
            var batchOperation = new TableBatchOperation();
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
                batchOperation.Add(TableOperation.InsertOrReplace(gameEntity));
                if (++batchCount % 100 != 0)
                {
                    continue;
                }

                Console.WriteLine("Updating batch {0}...", batchCount / 100);
                tasks.Add(table.ExecuteBatchAsync(batchOperation));
                batchOperation = new TableBatchOperation();
            }

            if (batchCount%100 != 0)
            {
                Console.WriteLine("Updating final batch...");
                tasks.Add(table.ExecuteBatchAsync(batchOperation));
            }

            Console.WriteLine("Waiting for updates to finish...");
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("All done!");
        }
    }
}
