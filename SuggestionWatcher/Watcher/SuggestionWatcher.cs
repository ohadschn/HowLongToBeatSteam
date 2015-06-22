using System;
using System.Threading.Tasks;
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
            var tickCount = Environment.TickCount;
            SuggestionWatcherEventSource.Log.WatchSuggestionsStart();

            var suggestions = await StorageHelper.GetAllSuggestions();
            var pendingSuggestions = suggestions.Count;

            await SiteUtil.SendSuccessMail(
                "Suggestion Watcher", SiteUtil.GetTimeElapsedFromTickCount(tickCount),
                pendingSuggestions > 0 ? pendingSuggestions + " suggestion(s) pending" : "no pending suggestions");

            SuggestionWatcherEventSource.Log.WatchSuggestionsStop(pendingSuggestions);
        }
    }
}
