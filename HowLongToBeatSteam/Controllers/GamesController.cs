using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
        private static readonly string SteamApiKey = SiteUtil.GetMandatoryValueFromConfig("SteamApiKey");

        private static readonly int s_vanitUrlResolutionParallelization = SiteUtil.GetOptionalValueFromConfig("VanitUrlResolutionParallelization", 3);
        private const string ResolveVanityUrlFormat = @"http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
        private const int VanityUrlResolutionSuccess = 1;

        private static readonly int s_ownedGamesRetrievalParallelization = SiteUtil.GetOptionalValueFromConfig("OwnedGamesRetrievalParallelization", 3);
        private const string GetOwnedSteamGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";

        private static readonly int s_playerSummaryRetrievalParallelization = SiteUtil.GetOptionalValueFromConfig("PlayerSummaryRetrievalParallelization", 3);
        private const string GetPlayerSummariesUrlFormat = @"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";

        private const string CacheAvatar = @"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/ce/ce8f7969f8c79019ab3c7c88ccfbf3185e8ec7da_medium.jpg";

        private static readonly HttpRetryClient Client = new HttpRetryClient(0);
        private static readonly ConcurrentDictionary<int, SteamAppData> Cache = new ConcurrentDictionary<int, SteamAppData>();

        private static readonly string[] NonGenres =  { "Indie", "Casual" };

        public static async void StartUpdatingCache() 
        {
            while (true)
            {
                EventSource.SetCurrentThreadActivityId(Guid.NewGuid());

                SiteEventSource.Log.UpdateCacheStart();
                await StorageHelper.QueryAllApps((segment, bucket) =>
                {
                    foreach (var appEntity in segment)
                    {
                        Cache[appEntity.SteamAppId] = new SteamAppData(appEntity);
                    }
                }, null, 100).ConfigureAwait(false); //we'll crash and get recycled after 100 failed attempts - something would have to be very wrong!
                SiteEventSource.Log.UpdateCacheStop(Cache.Count);
                
                await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(false);
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
                MemoryCache.Default[userVanityUrlName] = steamId;
            }

            var ownedGamesInfo = await GetGamesCore(steamId).ConfigureAwait(true);

            SiteEventSource.Log.HandleGetGamesRequestStop(userVanityUrlName);
            return ownedGamesInfo;
        }

        [Route("library/cached/{count:minlength(1)}")]
        public async Task<PlayerInfo> GetCachedGames(string count)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            SiteEventSource.Log.HandleGetGamesRequestStart("cached/" + count);

            int countTyped = String.Equals(count, "all", StringComparison.OrdinalIgnoreCase) ? Int32.MaxValue : Int32.Parse(count);
            var ownedGamesInfo = await GetGamesCore(-countTyped).ConfigureAwait(true);

            SiteEventSource.Log.HandleGetGamesRequestStop("cached/" + count);
            return ownedGamesInfo;
        }

        private static async Task<PlayerInfo> GetGamesCore(long steamId)
        {
            var ownedGamesTask = SiteUtil.GetFirstResult(ct => GetOwnedGames(steamId, ct), s_ownedGamesRetrievalParallelization, e => { }).ConfigureAwait(false);
            var personaInfoTask = SiteUtil.GetFirstResult(ct => GetPersonaInfo(steamId, ct), s_playerSummaryRetrievalParallelization, e => { }).ConfigureAwait(false);

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
            Dictionary<string, int> playtimesByGenre = new Dictionary<string, int>();
            Dictionary<int, int> playtimesByMetacritic = new Dictionary<int, int>();
            Dictionary<string, int> playtimesByAppType = new Dictionary<string, int>();
            Dictionary<string, int> playtimesByPlatform = new Dictionary<string, int>();
            Dictionary<int, int> playtimesByReleaseYear = new Dictionary<int, int>();

            foreach (var game in ownedGames)
            {
                SteamAppData cachedGameData;
                var inCache = Cache.TryGetValue(game.appid, out cachedGameData);
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
                var gameMainRemaining = Math.Max(0, cachedGameData.HltbInfo.MainTtb - game.playtime_forever);
                mainRemaining += gameMainRemaining;
                extrasRemaining += Math.Max(0, cachedGameData.HltbInfo.ExtrasTtb - game.playtime_forever);
                completionistRemaining += Math.Max(0, cachedGameData.HltbInfo.CompletionistTtb - game.playtime_forever);

                IReadOnlyList<string> genres = cachedGameData.Genres.Except(NonGenres).ToArray();
                if (genres.Count == 0)
                {
                    genres = cachedGameData.Genres;
                }
                IncrementDictionaryEntryFromZero(playtimesByGenre, String.Join("/", genres), gameMainRemaining);
                IncrementDictionaryEntryFromZero(playtimesByMetacritic, cachedGameData.MetacriticScore, gameMainRemaining);

                games.Add(new SteamAppUserData(cachedGameData, game.playtime_forever));
            }
            SiteEventSource.Log.PrepareResponseStop();

            return new PlayerInfo(partialCache, games, new
                Totals(playtime, mainTtb, extrasTtb, completionistTtb, mainRemaining, extrasRemaining, completionistRemaining,
                playtimesByGenre, playtimesByMetacritic, playtimesByAppType, playtimesByPlatform, playtimesByReleaseYear), personaInfo);
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
            await StorageHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId)).ConfigureAwait(true);
            return "success";
        }

        private static async Task<long> ResolveVanityUrl(string userVanityUrlName, CancellationToken ct)
        {
            SiteEventSource.Log.ResolveVanityUrlStart(userVanityUrlName);

            var vanityUrlResolutionResponse = await 
                SiteUtil.GetAsync<VanityUrlResolutionResponse>(Client, String.Format(ResolveVanityUrlFormat, SteamApiKey, userVanityUrlName), ct)
                .ConfigureAwait(false);

            if (vanityUrlResolutionResponse.response == null)
            {
                SiteEventSource.Log.VanityUrlResolutionInvalidResponse(userVanityUrlName, VanityUrlResolutionInvalidResponseType.Unknown);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            
            if (vanityUrlResolutionResponse.response.success != VanityUrlResolutionSuccess)
            {
                SiteEventSource.Log.ErrorResolvingVanityUrl(userVanityUrlName, vanityUrlResolutionResponse.response.message);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            long steam64Id;
            if (!Int64.TryParse(vanityUrlResolutionResponse.response.steamid, out steam64Id))
            {
                SiteEventSource.Log.VanityUrlResolutionInvalidResponse(userVanityUrlName, VanityUrlResolutionInvalidResponseType.SteamIdIsNotAnInt64);
                throw new HttpResponseException(HttpStatusCode.BadRequest); 
            }

            SiteEventSource.Log.ResolveVanityUrlStop(userVanityUrlName);
            return steam64Id;
        }

        private static async Task<OwnedGame[]> GetOwnedGames(long steamId, CancellationToken ct)
        {
            if (steamId <= 0)
            {
                return Cache
                    .Where(kvp => kvp.Value.HltbInfo != null)
                    .Take((int) Math.Abs(steamId))
                    .Select(kvp => new OwnedGame
                    {
                        appid = kvp.Key,
                        name = kvp.Value.SteamName,
                        playtime_forever = 0,
                    }).ToArray();
            }

            SiteEventSource.Log.RetrieveOwnedGamesStart(steamId);
            
            var ownedGamesResponse = await
                SiteUtil.GetAsync<OwnedGamesResponse>(Client, String.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId), ct).ConfigureAwait(false);
            
            SiteEventSource.Log.RetrieveOwnedGamesStop(steamId);

            if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
            {
                SiteEventSource.Log.ErrorRetrievingOwnedGames(steamId);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var games = ownedGamesResponse.response.games;
            SiteEventSource.Log.RetrievedOwnedGames(steamId, games.Length);
            return games;
        }

        private static async Task<PersonaInfo> GetPersonaInfo(long steamId, CancellationToken ct)
        {
            if (steamId <= 0)
            {
                return new PersonaInfo(String.Empty, CacheAvatar);
            }

            SiteEventSource.Log.RetrievePersonaInfoStart(steamId);

            var playerSummariesResponse = await
                SiteUtil.GetAsync<PlayerSummariesResponse>(Client, String.Format(GetPlayerSummariesUrlFormat, SteamApiKey, steamId), ct).ConfigureAwait(false);

            if (playerSummariesResponse == null || 
                playerSummariesResponse.response == null ||
                playerSummariesResponse.response.players == null || 
                playerSummariesResponse.response.players.Length != 1 ||
                String.IsNullOrWhiteSpace(playerSummariesResponse.response.players[0].avatarmedium) ||
                String.IsNullOrWhiteSpace(playerSummariesResponse.response.players[0].personaname))
            {
                SiteEventSource.Log.ErrorRetrievingPersonaInfo(steamId);
                return new PersonaInfo(String.Empty, CacheAvatar);
            }

            var playerSummary = playerSummariesResponse.response.players[0];
            SiteEventSource.Log.RetrievePersonaInfoStop(steamId, playerSummary.personaname, playerSummary.avatarmedium);

            return new PersonaInfo(playerSummary.personaname, playerSummary.avatarmedium);
        }
    }
}