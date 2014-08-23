using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using HowLongToBeatSteam.Controllers.Responses;
using HowLongToBeatSteam.Models;

namespace HowLongToBeatSteam.Controllers

{
    [RoutePrefix("api/games")]
    public class GamesController : ApiController
    {
        private const string SteamApiKey = "5EAD28314E0420C154D739F37F110007";
        private const string GetOwnedGamesFormat = @"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1";

        [Route("library/{steamId:long}")]
        public async Task<IEnumerable<Game>> GetGames(long steamId)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(string.Format(GetOwnedGamesFormat, SteamApiKey, steamId));
                response.EnsureSuccessStatusCode();

                var ownedGamesResponse = (await response.Content.ReadAsAsync<OwnedGamesResponse>());
                if (ownedGamesResponse == null || ownedGamesResponse.response == null || ownedGamesResponse.response.games == null)
                {
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
                }

                return ownedGamesResponse.response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, -1));
            }
        }

        [Route("howlong/{hltbId:int}")]
        public async Task<TimeToBeat> GetHowLong(int hltbId)
        {
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(TableStorageConnectionString);
            //CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            //CloudTable table = tableClient.GetTableReference("steamToHltb");
            
            //await table.CreateIfNotExistsAsync();

            return new TimeToBeat(1, 2, 3, 4);
        }
    }
}
