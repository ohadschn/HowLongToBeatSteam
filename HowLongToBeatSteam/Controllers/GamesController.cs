using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
                Trace.TraceInformation("Retrieving all owned games for user ID {0}...", steamId);
                var response = await httpClient.GetAsync(string.Format(GetOwnedGamesFormat, SteamApiKey, steamId));
                response.EnsureSuccessStatusCode();

                var ownedGamesResponse = (await response.Content.ReadAsAsync<OwnedGamesResponse>());
                if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
                {
                    Trace.TraceError("Error retrieving owned games for user ID {0}", steamId);
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                var hltbInfoDict = await GetHltbInfo(ownedGamesResponse.response.games);

                return ownedGamesResponse.response.games
                    .Select(game => new Game(game.appid, game.name, game.playtime_forever, hltbInfoDict.GetOrCreate(game.appid)));
            }
        }

        private static async Task<IDictionary<int, HltbInfo>> GetHltbInfo(OwnedGame[] ownedGames)
        {
            var ret = new Dictionary<int, HltbInfo>();
            if (ownedGames.Length == 0)
            {
                return ret;
            }

            //TODO determine / test limits of filter size
            var filter = new StringBuilder("(" + GenerateSteamAppIdFilter(ownedGames[0].appid) + ")");
            foreach (var appid in ownedGames.Skip(1))
            {
                filter.AppendFormat(" {0} ({1})", TableOperators.Or, GenerateSteamAppIdFilter(appid.appid));
            }

            Trace.TraceInformation("Connecting to table {0}...", SteamToHltbTableName);
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);

            var query = new TableQuery<GameEntity>().Where(filter.ToString());

            Trace.TraceInformation("Executing query with filter {0}...", query.FilterString);
            TableQuerySegment<GameEntity> currentSegment = null;
            while (currentSegment == null || currentSegment.ContinuationToken != null)
            {
                currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null);
                foreach (var gameEntity in currentSegment)
                {
                    ret.Add(gameEntity.SteamAppId, new HltbInfo(gameEntity));
                }
            }

            return ret;
        }

        private static string GenerateSteamAppIdFilter(int appId)
        {
            return TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, appId.ToString(CultureInfo.InvariantCulture));
        }
    }
}
