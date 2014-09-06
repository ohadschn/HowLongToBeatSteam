using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace HowLongToBeatSteam.Controllers

{
    [RoutePrefix("api/games")]
    public class GamesController : ApiController
    {
        private const string SteamApiKey = "5EAD28314E0420C154D739F37F110007"; //TODO move to config
        private const string GetOwnedGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";

        //TODO move to configuration
        private const string TableStorageConnectionString =
            @"DefaultEndpointsProtocol=https;AccountName=hltbs;AccountKey=XhDjB312agakHZdi2+xFCe5Dd3j2KAcl+yTtAiCyinCIOYuAKphKjaP0psCm83t/+iLKdMii/uxmUhMetZ7Hiw==";

        private const string SteamToHltbTableName = "steamToHltb";

        [Route("library/{steamId:long}")]
        public async Task<IEnumerable<Game>> GetGames(long steamId)
        {
            using (var httpClient = new HttpClient())
            {
                Util.TraceInformation("Retrieving all owned games for user ID {0}...", steamId);
                var response = await httpClient.GetAsync(string.Format(GetOwnedGamesFormat, SteamApiKey, steamId));
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
            var ret = new Dictionary<int, HltbInfo>();
            var ownedGamesHash = new HashSet<int>(ownedGames.Select(og => og.appid));

            Util.TraceInformation("Preparing table query...");
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            var query = new TableQuery<GameEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, String.Empty));

            TableQuerySegment<GameEntity> currentSegment = null;
            int batch = 1;
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                Util.TraceInformation("Retrieving mappings - batch {0}...", batch);
                currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null);
                
                Util.TraceInformation("Processing batch {0}...", batch);
                foreach (var gameEntity in currentSegment.Where(ge => ownedGamesHash.Contains(ge.SteamAppId)))
                {
                    ret.Add(gameEntity.SteamAppId, new HltbInfo(gameEntity));
                }

                batch++;
            }

            return ret;
        }
    }
}
