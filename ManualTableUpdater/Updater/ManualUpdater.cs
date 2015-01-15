using System;
using System.IO;
using System.Linq;
using Common.Entities;
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
                //var allApps = StorageHelper.GetAllApps(e => e, AppEntity.MeasuredFilter, 20).Result.ToArray();
                //Imputer.Impute(allApps)
                
                //var games = GetAppsFromCsv();
                //GetAppsWithStoreData().Wait();

                //SiteUtil.TraceInformation("All done!");
                //var ids = StorageHelper.GetAllApps(e => e.SteamAppId, StorageHelper.DoesNotStartWithFilter(StorageHelper.RowKey, "Suggestion")).Result;

                //foreach (var app in StorageHelper.GetAllApps(a => a).Result.Where(a => a.Measured).OrderBy(a => a.AppType))
                //{
                //    Console.WriteLine("{0} ({1}): {2} | {3}", app.SteamName, app.AppType, app.GenresFlat, app.CategoriesFlat);
                //}

                //foreach (var app in StorageHelper.GetAllApps(a => a).Result
                //    .Where(a => a.Genres.Contains("Massively Multiplayer", StringComparer.OrdinalIgnoreCase) && a.Measured && a.Categories.Contains("Single-player", StringComparer.OrdinalIgnoreCase)))
                //{
                //    //Console.WriteLine("{10} / {0} ({9}): {1}/{2}/{3} ({4}/{5}/{6}) | {7} | {8}",
                //    //    app.SteamName, app.MainTtb, app.ExtrasTtb, app.CompletionistTtb,
                //    //    app.MainTtbImputed, app.ExtrasTtbImputed, app.CompletionistTtbImputed,
                //    //    app.GenresFlat, app.CategoriesFlat, app.AppType, app.SteamAppId);
                //    Console.WriteLine(DataContractSerializeObject(app));
                //}
                using (var writer = new StreamWriter("games.csv"))
                {
                    foreach (var app in StorageHelper.GetAllApps(a => a, AppEntity.MeasuredFilter).Result)
                    {
                        writer.WriteLine("{0},{1},{2},{3},{4}", 
                            app.IsGame,
                            app.Genres.First().Replace(",","-"), 
                            app.MainTtbImputed ? 0 : app.MainTtb,
                            app.ExtrasTtbImputed ? 0 : app.ExtrasTtb,
                            app.CompletionistTtbImputed ? 0 : app.CompletionistTtb);
                    }   
                }

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
