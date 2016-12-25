using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Util;
using SuggestionWatcher.Logging;

namespace SuggestionWatcher.Watcher
{
    class SuggestionWatcher
    {
        static void Main()
        {
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("SuggestionWatcher");
                WatchForSuggestions().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task WatchForSuggestions()
        {
            var ticks = Environment.TickCount;
            SuggestionWatcherEventSource.Log.WatchSuggestionsStart();

            var suggestionsTask = StorageHelper.GetAllSuggestions();
            var processedSuggestionsTask = StorageHelper.GetAllProcessedSuggestions();

            await Task.WhenAll(suggestionsTask, processedSuggestionsTask).ConfigureAwait(false);

            var allProcessedSuggestionsS = new HashSet<ProcessedSuggestionEntity>(processedSuggestionsTask.Result);
            var unprocessedSuggestions = suggestionsTask.Result.Where(s => !allProcessedSuggestionsS.Contains(new ProcessedSuggestionEntity(s))).ToArray();

            var pendingSuggestionsCount = unprocessedSuggestions.Length;

            await SiteUtil.SendSuccessMail(
                "Suggestion Watcher",
                pendingSuggestionsCount > 0 ? pendingSuggestionsCount + " suggestion(s) pending" : "no pending suggestions", ticks).ConfigureAwait(false);

            SuggestionWatcherEventSource.Log.WatchSuggestionsStop(pendingSuggestionsCount);
        }
    }
}
