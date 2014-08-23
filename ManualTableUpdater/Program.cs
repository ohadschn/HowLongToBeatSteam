using System;
using System.IO;
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

            foreach (var line in File.ReadLines(@"F:\Downloads\steamHltb.csv"))
            {
                var parts = line.Split(',');
                if (parts.Length != 3)
                {
                    continue;
                }

                int appId = int.Parse(parts[1]);
                string name = parts[0];
                int hltbId = int.Parse(parts[2]);

                var gameEntity = new GameEntity(appId, name, hltbId);
                Console.WriteLine("Updating {0}...", gameEntity);
                table.Execute(TableOperation.InsertOrReplace(gameEntity));
            }
        }
    }
}
