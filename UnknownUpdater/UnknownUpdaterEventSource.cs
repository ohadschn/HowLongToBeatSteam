using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using Common;

namespace UnknownUpdater
{
    public interface IUnknownUpdaterEventSource
    {
        void UpdateUnknownAppsStart();
        void UpdateUnknownAppsStop();
        void UpdateNewlyCategorizedApps(ConcurrentBag<AppEntity> updates);
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-UnknownGamesUpdater")]
    public class UnknownUpdaterEventSource : EventSource, IUnknownUpdaterEventSource
    {
        public static readonly IUnknownUpdaterEventSource Log = new UnknownUpdaterEventSource();
        private UnknownUpdaterEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() {}
            public const EventKeywords UnknownUpdater = (EventKeywords) 1;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask UpdateUnknownApps = (EventTask) 1;
        }
// ReSharper restore ConvertToStaticClass

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
            if (updates == null)
            {
                throw new ArgumentNullException("updates");
            }
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
