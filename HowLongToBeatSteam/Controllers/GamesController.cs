﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Runtime.Caching;
using Common.Entities;
using Common.Storage;
using Common.Util;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Logging;
using HowLongToBeatSteam.Models;

namespace HowLongToBeatSteam.Controllers
{
    [RoutePrefix("api/games")]
    public class GamesController : ApiController
    {
        private static readonly string SteamApiKey = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("SteamApiKey");

        private static readonly int s_vanitUrlResolutionParallelization = SiteUtil.GetOptionalValueFromConfig("VanitUrlResolutionParallelization", 3);
        
        [SuppressMessage("Sonar.CodeSmell", "S1075:URIsShouldNotBeHardcoded", Justification = "Steam API")]
        private const string ResolveVanityUrlFormat = @"http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
        private const int VanityUrlResolutionSuccess = 1;

        private static readonly int s_ownedGamesRetrievalParallelization = SiteUtil.GetOptionalValueFromConfig("OwnedGamesRetrievalParallelization", 3);
        private const string GetOwnedSteamGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";

        private static readonly int s_playerSummaryRetrievalParallelization = SiteUtil.GetOptionalValueFromConfig("PlayerSummaryRetrievalParallelization", 3);
        [SuppressMessage("Sonar.CodeSmell", "S1075:URIsShouldNotBeHardcoded", Justification = "Steam API")]
        private const string GetPlayerSummariesUrlFormat = @"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";

        private const string CacheAvatar = @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/ce/ce8f7969f8c79019ab3c7c88ccfbf3185e8ec7da_medium.jpg";
        private static readonly int s_cacheUpdateIntervalMinutes = SiteUtil.GetOptionalValueFromConfig("CacheUpdateIntervalMinutes", 60);

        private static readonly int VanityUrlToSteamIdCacheExpirationMinutes = SiteUtil.GetOptionalValueFromConfig("VanityUrlToSteamIdCacheExpirationMinutes", 60);

        private static readonly HttpRetryClient Client = new HttpRetryClient(0);
        private static readonly ConcurrentDictionary<int, SteamAppData> Cache = new ConcurrentDictionary<int, SteamAppData>(); //int = Steam App ID

        private static readonly string[] NonGenres =  { "Indie", "Casual" };

        [SuppressMessage("Sonar.Bug", "S3168:AsyncMethodsShouldNotReturnVoid", Justification = "We want a failure here to crash the process as per async void semantics")]
        public static async void StartUpdatingCache() 
        {
            while (true)
            {
                EventSource.SetCurrentThreadActivityId(Guid.NewGuid());

                SiteEventSource.Log.UpdateCacheStart();

                var allApps = await StorageHelper.GetAllApps(null, 20).ConfigureAwait(false); //we'll crash and get recycled after 100 failed attempts
                foreach (var appEntity in allApps)
                {
                    Cache[appEntity.SteamAppId] = new SteamAppData(appEntity);
                }
                
                SiteEventSource.Log.UpdateCacheStop(Cache.Count);

                await Task.Delay(TimeSpan.FromMinutes(s_cacheUpdateIntervalMinutes)).ConfigureAwait(false);
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        [Route("library/{userVanityUrlName:minlength(1)}")]
        public async Task<PlayerInfo> GetGames(string userVanityUrlName)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            SiteEventSource.Log.HandleGetGamesRequestStart(userVanityUrlName);

            long steamId;

            var cachedSteamId = MemoryCache.Default[userVanityUrlName];
            if (cachedSteamId != null)
            {
                steamId = (long) cachedSteamId;
            }
            else
            {
                try
                {
                    steamId = await SiteUtil.GetFirstResult(
                        ct => ResolveVanityUrl(userVanityUrlName, ct), s_vanitUrlResolutionParallelization, e => { }).ConfigureAwait(true);
                }
                catch (HttpResponseException) //Steam API rejected vanity URL name
                {
                    //if the userVanityUrlName is numerical, we'll try assuming it's actually the Steam64 ID
                    if (!Int64.TryParse(userVanityUrlName, out steamId))
                    {
                        throw;
                    }
                }
                MemoryCache.Default.Set(userVanityUrlName, steamId, DateTimeOffset.Now.AddMinutes(VanityUrlToSteamIdCacheExpirationMinutes));
            }

            var ownedGamesInfo = await GetGamesCore(ct => GetOwnedGames(steamId, ct), ct => GetPersonaInfo(steamId, ct)).ConfigureAwait(true);

            SiteEventSource.Log.HandleGetGamesRequestStop(userVanityUrlName);
            return ownedGamesInfo;
        }

        [Route("library/cached/{count:minlength(1)}")]
        public async Task<PlayerInfo> GetCachedGames(string count)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            var vanityUrlName = "cached/" + count;
            
            SiteEventSource.Log.HandleGetGamesRequestStart(vanityUrlName);

            var ownedGamesInfo = await GetGamesCore(
                ct => Task.FromResult(GetCachedGames(GetCachedGameCount(count))), 
                ct => Task.FromResult(new PersonaInfo(String.Empty, CacheAvatar))).ConfigureAwait(true);

            SiteEventSource.Log.HandleGetGamesRequestStop(vanityUrlName);
            return ownedGamesInfo;
        }

        [Route("library/missing/{count:minlength(1)}")]
        public async Task<PlayerInfo> GetMissingGames(string count)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            var vanityUrlName = "missing/" + count;

            SiteEventSource.Log.HandleGetGamesRequestStart(vanityUrlName);
           
            var ownedGamesInfo = await GetGamesCore(
                ct => Task.FromResult(GetCachedGames().Where(g => Cache.TryGetValue(g.appid, out var app) && app.HltbInfo.Id < 0).Take(GetCachedGameCount(count)).ToArray()),
                ct => Task.FromResult(new PersonaInfo(String.Empty, CacheAvatar))).ConfigureAwait(true);

            SiteEventSource.Log.HandleGetGamesRequestStop(vanityUrlName);
            return ownedGamesInfo;
        }

        private static int GetCachedGameCount(string count)
        {
            return String.Equals(count, "all", StringComparison.OrdinalIgnoreCase) ? Int32.MaxValue : Int32.Parse(count, CultureInfo.InvariantCulture);
        }

        private static async Task<PlayerInfo> GetGamesCore(Func<CancellationToken, Task<OwnedGame[]>> gamesGetter, Func<CancellationToken, Task<PersonaInfo>> personaGetter)
        {
            var ownedGamesTask = SiteUtil.GetFirstResult(gamesGetter, s_ownedGamesRetrievalParallelization, e => { }).ConfigureAwait(false);
            var personaInfoTask = SiteUtil.GetFirstResult(personaGetter, s_playerSummaryRetrievalParallelization, e => { }).ConfigureAwait(false);

            var ownedGames = await ownedGamesTask;
            var personaInfo = await personaInfoTask;

            SiteEventSource.Log.PrepareResponseStart();
            var games = new List<SteamAppUserData>();
            bool partialCache = false;

            int playtime = 0;
            int mainTtb = 0;
            int extrasTtb = 0;
            int completionistTtb = 0;
            int mainRemaining = 0;
            int extrasRemaining = 0;
            int completionistRemaining = 0;
            int mainCompleted = 0;
            int extrasCompleted = 0;
            int completionistCompleted = 0;
            var playtimesByGenre = new Dictionary<string, int>();
            var playtimesByMetacritic = new Dictionary<int, int>();
            var playtimesByAppType = new Dictionary<string, int>();
            var playtimesByReleaseYear = new Dictionary<int, int>();

            foreach (var game in ownedGames)
            {
                var inCache = Cache.TryGetValue(game.appid, out SteamAppData cachedGameData);
                if (!inCache)
                {
                    SiteEventSource.Log.SkipNonCachedApp(game.appid, game.name);
                    partialCache = true;
                    continue;
                }

                if (cachedGameData.HltbInfo == null)
                {
                    SiteEventSource.Log.SkipNonGame(game.appid, game.name);
                    continue;
                }

                playtime += game.playtime_forever;
                mainTtb += cachedGameData.HltbInfo.MainTtb;
                extrasTtb += cachedGameData.HltbInfo.ExtrasTtb;
                completionistTtb += cachedGameData.HltbInfo.CompletionistTtb;
                mainRemaining += Math.Max(0, cachedGameData.HltbInfo.MainTtb - game.playtime_forever);
                extrasRemaining += Math.Max(0, cachedGameData.HltbInfo.ExtrasTtb - game.playtime_forever);
                completionistRemaining += Math.Max(0, cachedGameData.HltbInfo.CompletionistTtb - game.playtime_forever);
                mainCompleted += Math.Min(game.playtime_forever, cachedGameData.HltbInfo.MainTtb);
                extrasCompleted += Math.Min(game.playtime_forever, cachedGameData.HltbInfo.ExtrasTtb);
                completionistCompleted += Math.Min(game.playtime_forever, cachedGameData.HltbInfo.CompletionistTtb);

                IReadOnlyList<string> genres = cachedGameData.Genres.Except(NonGenres).ToArray();
                if (genres.Count == 0)
                {
                    genres = cachedGameData.Genres;
                }
                IncrementDictionaryEntryFromZero(playtimesByGenre, String.Join("/", genres), cachedGameData.HltbInfo.MainTtb);
                IncrementDictionaryEntryFromZero(playtimesByMetacritic, cachedGameData.MetacriticScore, cachedGameData.HltbInfo.MainTtb);
                IncrementDictionaryEntryFromZero(playtimesByReleaseYear, cachedGameData.ReleaseYear, cachedGameData.HltbInfo.MainTtb);
                IncrementDictionaryEntryFromZero(playtimesByAppType, cachedGameData.AppType, cachedGameData.HltbInfo.MainTtb);

                games.Add(new SteamAppUserData(cachedGameData, game.playtime_forever));
            }
            SiteEventSource.Log.PrepareResponseStop();

            return new PlayerInfo(partialCache, games.ToArray(), ownedGames.Length - games.Count, new
                Totals(playtime, mainTtb, extrasTtb, completionistTtb, mainRemaining, extrasRemaining, completionistRemaining,
                mainCompleted, extrasCompleted, completionistCompleted,
                playtimesByGenre, playtimesByMetacritic, playtimesByAppType, playtimesByReleaseYear), personaInfo);
        }

        private static void IncrementDictionaryEntryFromZero<TKey>(IDictionary<TKey, int> dict, TKey key, int value) 
        {
            dict[key] = dict.GetOrCreate(key) + value;
        }

        [Route("update/{steamAppId:int}/{hltbId:int}")]
        [HttpPost]
        public async Task<string> UpdateGameMapping(int steamAppId, int hltbId)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            
            await StorageHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId, hltbId < 0 ? AppEntity.NonGameTypeName : null))
                .ConfigureAwait(true);
            
            return "success";
        }

        private static async Task<long> ResolveVanityUrl(string userVanityUrlName, CancellationToken ct)
        {
            SiteEventSource.Log.ResolveVanityUrlStart(userVanityUrlName);

            using (var vanityUrlResolutionResponse = await Client.GetAsync<VanityUrlResolutionResponse>(
                String.Format(ResolveVanityUrlFormat, SteamApiKey, userVanityUrlName), ct).ConfigureAwait(false))
            {
                if (vanityUrlResolutionResponse.Content.response == null)
                {
                    SiteEventSource.Log.VanityUrlResolutionInvalidResponse(userVanityUrlName, VanityUrlResolutionInvalidResponseType.Unknown);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                if (vanityUrlResolutionResponse.Content.response.success != VanityUrlResolutionSuccess)
                {
                    SiteEventSource.Log.ErrorResolvingVanityUrl(userVanityUrlName, vanityUrlResolutionResponse.Content.response.message);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                if (!Int64.TryParse(vanityUrlResolutionResponse.Content.response.steamid, out long steam64Id))
                {
                    SiteEventSource.Log.VanityUrlResolutionInvalidResponse(userVanityUrlName, VanityUrlResolutionInvalidResponseType.SteamIdIsNotAnInt64);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                SiteEventSource.Log.ResolveVanityUrlStop(userVanityUrlName);
                return steam64Id;
            }
        }

        private static async Task<OwnedGame[]> GetOwnedGames(long steamId, CancellationToken ct)
        {
            SiteEventSource.Log.RetrieveOwnedGamesStart(steamId);

            using (var ownedGamesResponse = await
                Client.GetAsync<OwnedGamesResponse>(String.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId), ct).ConfigureAwait(false))
            {
                SiteEventSource.Log.RetrieveOwnedGamesStop(steamId);

                if (ownedGamesResponse.Content?.response?.games == null)
                {
                    if (ownedGamesResponse.Content?.response?.game_count == 0)
                    {
                        return new OwnedGame[0];
                    }

                    SiteEventSource.Log.ErrorRetrievingOwnedGames(steamId);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                var games = ownedGamesResponse.Content.response.games;
                SiteEventSource.Log.RetrievedOwnedGames(steamId, games.Length);
                return games;
            }
        }

        private static OwnedGame[] GetCachedGames(int count = Int32.MaxValue)
        {
            return Cache
                .Where(kvp => kvp.Value.HltbInfo != null)
                .Take(count)
                .Select(kvp => new OwnedGame
                {
                    appid = kvp.Key,
                    name = kvp.Value.SteamName,
                    playtime_forever = 0
                }).ToArray();
        }

        private static async Task<PersonaInfo> GetPersonaInfo(long steamId, CancellationToken ct)
        {
            SiteEventSource.Log.RetrievePersonaInfoStart(steamId);

            using (var playerSummariesResponse = await
                Client.GetAsync<PlayerSummariesResponse>(String.Format(GetPlayerSummariesUrlFormat, SteamApiKey, steamId), ct).ConfigureAwait(false))
            {
                if (playerSummariesResponse.Content?.response?.players == null ||
                    playerSummariesResponse.Content.response.players.Length != 1 ||
                    String.IsNullOrWhiteSpace(playerSummariesResponse.Content.response.players[0].avatarmedium) ||
                    String.IsNullOrWhiteSpace(playerSummariesResponse.Content.response.players[0].personaname))
                {
                    SiteEventSource.Log.ErrorRetrievingPersonaInfo(steamId);
                    return new PersonaInfo(String.Empty, CacheAvatar);
                }

                var playerSummary = playerSummariesResponse.Content.response.players[0];
                SiteEventSource.Log.RetrievePersonaInfoStop(steamId, playerSummary.personaname, playerSummary.avatarmedium);

                return new PersonaInfo(playerSummary.personaname, playerSummary.avatarmedium);
            }
        }
    }
}