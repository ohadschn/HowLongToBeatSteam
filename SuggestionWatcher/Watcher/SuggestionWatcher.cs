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
            var ticks = Environment.TickCount;
            SuggestionWatcherEventSource.Log.WatchSuggestionsStart();

            var suggestions = await StorageHelper.GetAllSuggestions().ConfigureAwait(false);
            var pendingSuggestions = suggestions.Count;

            await SiteUtil.SendSuccessMail(
                "Suggestion Watcher",
                pendingSuggestions > 0 ? pendingSuggestions + " suggestion(s) pending" : "no pending suggestions", ticks).ConfigureAwait(false);

            SuggestionWatcherEventSource.Log.WatchSuggestionsStop(pendingSuggestions);
        }
    }
}
