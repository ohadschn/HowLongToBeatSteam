using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace ManualTableUpdater
{
    class ManualUpdater
    {
        static void Main()
        {
            var games = new List<AppEntity>();
            foreach (var line in File.ReadLines(@"steamHltb.csv"))
            {
                var parts = line.Split(',');
                if (parts.Length != 3)
                {
                    continue; //too few to worry about these, we'll get them properly with the web job
                }

                string name = parts[0];
                int appId = int.Parse(parts[1]);
                int hltbId = int.Parse(parts[2]);

                games.Add(new AppEntity(appId, name, hltbId));
            }
            
            Util.TraceInformation("Initiating update operations...");
            var task = TableHelper.InsertOrReplace(games);

            Util.TraceInformation("Waiting for updates to finish...");
            task.Wait();

            Util.TraceInformation("All done!");
            Console.ReadLine();
        }
    }
}
