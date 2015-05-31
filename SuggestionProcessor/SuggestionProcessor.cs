﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using SteamHltbScraper.Imputation;
using SteamHltbScraper.Scraper;

namespace SuggestionProcessor
{
    class SuggestionProcessor
    {
        static void Main()
        {
            try
            {
                ProcessSuggestions().Wait();
                Console.WriteLine("Press [return] to terminate...");
                Console.ReadLine();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();                
            }
        }

        class SuggestionInfo
        {
            public SuggestionEntity Suggestion { get; private set; }
            public AppEntity App { get; private set; }
            public int OriginalHltbId { get; private set; }
            public string OriginalHltbName { get; private set; }

            public SuggestionInfo(SuggestionEntity suggestion, AppEntity app, int originalHltbId, string originalHltbName)
            {
                Suggestion = suggestion;
                App = app;
                OriginalHltbId = originalHltbId;
                OriginalHltbName = originalHltbName;
            }
        }

        private static async Task ProcessSuggestions()
        {
            Console.WriteLine("Retrieving all apps and suggestions...");
            var allAppsTask = StorageHelper.GetAllApps(AppEntity.MeasuredFilter);
            var suggestionsTask = StorageHelper.GetAllSuggestions();

            await Task.WhenAll(allAppsTask, suggestionsTask).ConfigureAwait(false);

            Console.WriteLine("Building app dictionary");
            var appsMap = allAppsTask.Result.ToDictionary(a => a.SteamAppId);

            var suggestions = suggestionsTask.Result.Distinct(new SuggestionSteamIdComparer()); //only consider the first suggestion for each app
            var validSuggestions = 
                new ConcurrentDictionary<int, SuggestionInfo>(GetValidSuggestions(appsMap, suggestions).ToDictionary(si => si.App.SteamAppId));
            var validSuggestedApps = validSuggestions.Values.Select(s => s.App).ToArray();

            Console.WriteLine("Scraping HLTB info for suggestions...");
            SuggestionInfo temp;
            await HltbScraper.ScrapeHltb(validSuggestedApps, (a,e) =>
            {
                Console.WriteLine("Can't parse HLTB ID {0} suggested for {1} ({2}): {3}", a.HltbId, a.SteamName, a.SteamAppId, e);
                bool removed = validSuggestions.TryRemove(a.SteamAppId, out temp);
                Trace.Assert(removed, "Invalid validSuggestions state");
            }).ConfigureAwait(false);

            Console.WriteLine("Imputing missing TTBs from genre stats...");
            await Imputer.ImputeFromStats(validSuggestedApps).ConfigureAwait(false);

            Console.WriteLine("Processing suggestions...");
            await ProcessSuggestionsByUserInput(validSuggestions.Values).ConfigureAwait(false);
        }

        private static List<SuggestionInfo> GetValidSuggestions(IReadOnlyDictionary<int, AppEntity> appsMap, IEnumerable<SuggestionEntity> suggestions)
        {
            var existingSuggestions = new List<SuggestionInfo>();
            foreach (var suggestion in suggestions)
            {
                AppEntity app;
                if (!appsMap.TryGetValue(suggestion.SteamAppId, out app))
                {
                    Console.WriteLine("ERROR: suggestion for unknown Steam ID {0} (Suggested HLTB ID {1})", suggestion.SteamAppId, suggestion.HltbId);
                    continue;
                }

                existingSuggestions.Add(new SuggestionInfo(suggestion, app, app.HltbId, app.HltbName));
                
                app.HltbId = suggestion.HltbId; //scarping will now take place for the suggested ID
                app.SetMainTtb(0, true);
                app.SetExtrasTtb(0, true);
                app.SetCompletionistTtb(0, true);
            }
            return existingSuggestions;
        }

        private static async Task ProcessSuggestionsByUserInput(ICollection<SuggestionInfo> validSuggestions)
        {
            int i = 0;
            foreach (var suggestion in validSuggestions)
            {
                var app = suggestion.App;
                Console.WriteLine("[#{0}/{1}] {2} ({3}) | Current: {4} ({5}) | Suggested: {6} ({7})",
                    ++i, validSuggestions.Count,
                    app.SteamName, app.SteamAppId,
                    suggestion.OriginalHltbName, suggestion.OriginalHltbId,
                    app.HltbName, app.HltbId);

                while (true)
                {
                    Console.Write("Accept suggestion? (Y/N) ");
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                    {
                        Console.Write("Updating... ");
                        await StorageHelper.AcceptSuggestion(app, suggestion.Suggestion);
                        Console.WriteLine("Done!");
                        break;
                    }
                    if (key.KeyChar == 'n' || key.KeyChar == 'N')
                    {
                        Console.Write("Deleting suggestion... ");
                        await StorageHelper.DeleteSuggestion(suggestion.Suggestion);
                        Console.WriteLine("Done!");
                        break;
                    }
                }
            }
        }

        class SuggestionSteamIdComparer : IEqualityComparer<SuggestionEntity>
        {
            public bool Equals(SuggestionEntity x, SuggestionEntity y)
            {
                return x.SteamAppId == y.SteamAppId;
            }

            public int GetHashCode(SuggestionEntity obj)
            {
                return obj.SteamAppId;
            }
        }
    }
}