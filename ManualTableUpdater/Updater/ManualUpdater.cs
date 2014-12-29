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
                //var ids = StorageHelper.GetAllApps(e => e.SteamAppId, StorageHelper.DoesNotStartWithFilter(StorageHelper.RowKey, "Suggestion")).Result;
                foreach (var app in StorageHelper.GetAllApps(e => e).Result
                    .Where(a => a.MainTtb == 0 && a.ExtrasTtb == 0 && a.CompletionistTtb == 0 && 
                        (!a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed)))
                {
                    Console.WriteLine(app);
                }

                Console.WriteLine("Press any key to continute...");
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
        //    StorageHelper.InsertApps(games);
        //}
    }
}
