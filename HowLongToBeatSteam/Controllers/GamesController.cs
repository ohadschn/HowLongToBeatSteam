using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Models;

namespace HowLongToBeatSteam.Controllers
{
    [RoutePrefix("api/games")]
    public class GamesController : ApiController
    {
        private static readonly string SteamApiKey = ConfigurationManager.AppSettings["SteamApiKey"];
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
                SiteUtil.TraceInformation("Updating cache...");
                await TableHelper.QueryAllApps((segment, bucket) =>
                {
                    foreach (var appEntity in segment)
                    {
                        Cache[appEntity.SteamAppId] = new CachedGameInfo(appEntity.SteamName, appEntity.Measured ? new HltbInfo(appEntity) : null);
                    }
                }, null, 100).ConfigureAwait(false); //we'll crash and get recycled after 100 failed attempts - something would have to be very wrong!

                SiteUtil.TraceInformation("Finished updating cache: {0} items", Cache.Count);
                await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(false);
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        [Route("library/{steamId:long}")]
        public async Task<OwnedGamesInfo> GetGames(long steamId)
        {
            SiteUtil.TraceInformation("Retrieving all owned games for user ID {0}...", steamId);
            var ownedGamesTask = GetOwnedGames(steamId);
            var personaNameTask = GetPersonaName(steamId);

            await Task.WhenAll(ownedGamesTask, personaNameTask).ConfigureAwait(true);
            var ownedGamesResponse = ownedGamesTask.Result;
            var personaName = personaNameTask.Result;

            SiteUtil.TraceInformation("Preparing response...");
            var games = new List<SteamApp>();
            bool partialCache = false; 
            foreach (var game in ownedGamesResponse.response.games)
            {
                CachedGameInfo cachedGameInfo;
                var inCache = Cache.TryGetValue(game.appid, out cachedGameInfo);
                if (!inCache)
                {
                    SiteUtil.TraceWarning("Skipping non-cached app: {0} / {1}", game.appid, game.name);
                    partialCache = true;
                    continue;
                }
                
                if (cachedGameInfo.HltbInfo == null)
                {
                    SiteUtil.TraceInformation("Skipping non-game: {0} / {1}", game.appid, game.name);
                    continue;
                }

                games.Add(new SteamApp(game.appid, game.name, game.playtime_forever, cachedGameInfo.HltbInfo.Resolved ? cachedGameInfo.HltbInfo : null));
            }

            SiteUtil.TraceInformation("Sending response...");
            return new OwnedGamesInfo(personaName, partialCache, games);
        }

        private static async Task<OwnedGamesResponse> GetOwnedGames(long steamId)
        {
            if (steamId < 0)
            {
                return new OwnedGamesResponse
                {
                    response = new OwnedGames
                    {
                        games = Cache.Where(kvp => kvp.Value.HltbInfo != null).Take((int)Math.Abs(steamId)).Select(kvp => new OwnedGame
                        {
                            appid = kvp.Key,
                            name = kvp.Value.SteamName,
                            playtime_forever = 0,
                        }).ToArray()
                    }
                };
            }

            OwnedGamesResponse ownedGamesResponse;
            using (var response = await Client.GetAsync(string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId)).ConfigureAwait(false))
            {
                ownedGamesResponse = await response.Content.ReadAsAsync<OwnedGamesResponse>().ConfigureAwait(false);
            }

            if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
            {
                SiteUtil.TraceError("Error retrieving owned games for user ID {0}", steamId);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return ownedGamesResponse;
        }

        private static async Task<string> GetPersonaName(long steamId)
        {
            if (steamId < 0)
            {
                return "Cached games";
            }

            PlayerSummariesResponse playerSummaries;
            using (var response = await Client.GetAsync(string.Format(GetPlayerSummariesFormat, SteamApiKey, steamId)).ConfigureAwait(false))
            {
                playerSummaries = await response.Content.ReadAsAsync<PlayerSummariesResponse>().ConfigureAwait(false);
            }

            if (playerSummaries == null || playerSummaries.response == null ||
                playerSummaries.response.players == null || playerSummaries.response.players.Length == 0)
            {
                SiteUtil.TraceError("Error retrieving player summary for user ID {0}", steamId);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return playerSummaries.response.players[0].personaname;
        }
    }
}