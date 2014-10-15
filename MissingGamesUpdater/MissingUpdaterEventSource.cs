using System;
using System.Diagnostics.Tracing;

namespace MissingGamesUpdater
{
    public interface IMissingUpdaterEventSource
    {
        void RetrieveAllSteamAppsStart(Uri uri);
        void RetrieveAllSteamAppsStop(Uri uri);
        void ErrorRetrievingAllSteamApps(Uri uri);
        void RetrievedAllSteamApps(Uri uri, int count);
        void UpdateMissingGamesStart();
        void UpdateMissingGamesStop();
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-MissingGamesUpdater")]
    public class MissingUpdaterEventSource : EventSource, IMissingUpdaterEventSource
    {
        public static readonly IMissingUpdaterEventSource Log = new MissingUpdaterEventSource();
        private MissingUpdaterEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords SteamApi = (EventKeywords) 1;
            public const EventKeywords MissingGamesUpdater = (EventKeywords) 2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
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
                throw new ArgumentNullException("uri");
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
                throw new ArgumentNullException("uri");
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
                throw new ArgumentNullException("uri");
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
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrievedAllSteamApps(uri.ToString(), count);
        }

        [Event(
            4,
            Message = "Retrieved {1} games from {0}",
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
    }
}
