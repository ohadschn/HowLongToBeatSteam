using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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

        [Route("library/{steamId:long}")]
        public async Task<IEnumerable<Game>> GetGames(long steamId)
        {
            using (var httpClient = new HttpClient())
            {
                Util.TraceInformation("Retrieving all owned games for user ID {0}...", steamId);
                var response = await httpClient.GetAsync(string.Format(GetOwnedSteamGamesFormat, SteamApiKey, steamId));
                response.EnsureSuccessStatusCode();

                var ownedGamesResponse = await response.Content.ReadAsAsync<OwnedGamesResponse>();
                if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
                {
                    Trace.TraceError("Error retrieving owned games for user ID {0}", steamId);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                Util.TraceInformation("Retrieving Steam->HLTB mappings");
                var hltbInfoDict = await GetHltbInfo(ownedGamesResponse.response.games);

                Util.TraceInformation("Preparing response...");
                var ret = ownedGamesResponse.response.games
                    .Select(game => new Game(game.appid, game.name, game.playtime_forever, hltbInfoDict.GetOrCreate(game.appid)));

                Util.TraceInformation("Sending response...");
                return ret;
            }
        }

        private static async Task<IDictionary<int, HltbInfo>> GetHltbInfo(IEnumerable<OwnedGame> ownedGames)
        {
            Util.TraceInformation("Generating owned games hash...");
            var ownedGamesHashes = new HashSet<int>[AppEntity.Buckets];
            ownedGamesHashes[0] = new HashSet<int>(ownedGames.Select(og => og.appid));
            for (int i = 1; i < ownedGamesHashes.Length; i++)
            {
                ownedGamesHashes[i] = new HashSet<int>(ownedGamesHashes[0]); //HashSet<T> is not thread safe so we'll use copies        
            }

            Util.TraceInformation("Preparing mapping...");
            var steamHltbMap = new ConcurrentDictionary<int, HltbInfo>();
            await TableHelper.QueryAllApps((segment, bucket) =>
            {
                foreach (var gameEntity in segment.Where(ge => ownedGamesHashes[bucket].Contains(ge.SteamAppId)))
                {
                    bool added = steamHltbMap.TryAdd(gameEntity.SteamAppId, new HltbInfo(gameEntity));
                    Trace.Assert(
                        added,
                        string.Format("identical steam ID {0} in different partitions or in the same partition ({1})", gameEntity.SteamAppId, bucket));
                }
            });

            return steamHltbMap;
        }
    }
}