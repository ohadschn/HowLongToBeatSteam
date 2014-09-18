using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Common;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Models;
using JetBrains.Annotations;

namespace HowLongToBeatSteam.Controllers
{
    [RoutePrefix("api/games")]
    public class GamesController : ApiController
    {
        private static readonly string SteamApiKey = ConfigurationManager.AppSettings["SteamApiKey"];
        private const string GetOwnedSteamGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";
        private static readonly HttpClient Client = new HttpClient();
        
        [UsedImplicitly] 
        private static readonly Timer CacheTimer = new Timer(o => UpdateCache(), null, TimeSpan.Zero, TimeSpan.FromHours(1));
        private static readonly ConcurrentDictionary<int, HltbInfo> Cache = new ConcurrentDictionary<int, HltbInfo>();

        internal static void Touch() { } //called externally to start caching as soon as site is up (by instantiating CacheTimer)

        private static async void UpdateCache() 
        {
            Util.TraceInformation("Updating cache...");
            await TableHelper.QueryAllApps((segment, bucket) =>
            {
                foreach (var appEntity in segment)
                {
                    Cache[appEntity.SteamAppId] = new HltbInfo(appEntity);
                }
            });
            Util.TraceInformation("Finished updating cache");
        }

        [Route("library/{steamId:long}")]
        public async Task<IEnumerable<Game>> GetGames(long steamId)
        {
            Util.TraceInformation("Retrieving all owned games for user ID {0}...", steamId);
            var response = await Client.GetAsync(string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId));
            response.EnsureSuccessStatusCode();

            var ownedGamesResponse = await response.Content.ReadAsAsync<OwnedGamesResponse>();
            if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
            {
                Trace.TraceError("Error retrieving owned games for user ID {0}", steamId);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            
            Util.TraceInformation("Preparing response...");
            var ret = ownedGamesResponse.response.games.Select(
                game => new Game(game.appid, game.name, game.playtime_forever, Cache.GetOrDefault(game.appid)));

            Util.TraceInformation("Sending response...");
            return ret;
        }
    }
}