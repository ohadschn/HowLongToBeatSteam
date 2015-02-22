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

        private static readonly int s_vanitUrlResolutionParallelization = SiteUtil.GetOptionalValueFromConfig("VanitUrlResolutionParallelization", 5);
        private const string ResolveVanityUrlFormat = @"http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
        private const int VanityUrlResolutionSuccess = 1;

        private static readonly int s_ownedGamesRetrievalParallelization = SiteUtil.GetOptionalValueFromConfig("OwnedGamesRetrievalParallelization", 5);
        private const string GetOwnedSteamGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";
        private const string CacheVanityUrlName = "b4a836df-4dba-4286-a3f4-ea2c652b7715";

        private static readonly HttpRetryClient Client = new HttpRetryClient(0);

        private static readonly ConcurrentDictionary<int, SteamAppData> Cache = new ConcurrentDictionary<int, SteamAppData>();

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
        public async Task<OwnedGamesInfo> GetGames(string userVanityUrlName)
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
                steamId = await SiteUtil.GetFirstResult(
                    ct => ResolveVanityUrl(userVanityUrlName, ct), s_vanitUrlResolutionParallelization, e => { }).ConfigureAwait(true);
                MemoryCache.Default[userVanityUrlName] = steamId;
            }

            var ownedGames = await 
                SiteUtil.GetFirstResult(ct => GetOwnedGames(steamId, ct), s_ownedGamesRetrievalParallelization, e => { }).ConfigureAwait(true);

            SiteEventSource.Log.PrepareResponseStart();
            var games = new List<SteamAppUserData>();
            bool partialCache = false; 
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

                games.Add(new SteamAppUserData(cachedGameData, game.playtime_forever));
            }
            SiteEventSource.Log.PrepareResponseStop();

            SiteEventSource.Log.HandleGetGamesRequestStop(userVanityUrlName);
            return new OwnedGamesInfo(partialCache, games);
        }

        [Route("update/{steamAppId:int}/{hltbId:int}")]
        [HttpPost]
        public Task UpdateGameMapping(int steamAppId, int hltbId)
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            return StorageHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId));
        }

        private static async Task<long> ResolveVanityUrl(string userVanityUrlName, CancellationToken ct)
        {
            SiteEventSource.Log.ResolveVanityUrlStart(userVanityUrlName);

            if (userVanityUrlName.Contains(CacheVanityUrlName))
            {
                return Int32.Parse(userVanityUrlName.Substring(36)); //truncate GUID
            }

            var vanityUrlResolutionResponse = await 
                SiteUtil.GetAsync<VanityUrlResolutionResponse>(Client, string.Format(ResolveVanityUrlFormat, SteamApiKey, userVanityUrlName), ct)
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
                SiteUtil.GetAsync<OwnedGamesResponse>(Client, string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId), ct).ConfigureAwait(false);
            
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
    }
}