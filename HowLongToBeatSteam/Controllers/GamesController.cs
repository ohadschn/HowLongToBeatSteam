using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
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
        private static readonly ConcurrentDictionary<int, HltbInfo> Cache = new ConcurrentDictionary<int, HltbInfo>();

        public static async void StartUpdatingCache() 
        {
            while (true)
            {
                Util.TraceInformation("Updating cache...");
                await TableHelper.QueryAllApps((segment, bucket) =>
                {
                    foreach (var appEntity in segment)
                    {
                        Cache[appEntity.SteamAppId] = appEntity.Measured ? new HltbInfo(appEntity) : null;
                    }
                }, null, 100); //we'll let the site crash and get recycled after 100 attempts - something would have to be very wrong!

                Util.TraceInformation("Finished updating cache: {0} items", Cache.Count);
                await Task.Delay(TimeSpan.FromHours(1));
            }
// ReSharper disable FunctionNeverReturns
        }
// ReSharper restore FunctionNeverReturns

        [Route("library/{steamId:long}")]
        public async Task<OwnedGamesInfo> GetGames(long steamId)
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

            var games = new List<SteamApp>();
            bool partialCache = false; 
            foreach (var game in ownedGamesResponse.response.games)
            {
                HltbInfo hltbInfo;
                var inCache = Cache.TryGetValue(game.appid, out hltbInfo);
                if (!inCache)
                {
                    Util.TraceWarning("Skipping non-cached app: {0} / {1}", game.appid, game.name);
                    partialCache = true;
                    continue;
                }
                
                if (hltbInfo == null) //non-game
                {
                    Util.TraceInformation("Skipping non-game: {0} / {1}", game.appid, game.name);
                    continue;
                }

                games.Add(new SteamApp(game.appid, game.name, game.playtime_forever, hltbInfo.Resolved ? hltbInfo : null));
            }

            Util.TraceInformation("Sending response...");
            return new OwnedGamesInfo(partialCache, games);
        }
    }
}