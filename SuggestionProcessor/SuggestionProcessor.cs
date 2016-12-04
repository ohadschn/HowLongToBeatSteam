using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private const string SteamStoreGamePageTemplate = "http://store.steampowered.com/app/{0}";
        
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
            public SuggestionEntity Suggestion { get; }
            public AppEntity App { get; }
            public int OriginalHltbId { get; }
            public string OriginalHltbName { get; }

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
            var allMeasuredAppsTask = StorageHelper.GetAllApps(AppEntity.MeasuredFilter);
            var suggestionsTask = StorageHelper.GetAllSuggestions();
            var processedSuggestionsTask = StorageHelper.GetAllProcessedSuggestions();

            await Task.WhenAll(allMeasuredAppsTask, suggestionsTask, processedSuggestionsTask).ConfigureAwait(false);

            Console.WriteLine("Building app dictionary");
            var allMeasuredApps = allMeasuredAppsTask.Result;
            var allMeasuredAppsMap = allMeasuredApps.ToDictionary(a => a.SteamAppId);
            var allMeasuredAppsMapForImputation = allMeasuredApps.ToDictionary(ae => ae.SteamAppId, ae => ae.ShallowClone());

            var allProcessedSuggestions = processedSuggestionsTask.Result;
            var allProcessedSuggestionsSet = new HashSet<ProcessedSuggestionEntity>(allProcessedSuggestions, new ProcessedSuggestionComparer());

            //only consider the first suggestion for each app per run
            var suggestions = suggestionsTask.Result.Where(s => !allProcessedSuggestionsSet.Contains(new ProcessedSuggestionEntity(s))).Distinct(new SuggestionSteamIdComparer()).ToArray(); 

            var validSuggestions = new ConcurrentDictionary<int, SuggestionInfo>(
                GetSuggestionsForKnownAndPrepareApps(allMeasuredAppsMap, suggestions).ToDictionary(si => si.App.SteamAppId));

            var invalidSuggestions = new ConcurrentBag<SuggestionInfo>(
                suggestions.Where(s => !validSuggestions.ContainsKey(s.SteamAppId))
                    .Select(s => new SuggestionInfo(s, new AppEntity(-1, "[Unknown]", "[Unknown]"), -1, "[Unknown]")));

            Console.WriteLine("Scraping HLTB info for suggestions...");
            await HltbScraper.ScrapeHltb(validSuggestions.Values.Where(s => !s.Suggestion.IsRetype).Select(s => s.App).ToArray(), 
                (a, e) =>
                {
                    Console.WriteLine("Can't parse HLTB ID {0} suggested for {1} ({2}): {3}", a.HltbId, a.SteamName, a.SteamAppId, e);
                    SuggestionInfo invalidSuggestion;
                    bool removed = validSuggestions.TryRemove(a.SteamAppId, out invalidSuggestion);
                    Trace.Assert(removed, "Invalid validSuggestions state");
                    invalidSuggestions.Add(invalidSuggestion);
                }).ConfigureAwait(false);

            Console.WriteLine("Removing invalid suggestions...");
            RemoveInvalidSuggestions(invalidSuggestions);

            Console.WriteLine("Processing suggestions...");

            var acceptedHltbSuggestions = await ProcessSuggestionsByUserInput(validSuggestions.Values).ConfigureAwait(false);
            if (acceptedHltbSuggestions.Count > 0)
            {
                foreach (var suggestion in acceptedHltbSuggestions)
                {
                    allMeasuredAppsMapForImputation[suggestion.Suggestion.SteamAppId] = suggestion.App;
                }

                Console.WriteLine("Imputing missing TTBs from genre stats...");
                await Imputer.Impute(allMeasuredAppsMapForImputation.Values.ToArray()).ConfigureAwait(false);

                Console.Write("Updating HLTB suggestions... ");
                foreach (var suggestion in acceptedHltbSuggestions)
                {
                    await StorageHelper.AcceptSuggestion(suggestion.App, suggestion.Suggestion).ConfigureAwait(false);
                } 
            }

            Console.WriteLine("All Done!");
        }

        private static List<SuggestionInfo> GetSuggestionsForKnownAndPrepareApps(IReadOnlyDictionary<int, AppEntity> appsMap, IEnumerable<SuggestionEntity> suggestions)
        {
            var suggestionsForKnownApps = new List<SuggestionInfo>();
            foreach (var suggestion in suggestions)
            {
                AppEntity app;
                if (!appsMap.TryGetValue(suggestion.SteamAppId, out app))
                {
                    Console.WriteLine("ERROR: suggestion for unknown Steam ID {0} (Suggested HLTB ID {1})", suggestion.SteamAppId, suggestion.HltbId);
                    continue;
                }

                suggestionsForKnownApps.Add(new SuggestionInfo(suggestion, app, app.HltbId, app.HltbName));

                if (!suggestion.IsRetype)
                {
                    app.HltbId = suggestion.HltbId; //scarping will now take place for the suggested ID
                    app.HltbName = "[Unknown]";
                    app.SetMainTtb(0, true);
                    app.SetExtrasTtb(0, true);
                    app.SetCompletionistTtb(0, true);
                }
            }
            return suggestionsForKnownApps;
        }

        private static void RemoveInvalidSuggestions(ConcurrentBag<SuggestionInfo> invalidSuggestions)
        {
            int i = 0;
            foreach (var invalidSuggestion in invalidSuggestions)
            {
                i++;
                PrintSuggestion(invalidSuggestion, i, invalidSuggestions.Count);

                while (true)
                {
                    Console.Write("Remove invalid suggestions? (Y)es / (N)o / (I)nspect ");
                    var key = Console.ReadKey();
                    Console.WriteLine();

                    if (key.KeyChar == 'i' || key.KeyChar == 'I')
                    {
                        if (invalidSuggestion.App.SteamAppId == -1)
                        {
                            Console.WriteLine("Unknown Steam ID - nothing to inspect");
                        }
                        else
                        {
                            InspectSuggestion(invalidSuggestion);                            
                        }
                        continue;
                    }
                    if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                    {
                        Console.WriteLine("Removing suggestion...");
                        StorageHelper.DeleteSuggestion(invalidSuggestion.Suggestion).Wait();
                        break;
                    }
                    if (key.KeyChar == 'n' || key.KeyChar == 'N')
                    {
                        Console.WriteLine("Keeping invalid suggestions");
                        break;
                    }
                }
            }
        }

        private static async Task<IList<SuggestionInfo>>  ProcessSuggestionsByUserInput(ICollection<SuggestionInfo> validSuggestions)
        {
            var acceptedHltbSuggestions = new List<SuggestionInfo>();

            int i = 0;
            foreach (var suggestion in validSuggestions)
            {
                i++;
                PrintSuggestion(suggestion, i, validSuggestions.Count);

                while (true)
                {
                    Console.Write("Accept suggestion? (A)ccept / (D)elete {0}/ (I)nspect / (S)kip ", suggestion.Suggestion.IsRetype ? "/ (V)erify game and delete " : String.Empty);
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    if (key.KeyChar == 'a' || key.KeyChar == 'A')
                    {
                        if (suggestion.Suggestion.IsRetype)
                        {
                            Console.WriteLine("Accepting retype suggestion...");
                            await StorageHelper.AcceptSuggestion(suggestion.App, suggestion.Suggestion).ConfigureAwait(false);
                        }
                        else
                        {
                            Console.WriteLine("Marking suggestion as accepted (will accept after imputation)...");
                            acceptedHltbSuggestions.Add(suggestion);
                        }
                        break;
                    }
                    if (key.KeyChar == 'd' || key.KeyChar == 'D')
                    {
                        Console.Write("Deleting suggestion... ");
                        await StorageHelper.DeleteSuggestion(suggestion.Suggestion).ConfigureAwait(false);
                        Console.WriteLine("Done!");
                        break;
                    }
                    if (suggestion.Suggestion.IsRetype && (key.KeyChar == 'v' || key.KeyChar == 'V'))
                    {
                        Console.WriteLine("Verifying game and deleting suggestion...");
                        await StorageHelper.DeleteSuggestion(suggestion.Suggestion, suggestion.App).ConfigureAwait(false);
                        Console.WriteLine("Done");
                        break;
                    }
                    if (key.KeyChar == 'i' || key.KeyChar == 'I')
                    {
                        InspectSuggestion(suggestion);
                        continue;
                    }
                    if (key.KeyChar == 's' || key.KeyChar == 'S')
                    {
                        Console.WriteLine("Skipping...");
                        break;
                    }
                }
            }
            
            return acceptedHltbSuggestions;
        }

        private static void InspectSuggestion(SuggestionInfo suggestion)
        {
            Console.WriteLine("Launching Steam game page...");
            Process.Start(String.Format(CultureInfo.InvariantCulture, SteamStoreGamePageTemplate, suggestion.App.SteamAppId));

            if (suggestion.App.HltbId >= 0)
            {
                Console.WriteLine("Launching suggested HLTB game page...");
                Process.Start(String.Format(CultureInfo.InvariantCulture, HltbScraper.HltbGamePageFormat, suggestion.App.HltbId));   
            }
            else if (suggestion.Suggestion.IsRetype && suggestion.OriginalHltbId >= 0)
            {
                Console.WriteLine("Launching HLTB game page...");
                Process.Start(String.Format(CultureInfo.InvariantCulture, HltbScraper.HltbGamePageFormat, suggestion.OriginalHltbId));
            }
        }

        private static void PrintSuggestion(SuggestionInfo suggestion, int index, int count)
        {
            Console.WriteLine("[#{0}/{1}] {2} ({3}) | Current: {4} ({5}) | Suggested: {6}",
                index, count,
                suggestion.App.SteamName, suggestion.App.SteamAppId,
                suggestion.OriginalHltbName, suggestion.OriginalHltbId,
                suggestion.Suggestion.AppType == AppEntity.NonGameTypeName
                    ? "[non-finite game/app]"
                    : (String.Format(CultureInfo.InvariantCulture, "{0} ({1})", suggestion.App.HltbName, suggestion.App.HltbId)) +
                      (suggestion.Suggestion.IsRetype
                          ? String.Format(CultureInfo.InvariantCulture, " [{0}]", suggestion.Suggestion.AppType)
                          : String.Empty));
        }

        class SuggestionSteamIdComparer : IEqualityComparer<SuggestionEntity>
        {
            public bool Equals(SuggestionEntity x, SuggestionEntity y)
            {
                return x?.SteamAppId == y?.SteamAppId;
            }

            public int GetHashCode(SuggestionEntity obj)
            {
                return obj?.SteamAppId ?? 0;
            }
        }

        class ProcessedSuggestionComparer : IEqualityComparer<ProcessedSuggestionEntity>
        {
            public bool Equals(ProcessedSuggestionEntity x, ProcessedSuggestionEntity y)
            {
                return 
                    x?.SteamAppId ==    y?.SteamAppId && 
                    x?.HltbId ==        y?.HltbId && 
                    x?.AppType ==       y?.AppType;
            }

            public int GetHashCode(ProcessedSuggestionEntity processedSuggestion)
            {
                unchecked
                {
                    var hashCode = processedSuggestion?.SteamAppId;
                    hashCode = (hashCode * 397) ^ processedSuggestion?.HltbId;
                    hashCode = (hashCode * 397) ^ (processedSuggestion?.AppType?.GetHashCode() ?? 0);
                    return hashCode ?? 0;
                }
            }
        }
    }
}