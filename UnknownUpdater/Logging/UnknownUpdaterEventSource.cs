using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Entities;
using Common.Logging;
using Common.Storage;

namespace UnknownUpdater.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-UnknownGamesUpdater")]
    public sealed class UnknownUpdaterEventSource : EventSourceBase
    {
        public static readonly UnknownUpdaterEventSource Log = new UnknownUpdaterEventSource();
        private UnknownUpdaterEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() {}
            public const EventKeywords UnknownUpdater = (EventKeywords) 1;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
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
                throw new ArgumentNullException(nameof(updates));
            }
            if (!IsEnabled())
            {
                return;
            }

            UpdateNewlyCategorizedApps(updates.Count, StorageHelper.GetAppSummary(updates));
        }

        [Event(
            3,
            Message = "Updating {0} newly categorized apps:\n{1}",
            Keywords = Keywords.UnknownUpdater,
            Level = EventLevel.Informational)]
        private void UpdateNewlyCategorizedApps(int count, string appSummary)
        {
            WriteEvent(3, count, appSummary);
        }
    }
}
