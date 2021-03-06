﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Util;
using Microsoft.WindowsAzure.Storage.Table;
using static System.FormattableString;

namespace ManualTableUpdater.Updater
{
    [DataContract]
    public class SuggestionData
    {
        [DataMember]
        public int SteamAppId { get; set; }
        [DataMember]
        public int HltbId { get; set; }
        [DataMember]
        public string AppType { get; set; }

        public SuggestionData(int steamAppId, int hltbId, string appType)
        {
            SteamAppId = steamAppId;
            HltbId = hltbId;
            AppType = appType;
        }
    }

    [DataContract]
    public class AppEntityData
    {
        [DataMember]
        public int SteamAppId { get; set; }
        [DataMember]
        public string SteamName { get; set; }
        [DataMember]
        public string AppType { get; set; }
        [DataMember]
        public Platforms Platforms { get; set; }
        public IReadOnlyList<string> Categories { get; }
        public IReadOnlyList<string> Genres { get; }
        public IReadOnlyList<string> Developers { get; }
        public IReadOnlyList<string> Publishers { get; }
        [DataMember]
        public DateTime ReleaseDate { get; set; }
        [DataMember]
        public int MetacriticScore { get; set; }
        [DataMember]
        public int HltbId { get; set; }
        [DataMember]
        public string HltbName { get; set; }
        [DataMember]
        public int MainTtb { get; set; }
        [DataMember]
        public bool MainTtbImputed { get; set; }
        [DataMember]
        public int ExtrasTtb { get; set; }
        [DataMember]
        public bool ExtrasTtbImputed { get; set; }
        [DataMember]
        public int CompletionistTtb { get; set; }
        [DataMember]
        public bool CompletionistTtbImputed { get; set; }
        [DataMember]
        public bool VerifiedGame { get; set; }

        public AppEntityData(int steamAppId, string steamName, string appType, Platforms platforms,
            IReadOnlyList<string> categories, IReadOnlyList<string> genres, IReadOnlyList<string> developers,
            IReadOnlyList<string> publishers, DateTime releaseDate, int metacriticScore,
            int hltbId, string hltbName, 
            int mainTtb, bool mainTtbImputed, int extrasTtb, bool extrasTtbImputed, int completionistTtb, bool completionistTtbImputed,
            bool verifiedGame)
        {
            SteamAppId = steamAppId;
            SteamName = steamName;
            AppType = appType;
            Platforms = platforms;
            Categories = categories;
            Genres = genres;
            Developers = developers;
            Publishers = publishers;
            ReleaseDate = releaseDate;
            MetacriticScore = metacriticScore;
            HltbId = hltbId;
            HltbName = hltbName;
            MainTtb = mainTtb;
            MainTtbImputed = mainTtbImputed;
            ExtrasTtb = extrasTtb;
            ExtrasTtbImputed = extrasTtbImputed;
            CompletionistTtb = completionistTtb;
            CompletionistTtbImputed = completionistTtbImputed;
            VerifiedGame = verifiedGame;
        }
    }

    public static class ManualUpdater
    {
        public const string AppDataXml = "AppData.xml";

        private static void Main()
        {
            if (StorageHelper.AzureStorageTablesConnectionString.Contains("staging"))
            {
                Console.WriteLine("STAGING connection string detected - press any key to continue...");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Non-staging connection string detected");
                Console.WriteLine("By proceeding, you might override PRODUCTION DATA");
                Console.Write("Are you absolutely sure? If so, type ABSOLUTELY SURE (in capital letters): ");
                var input = Console.ReadLine();
                if (input != "ABSOLUTELY SURE")
                {
                    Console.WriteLine("You are not absolutely sure, aborting");
                    return;
                }
            }

            try
            {
                
                //RebuildProcessedSuggestions();
                //ProcessIDarbSuggestions();
                //ProcessIDarbApps();
                //PrintGenres();
                //SerializeAllAppsToFile();
                //LoadAllAppsFromFile();
                //WriteAllMeasuredToTsv();
                //DeleteInvalidSuggestions();
                //InsertManualSuggestions();
                //DeleteUnknowns();
                ForceUpdateAppHltbId(409710, 1065);
                ForceUpdateAppHltbId(409720, 1066);
                //ForceUpdateAppHltbId(266310, 27913);
                //SendMail();
                //GetEarliestGame();
                Console.WriteLine("Done - Press any key to continue...");
                Console.ReadLine();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static void RebuildProcessedSuggestions()
        {
            var allProcessedSuggestions = StorageHelper.GetAllProcessedSuggestions().Result;
            foreach (var processedSuggestion in allProcessedSuggestions.Where(ps => ps.SteamAppId == 0))
            {
                Console.Write(Invariant($"Restoring processed suggestions: {processedSuggestion} ({processedSuggestion.RowKey})..."));
                var components = processedSuggestion.RowKey.Split('_');
                processedSuggestion.SteamAppId = Convert.ToInt32(components[1], CultureInfo.InvariantCulture);
                processedSuggestion.HltbId = Convert.ToInt32(components[2], CultureInfo.InvariantCulture);
                processedSuggestion.AppType = components[3];
                StorageHelper.UpdateProcessedSuggestions(processedSuggestion).Wait();
                Console.WriteLine("Done.");
            }
        }

        public static void ProcessIDarbSuggestions()
        {
            var suggestions = StorageHelper.GetAllSuggestions().Result;
            var idarbSuggestions = suggestions.Where(a => a.HltbId == 23224).ToArray();

            Console.WriteLine("Serializing #IDARB suggestions to file...");
            SerializeSuggestionsToFile(idarbSuggestions, "IDARB-suggestions.xml");

            Console.WriteLine("Removing #IDARB suggestions...");
            foreach (var suggestion in idarbSuggestions)
            {
                Console.WriteLine("Removing #IDARB suggestion:");
                Console.WriteLine(suggestion);
                StorageHelper.Delete(new[] { suggestion }, "Removing #IDARB suggestion", StorageHelper.SteamToHltbTableName).Wait();
            }
        }

        public static void ProcessIDarbApps()
        {
            var apps = StorageHelper.GetAllApps(AppEntity.MeasuredFilter).Result;
            var idarbApps = apps.Where(a => a.HltbId == 23224).ToArray();

            Console.WriteLine("Serializing #IDARB apps to file...");
            SerializeAppsToFile(idarbApps, "IDARB.xml");

            Console.WriteLine("Removing #IDARB-correlated games...");
            foreach (var app in idarbApps)
            {
                Console.WriteLine("Removing #IDARB app:");
                PrintGame(app);
                StorageHelper.Delete(new[] {app}, "Resetting #IDARB correlation", StorageHelper.SteamToHltbTableName).Wait();
            }

            Console.WriteLine("apps removed - now run the missing games updater");
        }

        public static void SendMail()
        {
            SiteUtil.SendSuccessMail("lorem ipsum", "foo", TimeSpan.FromSeconds(20)).Wait();
        }

        public static void DeleteInvalidSuggestions()
        {
            var suggestions = StorageHelper.GetAllSuggestions().Result;
            Console.WriteLine(String.Join(Environment.NewLine, suggestions));
        }

        public static void ForceUpdateAppHltbId(int steamId, int hltbId)
        {
            var app = StorageHelper.GetAllApps().Result.First(a => a.SteamAppId == steamId);
            app.HltbId = hltbId;
            StorageHelper.Replace(new[] {app}, "Force update HLTB ID").Wait();
        }

        public static void DeleteUnknowns()
        {
            var unknowns = StorageHelper.GetAllApps(AppEntity.UnknownFilter).Result;
            Console.WriteLine(String.Join(Environment.NewLine, 
                unknowns.Select(u => String.Format(CultureInfo.InvariantCulture, "{0} / {1}", u.SteamName, u.SteamAppId))));
            Console.WriteLine("Unknowns: " + unknowns.Count);
            StorageHelper.ExecuteOperations(unknowns, a => new [] {TableOperation.Delete(a)}, StorageHelper.SteamToHltbTableName, "Deleting unknowns").Wait();
        }

        public static void InsertManualSuggestions()
        {
            Task.WaitAll(
                StorageHelper.InsertSuggestion(new SuggestionEntity(3830, 1, AppEntity.EndlessTitleTypeName)),
                StorageHelper.InsertSuggestion(new SuggestionEntity(9050, 1, AppEntity.EndlessTitleTypeName))
                );
        }

        public static void GetEarliestGame()
        {
            var allGames = StorageHelper.GetAllApps().Result;
            var gamesWithReleaseDates = allGames.Where(a => a.ReleaseDate.Year > 1900).ToArray();
            var firstReleaseDate = gamesWithReleaseDates.Min(a => a.ReleaseDate);
            Console.WriteLine("First game:" + firstReleaseDate);
            Console.WriteLine("Games:" + Environment.NewLine + String.Join(Environment.NewLine, gamesWithReleaseDates.Where(a => a.ReleaseDate == firstReleaseDate).Select(a => a.SteamName)));
        }

        public static void SerializeAllAppsToFile()
        {
            Console.WriteLine("About to serialize all apps to file (overwriting existing): " + Path.Combine(Environment.CurrentDirectory, AppDataXml));
            Console.WriteLine("Are you sure [y/n]? ");
            var input = Console.ReadLine();
            if (input != "y" && input != "Y")
            {
                Console.WriteLine("You are not sure, aborting");
                return;
            }

            Console.WriteLine();

            SerializeAppsToFile(StorageHelper.GetAllApps().Result, AppDataXml);
        }

        private static void SerializeAppsToFile(IEnumerable<AppEntity> apps, string filename)
        {
            var appData = apps.Select(a =>
                    new AppEntityData(a.SteamAppId, a.SteamName, a.AppType, a.Platforms, a.Categories.ToArray(),
                        a.Genres.ToArray(),
                        a.Developers.ToArray(), a.Publishers.ToArray(), a.ReleaseDate, a.MetacriticScore,
                        a.HltbId, a.HltbName, a.MainTtb, a.MainTtbImputed, a.ExtrasTtb, a.ExtrasTtbImputed, a.CompletionistTtb,
                        a.CompletionistTtbImputed, a.VerifiedGame))
                .ToArray();


            SerializeToFile(appData, filename);
        }

        private static void SerializeSuggestionsToFile(IEnumerable<SuggestionEntity> suggestions, string filename)
        {
            var suggestionsData = suggestions.Select(s => new SuggestionData(s.SteamAppId, s.HltbId, s.AppType)).ToArray();
            SerializeToFile(suggestionsData, filename);
        }

        private static void SerializeToFile<T>(T appData, string filename)
        {
            using (var stream = File.OpenWrite(filename))
            {
                new DataContractSerializer(typeof(T)).WriteObject(stream, appData);
            }
        }

        public static void LoadAllAppsFromFile()
        {
            Console.WriteLine("Loading apps from file - this will override all loaded games!!!");
            Console.Write("Are you sure? If so, type SURE (in capital letters): ");
            var input = Console.ReadLine();
            if (input != "SURE")
            {
                Console.WriteLine("You are not sure, aborting");
                return;
            }

            Console.WriteLine("Loading all apps from: " + Path.Combine(Environment.CurrentDirectory, AppDataXml));

            AppEntityData[] appData;
            using (var stream = File.OpenRead(AppDataXml))
            {
                appData = (AppEntityData[]) new DataContractSerializer(typeof (AppEntityData[])).ReadObject(stream);
            }

            StorageHelper.InsertOrReplace(appData.Select(
                a => new AppEntity(a.SteamAppId, a.SteamName, a.AppType, a.Platforms, a.Categories, a.Genres, a.Publishers, a.Developers, a.ReleaseDate, a.MetacriticScore)
                {
                    HltbId = a.HltbId,
                    HltbName = a.HltbName,
                    MainTtb = a.MainTtb,
                    MainTtbImputed = a.MainTtbImputed,
                    ExtrasTtb = a.ExtrasTtb,
                    ExtrasTtbImputed = a.ExtrasTtbImputed,
                    CompletionistTtb = a.CompletionistTtb,
                    CompletionistTtbImputed = a.CompletionistTtbImputed,
                    VerifiedGame = a.VerifiedGame
                }), "updating apps from file").Wait();
        }

        public static void PrintGenres()
        {
            var measured = StorageHelper.GetAllApps(AppEntity.MeasuredFilter).Result;

            int count = 0;
            
            foreach (var game in measured.Where(a => a.IsGame && a.Genres.First() == "Unknown"
            && (!a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed)))
            {
                PrintGame(game);
                count++;
            }
            Console.WriteLine(Invariant($"Total: {count}"));

            count = 0;
            foreach (var game in measured.Where(a => !a.IsGame && a.Genres.First() == "Racing"))
            {
                PrintGame(game);
                count++;
            }
            Console.WriteLine(Invariant($"Total: {count}"));

            count = 0;
            foreach (var game in measured.Where(a => !a.IsGame && a.Genres.First() == "Racing"
                && (!a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed)))
            {
                PrintGame(game);
                count++;
            }
            Console.WriteLine(Invariant($"Total (non imputed): {count}"));

            foreach (var genre in measured.Select(a => a.Genres.First()).Distinct())
            {
                Console.WriteLine(genre);
            }

            foreach (var app in measured.Where(a =>
                a.Genres.Contains("Unknown", StringComparer.Ordinal) ||
                a.Genres.Contains("Audio Production", StringComparer.Ordinal) ||
                a.Genres.Contains("Animation & Modeling", StringComparer.Ordinal) ||
                a.Genres.Contains("Design & Illustration", StringComparer.Ordinal) ||
                a.Genres.Contains("Utilities", StringComparer.Ordinal) ||
                a.Genres.Contains("Web Publishing", StringComparer.Ordinal) ||
                a.Genres.Contains("Video Production", StringComparer.Ordinal)).OrderBy(a => a.Genres.First()))
            {
                PrintGame(app);
            }

            foreach (var app in StorageHelper.GetAllApps().Result
                .Where(a => a.Genres.Contains("Massively Multiplayer", StringComparer.OrdinalIgnoreCase) && a.Measured && a.Categories.Contains("Single-player", StringComparer.OrdinalIgnoreCase)))
            {
                PrintGame(app);
            }
        }

        private static void PrintGame(AppEntity app)
        {
            Console.WriteLine("{10} ({11}) / {0} ({9}): {1}/{2}/{3} ({4}/{5}/{6}) | {7} | {8}",
                app.SteamName, app.MainTtb, app.ExtrasTtb, app.CompletionistTtb,
                app.MainTtbImputed, app.ExtrasTtbImputed, app.CompletionistTtbImputed,
                app.GenresFlat, app.CategoriesFlat, app.AppType, app.SteamAppId, app.HltbId);
        }

        public static void WriteAllMeasuredToTsv()
        {
            using (var writer = new StreamWriter("games.tsv"))
            {
                writer.WriteLine(
                    "SteamID\tSteamName\tType\tGenres\tCategories\tHltbID\tHltbName\tMain\tMainImputed\tExtras\tExtrasImputed\tCompletionist\tCompletionistImputed\tDevelopers\tPublishers\tPlatforms\tMetacritic\tReleaseDate");
                foreach (var app in StorageHelper.GetAllApps(AppEntity.MeasuredFilter).Result)
                {
                    writer.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                        RemoveTabs(app.SteamAppId),
                        RemoveTabs(app.SteamName),
                        RemoveTabs(app.AppType),
                        RemoveTabs(app.GenresFlat),
                        RemoveTabs(app.CategoriesFlat),
                        RemoveTabs(app.HltbId),
                        RemoveTabs(app.HltbName),
                        RemoveTabs(app.MainTtb),
                        RemoveTabs(app.MainTtbImputed),
                        RemoveTabs(app.ExtrasTtb),
                        RemoveTabs(app.ExtrasTtbImputed),
                        RemoveTabs(app.CompletionistTtb),
                        RemoveTabs(app.CompletionistTtbImputed),
                        RemoveTabs(app.DevelopersFlat),
                        RemoveTabs(app.PublishersFlat),
                        RemoveTabs(app.Platforms),
                        RemoveTabs(app.MetacriticScore),
                        RemoveTabs(app.ReleaseDate));
                }
            }
        }

        public static string RemoveTabs(object obj)
        {
            return obj?.ToString().Replace('\t', ';') ?? String.Empty;
        }

        public static void GetAppsFromCsv()
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
                int appId = int.Parse(parts[1], CultureInfo.InvariantCulture);
                int hltbId = int.Parse(parts[2], CultureInfo.InvariantCulture);

                games.Add(new AppEntity(appId, name, hltbId.ToString(CultureInfo.InvariantCulture)));
            }

            StorageHelper.Insert(games, "inserting apps from CSV");
        }
    }
}