using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
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
        private static readonly string SteamApiKey = ConfigurationManager.AppSettings["SteamApiKey"];

        private const string ResolveVanityUrlFormat = @"http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
        private const int VanityUrlResolutionSuccess = 1;

        private const string GetOwnedSteamGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";
        private const string GetPlayerSummariesFormat = @"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";
        
        private static readonly HttpRetryClient Client = new HttpRetryClient(0);

        class CachedGameInfo
        {
            public string SteamName { get; private set; }
            public HltbInfo HltbInfo { get; private set; }

            public CachedGameInfo(string steamName, HltbInfo hltbInfo)
            {
                SteamName = steamName;
                HltbInfo = hltbInfo;
            }
        }
        private static readonly ConcurrentDictionary<int, CachedGameInfo> Cache = new ConcurrentDictionary<int, CachedGameInfo>();

        public static async void StartUpdatingCache() 
        {
            while (true)
            {
                SiteEventSource.Log.UpdateCacheStart();
                await TableHelper.QueryAllApps((segment, bucket) =>
                {
                    foreach (var appEntity in segment)
                    {
                        Cache[appEntity.SteamAppId] = new CachedGameInfo(appEntity.SteamName, appEntity.Measured ? new HltbInfo(appEntity) : null);
                    }
                }, null, 100).ConfigureAwait(false); //we'll crash and get recycled after 100 failed attempts - something would have to be very wrong!
                SiteEventSource.Log.UpdateCacheStop(Cache.Count);
                
                await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(false);
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        [Route("library/{userVanityUrlName}")]
        public async Task<OwnedGamesInfo> GetGames(string userVanityUrlName)
        {
            SiteEventSource.Log.HandleGetGamesRequestStart(userVanityUrlName);

            long steamId = await ResolveVanityUrl(userVanityUrlName).ConfigureAwait(true);

            var ownedGamesTask = GetOwnedGames(steamId);
            var personaNameTask = GetPersonaName(steamId);

            await Task.WhenAll(ownedGamesTask, personaNameTask).ConfigureAwait(true);
            var ownedGames = ownedGamesTask.Result;
            var personaName = personaNameTask.Result;

            SiteEventSource.Log.PrepareResponseStart();
            var games = new List<SteamApp>();
            bool partialCache = false; 
            foreach (var game in ownedGames)
            {
                CachedGameInfo cachedGameInfo;
                var inCache = Cache.TryGetValue(game.appid, out cachedGameInfo);
                if (!inCache)
                {
                    SiteEventSource.Log.SkipNonCachedApp(game.appid, game.name);
                    partialCache = true;
                    continue;
                }
                
                if (cachedGameInfo.HltbInfo == null)
                {
                    SiteEventSource.Log.SkipNonGame(game.appid, game.name);
                    continue;
                }

                games.Add(new SteamApp(game.appid, game.name, game.playtime_forever, cachedGameInfo.HltbInfo.Resolved ? cachedGameInfo.HltbInfo : null));
            }
            SiteEventSource.Log.PrepareResponseStop();

            SiteEventSource.Log.HandleGetGamesRequestStop(userVanityUrlName);
            return new OwnedGamesInfo(personaName, partialCache, games);
        }

        [Route("update/{steamAppId:int}/{hltbId:int}")]
        [HttpPost]
        public Task UpdateGameMapping(int steamAppId, int hltbId)
        {
            return TableHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId));
        }

        private static async Task<long> ResolveVanityUrl(string userVanityUrlName)
        {
            SiteEventSource.Log.ResolveVanityUrlStart(userVanityUrlName);

            var vanityUrlResolutionResponse = await 
                SiteUtil.GetAsync<VanityUrlResolutionResponse>(Client, string.Format(ResolveVanityUrlFormat, SteamApiKey, userVanityUrlName))
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

        private static async Task<OwnedGame[]> GetOwnedGames(long steamId)
        {
            if (steamId < 0)
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
                SiteUtil.GetAsync<OwnedGamesResponse>(Client, string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId)).ConfigureAwait(false);
            
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

        private static async Task<string> GetPersonaName(long steamId)
        {
            if (steamId < 0)
            {
                return String.Format(CultureInfo.InvariantCulture, "first {0} games", Math.Abs(steamId));
            }

            SiteEventSource.Log.RetrievePlayerSummaryStart();
            
            var playerSummaries = await 
                SiteUtil.GetAsync<PlayerSummariesResponse>(Client,string.Format(GetPlayerSummariesFormat, SteamApiKey, steamId)).ConfigureAwait(false);

            SiteEventSource.Log.RetrievePlayerSummaryStop();

            if (playerSummaries == null || playerSummaries.response == null ||
                playerSummaries.response.players == null || playerSummaries.response.players.Length == 0)
            {
                SiteEventSource.Log.ErrorRetrievingPersonaName(steamId);
                return "Unknown";
            }

            var personaName = playerSummaries.response.players[0].personaname;
            SiteEventSource.Log.ResolvedPersonaName(steamId, personaName);
            return personaName;
        }
    }
}