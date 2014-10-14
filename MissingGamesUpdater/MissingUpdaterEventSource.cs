using System.Diagnostics.Tracing;

namespace MissingGamesUpdater
{
    [EventSource(Name = "OS-HowLongToBeatSteam-MissingGamesUpdater")]
    public class MissingUpdaterEventSource : EventSource
    {
        public static readonly MissingUpdaterEventSource Log = new MissingUpdaterEventSource();
        private MissingUpdaterEventSource()
        {
        }

        public class Keywords
        {
            public const EventKeywords SteamApi = (EventKeywords) 1;
            public const EventKeywords MissingGamesUpdater = (EventKeywords) 2;
        }

        public class Tasks
        {
            public const EventTask RetrieveAllSteamApps = (EventTask) 1;
            public const EventTask UpdateMissingGames = (EventTask) 2;
        }

        [Event(
            1,
            Message = "Start retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveAllSteamApps,
            Opcode = EventOpcode.Start)]
        public void RetrieveAllSteamAppsStart(string getSteamAppListUrl)
        {
            WriteEvent(1, getSteamAppListUrl);
        }

        [Event(
            2,
            Message = "Finished retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveAllSteamApps,
            Opcode = EventOpcode.Stop)]
        public void RetrieveAllSteamAppsStop(string getSteamAppListUrl)
        {
            WriteEvent(2, getSteamAppListUrl);
        }

        [Event(
            3,
            Message = "Null object received when retrieving list of all Steam apps from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Critical)]
        public void ErrorRetrievingAllSteamApps(string getSteamAppListUrl)
        {
            WriteEvent(3, getSteamAppListUrl);
        }

        [Event(
            4,
            Message = "Retrieved {1} games from {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational)]
        public void RetrievedAllSteamApps(string getSteamAppListUrl, int count)
        {
            WriteEvent(4, getSteamAppListUrl, count);
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
