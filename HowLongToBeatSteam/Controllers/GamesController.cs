using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
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
        private static readonly HttpRetryClient Client = new HttpRetryClient(3);
        
        [UsedImplicitly] 
        private static readonly ConcurrentDictionary<int, HltbInfo> Cache = new ConcurrentDictionary<int, HltbInfo>();

        public static async void StartUpdatingCache() 
        {
            while (true)
            {
                SiteUtil.TraceInformation("Updating cache...");
                await TableHelper.QueryAllApps((segment, bucket) =>
                {
                    foreach (var appEntity in segment)
                    {
                        Cache[appEntity.SteamAppId] = appEntity.Measured ? new HltbInfo(appEntity) : null;
                    }
                }, null, 100).ConfigureAwait(false); //we'll let the site crash and get recycled after 100 attempts - something would have to be very wrong!

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
            
            OwnedGamesResponse ownedGamesResponse;
            using (var response = await Client.GetAsync(string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId)).ConfigureAwait(true))
            {
                ownedGamesResponse = await response.Content.ReadAsAsync<OwnedGamesResponse>().ConfigureAwait(true);
            }

            if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
            {
                SiteUtil.TraceError("Error retrieving owned games for user ID {0}", steamId);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            
            SiteUtil.TraceInformation("Preparing response...");

            var games = new List<SteamApp>();
            bool partialCache = false; 
            foreach (var game in ownedGamesResponse.response.games)
            {
                HltbInfo hltbInfo;
                var inCache = Cache.TryGetValue(game.appid, out hltbInfo);
                if (!inCache)
                {
                    SiteUtil.TraceWarning("Skipping non-cached app: {0} / {1}", game.appid, game.name);
                    partialCache = true;
                    continue;
                }
                
                if (hltbInfo == null)
                {
                    SiteUtil.TraceInformation("Skipping non-game: {0} / {1}", game.appid, game.name);
                    continue;
                }

                games.Add(new SteamApp(game.appid, game.name, game.playtime_forever, hltbInfo.Resolved ? hltbInfo : null));
            }

            SiteUtil.TraceInformation("Sending response...");
            return new OwnedGamesInfo(partialCache, games);
        }
    }
}