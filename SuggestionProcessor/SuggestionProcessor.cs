using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Util;
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
                Console.WriteLine("Press any key to continue...");
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

            public string HltbName { get; private set; }

            public SuggestionInfo(SuggestionEntity suggestion, string hltbName)
            {
                Suggestion = suggestion;
                HltbName = hltbName;
            }
        }

        private static async Task ProcessSuggestions()
        {
            Console.WriteLine("Retrieving suggestions and apps...");

            var suggestions = new ConcurrentBag<SuggestionInfo>();
            var suggestionsTask = StorageHelper.GetAllSuggestions().ContinueWith(t =>
            {
                Console.WriteLine("Scraping HLTB suggestion names...");
                return t.Result.ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async suggestion =>
                {
                    string hltbName;
                    try
                    {
                        hltbName = await HltbScraper.ScrapeWithExponentialRetries(HltbScraper.ScrapeHltbName, suggestion.HltbId).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error scraping HLTB ID {0} suggested for Steam ID {1}: {2}", suggestion.HltbId, suggestion.SteamAppId, e);
                        return;
                    }
                    suggestions.Add(new SuggestionInfo(suggestion, hltbName));
                });
            }).Unwrap();

            var apps = new Dictionary<long, AppEntity>();
            var appsTask = StorageHelper.GetAllApps(a => a, AppEntity.MeasuredFilter).ContinueWith(t =>
            {
                Console.WriteLine("Populating apps dictionary...");
                foreach (var app in t.Result)
                {
                    apps[app.SteamAppId] = app;
                }
            });

            await Task.WhenAll(suggestionsTask, appsTask);

            int i = 0;
            foreach (var suggestion in suggestions)
            {
                i++;

                AppEntity app;
                if (!apps.TryGetValue(suggestion.Suggestion.SteamAppId, out app))
                {
                    Console.WriteLine("ERROR: suggestion for unknown Steam ID {0} (Suggested HLTB ID {1})", 
                        suggestion.Suggestion.SteamAppId, suggestion.Suggestion.HltbId);
                    continue;
                }

                Console.WriteLine("(#{0}/{1}) {2} ({3}) | Current: {4} ({5}) | Suggested: {6} ({7})",
                    i, suggestions.Count, app.SteamName, app.SteamAppId, app.HltbName, app.HltbId, suggestion.HltbName, suggestion.Suggestion.HltbId);

                while (true)
                {
                    Console.Write("Accept suggestion (Y/N)? ");
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
    }
}
