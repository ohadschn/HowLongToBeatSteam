using System;
using Common;

namespace ManualTableUpdater
{
    class ManualUpdater
    {

        static void Main()
        {
            //var games = GetAppsFromCsv();
            //GetAppsWithStoreData().Wait();

            SiteUtil.TraceInformation("All done!");
            Console.ReadLine();
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
        //    TableHelper.Insert(games);
        //}
    }
}
