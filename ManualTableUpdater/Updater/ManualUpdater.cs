using System;
using System.Linq;
using Common.Logging;
using Common.Storage;
using Common.Util;

namespace ManualTableUpdater.Updater
{
    class ManualUpdater
    {
        static void Main()
        {
            try
            {
                using (var client = new HttpRetryClient(1))
                {
                    client.GetAsync("http://jklasdhsajlkqwewqrt23231dhsalkjdahlk.com").Wait();
                }
                //var games = GetAppsFromCsv();
                //GetAppsWithStoreData().Wait();

                //SiteUtil.TraceInformation("All done!");
                //var ids = StorageHelper.GetAllApps(e => e.SteamAppId, StorageHelper.DoesNotStartWithFilter(StorageHelper.RowKey, "Suggestion")).Result;

                //foreach (var app in StorageHelper.GetAllApps(a => a).Result.Where(a => a.Measured).OrderBy(a => a.AppType))
                //{
                //    Console.WriteLine("{0} ({1}): {2} | {3}", app.SteamName, app.AppType, app.GenresFlat, app.CategoriesFlat);
                //}

                //foreach (var app in StorageHelper.GetAllApps(a => a).Result
                //    .Where(a => String.Equals(a.Genres.First(), "Education", StringComparison.OrdinalIgnoreCase)))
                //{
                //    Console.WriteLine("{0} ({1}): {2} | {3}", app.SteamName, app.AppType, app.GenresFlat, app.CategoriesFlat);                    
                //}

                //var measured = StorageHelper.GetAllApps(e => e, AppEntity.MeasuredFilter).Result;
                //StorageHelper.GetAllApps(e => e, AppEntity.MeasuredFilter).Result;
                //foreach (var genre in measured.Select(a => a.Genres.First()).Distinct())
                //{
                //    Console.WriteLine(genre);
                //}

                //foreach (var app in measured.Where(a =>
                //    a.Genres.Contains("Unknown", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Audio Production", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Animation & Modeling", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Design & Illustration", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Utilities", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Web Publishing", StringComparer.Ordinal) ||
                //    a.Genres.Contains("Video Production", StringComparer.Ordinal)).OrderBy(a => a.Genres.First()))
                //{
                //    Console.WriteLine("{0}: {1} | {2}", app.SteamName, app.GenresFlat, app.CategoriesFlat);
                //}

                //var games = measured.Where(m => m.Genres.First() == "Action").ToArray();
                //foreach (var game in games)
                //{
                //    Console.WriteLine(game);
                //}
                //Console.WriteLine("Total: " + games.Count());

                //foreach (var app in StorageHelper.GetAllApps(e => e).Result
                //    .Where(a => a.MainTtb == 0 && a.ExtrasTtb == 0 && a.CompletionistTtb == 0 && 
                //        (!a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed)))
                //{
                //    Console.WriteLine(app);
                //}

                Console.WriteLine("Press any key to continue...");
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
