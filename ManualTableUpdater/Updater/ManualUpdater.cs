using System;
using System.Linq;
using Common.Logging;
using Common.Storage;

namespace ManualTableUpdater.Updater
{
    class ManualUpdater
    {
        static void Main()
        {
            try
            {
                //var games = GetAppsFromCsv();
                //GetAppsWithStoreData().Wait();

                //SiteUtil.TraceInformation("All done!");
                //var ids = TableHelper.GetAllApps(e => e.SteamAppId, TableHelper.DoesNotStartWithFilter(TableHelper.RowKey, "Suggestion")).Result;
                var ids = TableHelper.GetAllApps(e => e.SteamAppId).Result;
                Console.WriteLine(ids.Contains(12345));
                Console.ReadLine();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        //public static void GetAppsFromCsv()
        //{
        //    var games = new List<AppEntity>();
        //    foreach (var line in File.ReadLines(@"steamHltb.csv"))
        //    {
        //        var parts = line.Split(',');
        //        if (parts.Length != 3)
        //        {
        //            continue; //too few to worry about these, we'll get them properly with the web job
        //        }

        //        string name = parts[0];
        //        int appId = int.Parse(parts[1]);
        //        int hltbId = int.Parse(parts[2]);

        //        games.Add(new AppEntity(appId, name, hltbId));
        //    }

        //    SiteUtil.TraceInformation("Updating...");
        //    TableHelper.InsertApps(games);
        //}
    }
}
