using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using JetBrains.Annotations;
using MissingGamesUpdater.Updater;

namespace MissingGamesUpdater.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-MissingGamesUpdater")]
    public sealed class MissingUpdaterEventSource : EventSourceBase
    {
        public static readonly MissingUpdaterEventSource Log = new MissingUpdaterEventSource();
        private MissingUpdaterEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords SteamApi = (EventKeywords) 1;
            public const EventKeywords MissingGamesUpdater = (EventKeywords) 2;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask RetrieveAllSteamApps = (EventTask) 1;
            public const EventTask UpdateMissingGames = (EventTask) 2;
        }
// ReSharper restore ConvertToStaticClass

        [NonEvent]
        public void RetrieveAllSteamAppsStart(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveAllSteamAppsStart(uri.ToString());
        }

        [Event(
            1,
            Message = "Start retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveAllSteamApps,
            Opcode = EventOpcode.Start)]
        private void RetrieveAllSteamAppsStart(string uri)
        {
            WriteEvent(1, uri);
        }

        [NonEvent]
        public void RetrieveAllSteamAppsStop(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveAllSteamAppsStop(uri.ToString());
        }

        [Event(
            2,
            Message = "Finished retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveAllSteamApps,
            Opcode = EventOpcode.Stop)]
        private void RetrieveAllSteamAppsStop(string uri)
        {
            WriteEvent(2, uri);
        }

        [NonEvent]
        public void ErrorRetrievingAllSteamApps(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }
            
            ErrorRetrievingAllSteamApps(uri.ToString());
        }

        [Event(
            3,
            Message = "Null object received when retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Critical)]
        private void ErrorRetrievingAllSteamApps(string uri)
        {
            WriteEvent(3, uri);
        }

        [NonEvent]
        public void RetrievedAllSteamApps(Uri uri, int count)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrievedAllSteamApps(uri.ToString(), count);
        }

        [Event(
            4,
            Message = "Retrieved {1} apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational)]
        private void RetrievedAllSteamApps(string uri, int count)
        {
            WriteEvent(4, uri, count);
        }

        [Event(
            5,
            Message = "Start updating missing games",
            Keywords = Keywords.MissingGamesUpdater,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateMissingGames,
            Opcode = EventOpcode.Start)]
        public void UpdateMissingGamesStart()
        {
            WriteEvent(5);
        }

        [Event(
            6,
            Message = "Finished updating missing games",
            Keywords = Keywords.MissingGamesUpdater,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateMissingGames,
            Opcode = EventOpcode.Stop)]
        public void UpdateMissingGamesStop()
        {
            WriteEvent(6);
        }

        [NonEvent]
        public void MissingAppsDetermined([NotNull] App[] missingApps)
        {
            if (missingApps == null) throw new ArgumentNullException(nameof(missingApps));

            MissingAppsDetermined(missingApps.Length, StorageHelper.GetAppSummary(missingApps.Select(a => new AppEntity(a.appid, a.name, ""))));
        }

        [Event(
            7,
            Message = "Updating {0} missing apps:\n{1}",
            Keywords = Keywords.MissingGamesUpdater,
            Level = EventLevel.Informational)]
        public void MissingAppsDetermined(int count, string appSummary)
        {
            WriteEvent(7, count, appSummary);
        }
    }
}
