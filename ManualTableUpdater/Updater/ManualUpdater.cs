using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Common.Entities;
using Common.Logging;
using Common.Storage;
// ReSharper disable UnusedMember.Local

namespace ManualTableUpdater.Updater
{
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), DataMember]
        public string[] Categories { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), DataMember]
        public string[] Genres { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays"), DataMember]
        public string[] Developers { get; set; }
        [DataMember]
        public string[] Publishers { get; set; }
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

        public AppEntityData(int steamAppId, string steamName, string appType, Platforms platforms,
            string[] categories, string[] genres, string[] developers,
            string[] publishers, DateTime releaseDate, int metacriticScore,
            int hltbId, string hltbName, 
            int mainTtb, bool mainTtbImputed, int extrasTtb, bool extrasTtbImputed, int completionistTtb, bool completionistTtbImputed)
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
        }
    }
    class ManualUpdater
    {
        private const string AppdataXml = "AppData.xml";

        static void Main()
        {
            try
            {
                //SerializeAllAppsToFile();
                //LoadAllAppsFromFile();
                Console.WriteLine("Done - Press any key to continue...");
                Console.ReadLine();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static void SerializeAllAppsToFile()
        {
            var appData = StorageHelper.GetAllApps().Result.Select(a =>
                new AppEntityData(a.SteamAppId, a.SteamName, a.AppType, a.Platforms, a.Categories.ToArray(),
                    a.Genres.ToArray(),
                    a.Developers.ToArray(), a.Publishers.ToArray(), a.ReleaseDate, a.MetacriticScore, 
                    a.HltbId, a.HltbName, a.MainTtb, a.MainTtbImputed, a.ExtrasTtb, a.ExtrasTtbImputed, a.CompletionistTtb, a.CompletionistTtbImputed))
                    .ToArray();

            using (var stream = File.OpenWrite(AppdataXml))
            {
                new DataContractSerializer(typeof(AppEntityData[])).WriteObject(stream, appData);
            }
        }

        private static void LoadAllAppsFromFile()
        {
            AppEntityData[] appData;
            using (var stream = File.OpenRead(AppdataXml))
            {
                appData = (AppEntityData[]) new DataContractSerializer(typeof (AppEntityData[])).ReadObject(stream);
            }

            StorageHelper.InsertOrReplace(appData.Select(
                a => new AppEntity(a.SteamAppId, a.SteamName, a.AppType, a.Platforms, a.Categories,
                    a.Genres, a.Publishers, a.Developers, a.ReleaseDate, a.MetacriticScore)
                {
                    HltbId = a.HltbId,
                    HltbName = a.HltbName,
                    MainTtb = a.MainTtb,
                    MainTtbImputed = a.MainTtbImputed,
                    ExtrasTtb = a.ExtrasTtb,
                    ExtrasTtbImputed = a.ExtrasTtbImputed,
                    CompletionistTtb = a.CompletionistTtb,
                    CompletionistTtbImputed = a.CompletionistTtbImputed
                }), "updating apps from file").Wait();
        }

        private static void PrintGenres()
        {
            var measured = StorageHelper.GetAllApps(AppEntity.MeasuredFilter).Result;
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
                Console.WriteLine("{0}: {1} | {2}", app.SteamName, app.GenresFlat, app.CategoriesFlat);
            }

            foreach (var app in StorageHelper.GetAllApps().Result
                .Where(a => a.Genres.Contains("Massively Multiplayer", StringComparer.OrdinalIgnoreCase) && a.Measured && a.Categories.Contains("Single-player", StringComparer.OrdinalIgnoreCase)))
            {
                Console.WriteLine("{10} / {0} ({9}): {1}/{2}/{3} ({4}/{5}/{6}) | {7} | {8}",
                    app.SteamName, app.MainTtb, app.ExtrasTtb, app.CompletionistTtb,
                    app.MainTtbImputed, app.ExtrasTtbImputed, app.CompletionistTtbImputed,
                    app.GenresFlat, app.CategoriesFlat, app.AppType, app.SteamAppId);
            }
        }

        private static void WriteAllMeasuredToTsv()
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
            return obj == null ? String.Empty : obj.ToString().Replace('\t', ';');
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
                int appId = int.Parse(parts[1]);
                int hltbId = int.Parse(parts[2]);

                games.Add(new AppEntity(appId, name, hltbId.ToString()));
            }

            StorageHelper.Insert(games, "inserting apps from CSV");
        }
    }
}
