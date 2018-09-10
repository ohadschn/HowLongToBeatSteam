using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.Http;
using Common.Logging;
using DotNetOpenAuth.OpenId.RelyingParty;
using JetBrains.Annotations;

namespace HowLongToBeatSteam.Logging
{
    public enum VanityUrlResolutionInvalidResponseType
    {
        Unknown = 0,
        SteamIdIsNotAnInt64 = 1
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-Site")]
    public sealed class SiteEventSource : EventSourceBase
    {
        public static readonly SiteEventSource Log = new SiteEventSource();
        private SiteEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords SteamApi = (EventKeywords)1;
            public const EventKeywords GamesController = (EventKeywords) 2;
            public const EventKeywords TableStorage = (EventKeywords) 4;
            public const EventKeywords OpenId = (EventKeywords)8;
            public const EventKeywords Http = (EventKeywords) 16;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask UpdateCache = (EventTask) 1;
            public const EventTask RetrieveOwnedGames = (EventTask) 2;
            public const EventTask PrepareResponse = (EventTask) 3;
            public const EventTask HandleGetGamesRequest = (EventTask) 5;
            public const EventTask ResolveVanityUrl = (EventTask) 6;
            public const EventTask RetrievePersonaInfo = (EventTask) 7;
            public const EventTask CreateAuthenticationRequest = (EventTask) 8;
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
            Message = "Finished querying table storage for all apps to update cache - count: {0}",
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
            7,
            Message = "Start preparing response",
            Keywords = Keywords.GamesController,
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
            Keywords = Keywords.GamesController,
            Level = EventLevel.Informational,
            Task = Tasks.PrepareResponse,
            Opcode = EventOpcode.Stop)]
        public void PrepareResponseStop()
        {
            WriteEvent(8);
        }

        [NonEvent]
        public void SkipNonCachedApp(int appId, string name)
        {
            SkipNonCachedApp(name, appId);
        }

        [Event(
            11,
            Message = "Skipping non-cached app: {0} / {1}",
            Keywords = Keywords.GamesController,
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
            Message = "Skipping non finite single-player game: {0} / {1}",
            Keywords = Keywords.GamesController,
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

        [Event(
            15,
            Message = "Start handling GetGames request for Steam user {0}",
            Keywords = Keywords.GamesController,
            Level = EventLevel.Informational,
            Task = Tasks.HandleGetGamesRequest,
            Opcode = EventOpcode.Start)]
        public void HandleGetGamesRequestStart(string userVanityUrlName)
        {
            WriteEvent(15, userVanityUrlName);
        }

        [Event(
            16,
            Message = "Finished handling GetGames request for Steam user {0}",
            Keywords = Keywords.GamesController,
            Level = EventLevel.Informational,
            Task = Tasks.HandleGetGamesRequest,
            Opcode = EventOpcode.Stop)]
        public void HandleGetGamesRequestStop(string userVanityUrlName)
        {
            WriteEvent(16, userVanityUrlName);
        }

        [Event(
            17,
            Message = "Start resolving Steam 64 ID from user vanity URL name {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.ResolveVanityUrl,
            Opcode = EventOpcode.Start)]
        public void ResolveVanityUrlStart(string userVanityUrlName)
        {
            WriteEvent(17, userVanityUrlName);
        }

        [Event(
            18,
            Message = "Finished resolving Steam 64 ID from user vanity URL name {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.ResolveVanityUrl,
            Opcode = EventOpcode.Stop)]
        public void ResolveVanityUrlStop(string userVanityUrlName)
        {
            WriteEvent(18, userVanityUrlName);
        }

        [Event(
            19,
            Message = "Invalid response resolving user vanity URL name {0}: {1}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Error)]
        public void VanityUrlResolutionInvalidResponse(string userVanityUrlName, VanityUrlResolutionInvalidResponseType invalidResponseType)
        {
            WriteEvent(19, userVanityUrlName, (int)invalidResponseType);
        }

        [Event(
            20,
            Message = "Error resolving user vanity URL name {0}: {1}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Warning)]
        public void ErrorResolvingVanityUrl(string userVanityUrlName, string errorMessage)
        {
            WriteEvent(20, userVanityUrlName, errorMessage);
        }

        [Event(
            21,
            Message = "Start retrieving persona info for Steam ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrievePersonaInfo,
            Opcode = EventOpcode.Start)]
        public void RetrievePersonaInfoStart(long steamId)
        {
            WriteEvent(21, steamId);
        }

        [Event(
            22,
            Message = "Finished retrieving persona info for Steam ID {0} - Name: {1} / Avatar: {2}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrievePersonaInfo,
            Opcode = EventOpcode.Stop)]
        public void RetrievePersonaInfoStop(long steamId, string personaName, string avatar)
        {
            WriteEvent(22, steamId, personaName, avatar);
        }

        [Event(
            23,
            Message = "Error resolving persona info for Steam ID {0}",
            Keywords = Keywords.SteamApi,
            Level = EventLevel.Error)]
        public void ErrorRetrievingPersonaInfo(long steamId)
        {
            WriteEvent(23, steamId);
        }

        [Event(
            24,
            Message = "Start creating Open ID authentication request",
            Keywords = Keywords.OpenId,
            Level = EventLevel.Informational,
            Task = Tasks.CreateAuthenticationRequest,
            Opcode = EventOpcode.Start)]
        public void CreateAuthenticationRequestStart()
        {
            WriteEvent(24);
        }

        [Event(
            25,
            Message = "Finished creating Open ID authentication request",
            Keywords = Keywords.OpenId,
            Level = EventLevel.Informational,
            Task = Tasks.CreateAuthenticationRequest,
            Opcode = EventOpcode.Stop)]
        public void CreateAuthenticationRequestStop()
        {
            WriteEvent(25);
        }

        [Event(
            26,
            Message = "Malformed Steam Claimed ID provided: {0} ({1})",
            Keywords = Keywords.OpenId,
            Level = EventLevel.Error)]
        public void MalformedSteamClaimedIdProvided(string claimedId, string error)
        {
            WriteEvent(26, claimedId, error);
        }

        [Event(
            27,
            Message = "Steam Authentication Failed. AuthenticationStatus: {0}",
            Keywords = Keywords.OpenId,
            Level = EventLevel.Warning)]
        public void SteamAuthenticationFailed(AuthenticationStatus authenticationStatus)
        {
            WriteEvent(27, (int)authenticationStatus);
        }

        [Event(
            28,
            Message = "Steam authentication succeeded. Claimed ID: {0}",
            Keywords = Keywords.OpenId,
            Level = EventLevel.Informational)]
        public void SteamAuthenticationSucceeded(string claimedId)
        {
            WriteEvent(28, claimedId);
        }

        [NonEvent]
        public void NeitherOriginNorRefererSpecifiedInRequest([NotNull] HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            NeitherOriginNorRefererSpecifiedInRequest(request.ToString());
        }

        [Event(29,
            Message = "Neither Origin nor Referer were specified: {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void NeitherOriginNorRefererSpecifiedInRequest(string request)
        {
            WriteEvent(29, request);
        }

        [NonEvent]
        public void PartialRefererSpecifiedInRequest([NotNull] HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            PartialRefererSpecifiedInRequest(request.ToString());
        }

        [Event(30,
            Message = "Partial Referer header specified: {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void PartialRefererSpecifiedInRequest(string request)
        {
            WriteEvent(30, request);
        }

        [NonEvent]
        public void MismatchedOriginInRequest([NotNull] HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            MismatchedOriginInRequest(request.ToString());
        }

        [Event(31,
            Message = "Mismatched Origin header specified: {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void MismatchedOriginInRequest(string request)
        {
            WriteEvent(31, request);
        }

        [NonEvent]
        public void MismatchedRefererHeader([NotNull] HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            MismatchedRefererHeader(request.ToString());
        }

        [Event(32,
            Message = "Mismatched Referer header specified: {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void MismatchedRefererHeader(string request)
        {
            WriteEvent(32, request);
        }
    }
}