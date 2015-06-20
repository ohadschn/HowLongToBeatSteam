using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace SuggestionWatcher.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-SuggestionWatcher")]
    public class SuggestionWatcherEventSource : EventSourceBase
    {
        public static readonly SuggestionWatcherEventSource Log = new SuggestionWatcherEventSource();
        private SuggestionWatcherEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords SuggestionWatcher = (EventKeywords) 1;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() {}
            public const EventTask WatchSuggestions = (EventTask) 1;
        }
        // ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start watching for pending suggestions",
            Keywords = Keywords.SuggestionWatcher,
            Level = EventLevel.Informational,
            Task = Tasks.WatchSuggestions,
            Opcode = EventOpcode.Start)]
        public void WatchSuggestionsStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished watching for pending suggestions: {0} found",
            Keywords = Keywords.SuggestionWatcher,
            Level = EventLevel.Informational,
            Task = Tasks.WatchSuggestions,
            Opcode = EventOpcode.Stop)]
        public void WatchSuggestionsStop(int pendingSuggestions)
        {
            WriteEvent(2, pendingSuggestions);
        }
    }
}
