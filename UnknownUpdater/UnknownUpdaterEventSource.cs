using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using Common;

namespace UnknownUpdater
{
    [EventSource(Name = "OS-HowLongToBeatSteam-UnknownGamesUpdater")]
    public class UnknownUpdaterEventSource : EventSource
    {
        public static readonly UnknownUpdaterEventSource Log = new UnknownUpdaterEventSource();
        private UnknownUpdaterEventSource()
        {
        }

        public class Keywords
        {
            public const EventKeywords UnknownUpdater = (EventKeywords) 1;
        }

        public class Tasks
        {
            public const EventTask UpdateUnknownApps = (EventTask) 1;
        }

        [Event(
            1,
            Message = "Start updating unknown apps",
            Keywords = Keywords.UnknownUpdater,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateUnknownApps,
            Opcode = EventOpcode.Start)]
        public void UpdateUnknownAppsStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished updating unknown apps",
            Keywords = Keywords.UnknownUpdater,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateUnknownApps,
            Opcode = EventOpcode.Stop)]
        public void UpdateUnknownAppsStop()
        {
            WriteEvent(2);
        }

        [NonEvent]
        public void UpdateNewlyCategorizedApps(ConcurrentBag<AppEntity> updates)
        {
            if (!IsEnabled())
            {
                return;
            }

            UpdateNewlyCategorizedApps(
                String.Join(", ", updates.Select(ae => String.Format(CultureInfo.InvariantCulture, "{0} / {1}", ae.SteamAppId, ae.SteamName))));
        }

        [Event(
            3,
            Message = "Updating newly categorized apps: {0}")]
        private void UpdateNewlyCategorizedApps(string apps)
        {
            WriteEvent(3, apps);
        }
    }
}
