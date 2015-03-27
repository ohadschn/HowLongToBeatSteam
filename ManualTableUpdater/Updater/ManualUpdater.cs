using System;
using System.IO;
using System.Linq;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using SteamHltbScraper.Scraper;

namespace ManualTableUpdater.Updater
{
    class ManualUpdater
    {
        static void Main()
        {
            try
            {
                HltbScraper.ScrapeHltb(new[] { new AppEntity(80, "Baron Wittard", AppEntity.GameTypeName) }).Wait();
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
                //using (var writer = new StreamWriter("games.tsv"))
                //{
                //    writer.WriteLine("SteamID\tSteamName\tType\tGenres\tCategories\tHltbID\tHltbName\tMain\tMainImputed\tExtras\tExtrasImputed\tCompletionist\tCompletionistImputed\tDevelopers\tPublishers\tPlatforms\tMetacritic\tReleaseDate");
                //    foreach (var app in StorageHelper.GetAllApps(a => a, AppEntity.MeasuredFilter).Result)
                //    {
                //        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                //            RemoveTabs(app.SteamAppId),
                //            RemoveTabs(app.SteamName),
                //            RemoveTabs(app.AppType),
                //            RemoveTabs(app.GenresFlat),
                //            RemoveTabs(app.CategoriesFlat),
                //            RemoveTabs(app.HltbId),
                //            RemoveTabs(app.HltbName),
                //            RemoveTabs(app.MainTtb),
                //            RemoveTabs(app.MainTtbImputed),
                //            RemoveTabs(app.ExtrasTtb),
                //            RemoveTabs(app.ExtrasTtbImputed),
                //            RemoveTabs(app.CompletionistTtb),
                //            RemoveTabs(app.CompletionistTtbImputed),
                //            RemoveTabs(app.DevelopersFlat),
                //            RemoveTabs(app.PublishersFlat),
                //            RemoveTabs(app.Platforms),
                //            RemoveTabs(app.MetacriticScore),
                //            RemoveTabs(app.ReleaseDate));
                //    }   
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

        //public static string RemoveTabs(object obj)
        //{
        //    return obj == null ? String.Empty : obj.ToString().Replace('\t', ';');
        //}

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
