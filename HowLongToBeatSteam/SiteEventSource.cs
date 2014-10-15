using System.Diagnostics.Tracing;

namespace HowLongToBeatSteam
{
    public interface ISiteEventSource
    {
        void UpdateCacheStart();
        void UpdateCacheStop(int count);
        void RetrieveOwnedGamesStart(long steamId);
        void RetrieveOwnedGamesStop(long steamId);
        void ErrorRetrievingOwnedGames(long steamId);
        void ErrorRetrievingPersonaName(long steamId);
        void PrepareResponseStart();
        void PrepareResponseStop();
        void RetrievePlayerSummaryStart();
        void RetrievePlayerSummaryStop();
        void SkipNonCachedApp(int appId, string name);
        void SkipNonGame(int appId, string name);
        void RetrievedOwnedGames(long steamId, int count);
        void ResolvedPersonaName(long steamId, string personaName);
        void HandleGetGamesRequestStart(long steamId);
        void HandleGetGamesRequestStop(long steamId);
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-Site")]
    public sealed class SiteEventSource : EventSource, ISiteEventSource
    {
        private static readonly SiteEventSource s_log = new SiteEventSource();
        public static ISiteEventSource Log { get { return s_log; } }
        private SiteEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords SteamApi = (EventKeywords)1;
            public const EventKeywords TableStorage = (EventKeywords) 2;
            public const EventKeywords GameController = (EventKeywords) 4;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask UpdateCache = (EventTask) 1;
            public const EventTask RetrieveOwnedGames = (EventTask) 2;
            public const EventTask PrepareResponse = (EventTask) 3;
            public const EventTask RetrievePlayerSummary = (EventTask) 4;
            public const EventTask HandleGetGamesRequest = (EventTask) 5;
        }
// ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start querying table storage for all apps to update cache",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateCache,
            Opcode = EventOpcode.Start)]
        public void UpdateCacheStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished querying table storage for all apps to update cache - count: {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateCache,
            Opcode = EventOpcode.Stop)]
        public void UpdateCacheStop(int count)
        {
            WriteEvent(2, count);
        }

        [Event(
            3,
            Message = "Start retrieving all owned games for user ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveOwnedGames,
            Opcode = EventOpcode.Start)]
        public void RetrieveOwnedGamesStart(long steamId)
        {
            WriteEvent(3, steamId);
        }

        [Event(
            4,
            Message = "Finished retrieving all owned games for user ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveOwnedGames,
            Opcode = EventOpcode.Stop)]
        public void RetrieveOwnedGamesStop(long steamId)
        {
            WriteEvent(4, steamId);
        }

        [Event(
            5,
            Message = "Error retrieving owned games for user ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Warning)]
        public void ErrorRetrievingOwnedGames(long steamId)
        {
            WriteEvent(5, steamId);
        }

        [Event(
            6,
            Message = "Error retrieving player summary for user ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Warning)]
        public void ErrorRetrievingPersonaName(long steamId)
        {
            WriteEvent(6, steamId);
        }

        [Event(
            7,
            Message = "Start preparing response",
            Keywords = Keywords.GameController,
            Level = EventLevel.Informational,
            Task = Tasks.PrepareResponse,
            Opcode = EventOpcode.Start)]
        public void PrepareResponseStart()
        {
            WriteEvent(7);
        }

        [Event(
            8,
            Message = "Finished preparing response",
            Keywords = Keywords.GameController,
            Level = EventLevel.Informational,
            Task = Tasks.PrepareResponse,
            Opcode = EventOpcode.Stop)]
        public void PrepareResponseStop()
        {
            WriteEvent(8);
        }

        [Event(
            9,
            Message = "Start retrieving player summary",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrievePlayerSummary,
            Opcode = EventOpcode.Start)]
        public void RetrievePlayerSummaryStart()
        {
            WriteEvent(9);
        }

        [Event(
            10,
            Message = "Finished retrieving player summary",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrievePlayerSummary,
            Opcode = EventOpcode.Stop)]
        public void RetrievePlayerSummaryStop()
        {
            WriteEvent(10);
        }

        [NonEvent]
        public void SkipNonCachedApp(int appId, string name)
        {
            SkipNonCachedApp(name, appId);
        }

        [Event(
            11,
            Message = "Skipping non-cached app: {0} / {1}",
            Keywords = Keywords.GameController,
            Level = EventLevel.Warning)]
        private void SkipNonCachedApp(string name, int appId)
        {
            WriteEvent(11, name, appId);            
        }

        [NonEvent]
        public void SkipNonGame(int appId, string name)
        {
            SkipNonGame(name, appId);
        }

        [Event(
            12,
            Message = "Skipping non-game: {0} / {1}",
            Keywords = Keywords.GameController,
            Level = EventLevel.Verbose)]
        private void SkipNonGame(string name, int appId)
        {
            WriteEvent(12, name, appId);
        }

        [Event(
            13,
            Message = "Retrieved {1} games for Steam ID {0} ",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational)]
        public void RetrievedOwnedGames(long steamId, int count)
        {
            WriteEvent(13, steamId, count);
        }

        [NonEvent]
        public void ResolvedPersonaName(long steamId, string personaName)
        {
            ResolvedPersonaName(personaName, steamId);
        }

        [Event(
            14,
            Message = "Resolved persona name of Steam ID {1} to {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational)]
        private void ResolvedPersonaName(string personaName, long steamId)
        {
            WriteEvent(14, personaName, steamId);
        }

        [Event(
            15,
            Message = "Start handling GetGames request for Steam ID {0}",
            Keywords = Keywords.GameController,
            Level = EventLevel.Informational,
            Task = Tasks.HandleGetGamesRequest,
            Opcode = EventOpcode.Start)]
        public void HandleGetGamesRequestStart(long steamId)
        {
            WriteEvent(15, steamId);
        }

        [Event(
            16,
            Message = "Finished handling GetGames request for Steam ID {0}",
            Keywords = Keywords.GameController,
            Level = EventLevel.Informational,
            Task = Tasks.HandleGetGamesRequest,
            Opcode = EventOpcode.Start)]
        public void HandleGetGamesRequestStop(long steamId)
        {
            WriteEvent(16, steamId);
        }
    }
}