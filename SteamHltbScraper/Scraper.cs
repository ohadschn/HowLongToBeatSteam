using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common;
using HtmlAgilityPack;

namespace SteamHltbScraper
{
    internal class Scraper
    {

        private const string SearchHltbUrl = @"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0";
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        private static void Main()
        {
            ScrapeHltb().Wait();
        }

        private static async Task ScrapeHltb()
        {
            Util.TraceInformation("Scraping TTB...");
            //foreach (var app in apps.Take(4)) //TODO Remove Take(4) + PLINQ
            //{
            //    Util.TraceInformation("Querying for app ID {0}...", app.appid);
            //    var query = new TableQuery<GameEntity>()
            //        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, app.appid.ToString(CultureInfo.InvariantCulture)));

            //    IEnumerable<GameEntity> queryResults;
            //    try
            //    {
            //        //queryResults = table.ExecuteQuerySegmentedAsync(query, null); //TODO ExecuteQueryAsync when available
            //    }
            //    catch (Exception e)
            //    {
            //        Util.TraceError("Error querying for app ID {0}: {1}", app.appid, e);
            //        continue;
            //    }

            //    //if (queryResults.Any()) 
            //    {
            //        Util.TraceInformation("App ID {0} already exists in DB - skipping...", app.appid);
            //        continue;
            //    }

            //    //TODO batch operations
            //    Util.TraceInformation("Adding app Id {0}...", app.appid);
            //    try
            //    {
            //        var hltbId = await ScrapeHltbId(app.name);
            //        await table.ExecuteAsync(TableOperation.Insert(new GameEntity(app.appid, app.name, hltbId)));
            //    }
            //    catch (Exception e)
            //    {
            //        Util.TraceError("Error adding app ID {0}: {1}", app.appid, e);
            //        continue;
            //    }

            //    Util.TraceInformation("Finished processing app ID {0}", app.appid);
            //}
            Util.TraceInformation("Done Scraping TTB");
        }

        private static async Task<int> ScrapeHltbId(string appName)
        {
            using (var client = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, SearchHltbUrl)
                {
                    Content =
                        new StringContent(string.Format(SearchHltbPostDataFormat, appName), Encoding.UTF8,
                            "application/x-www-form-urlencoded")
                };
                var response = await client.SendAsync(req);

                var doc = new HtmlDocument();
                doc.Load(await response.Content.ReadAsStreamAsync());

                var listItems = doc.DocumentNode.Descendants("li").ToArray();
                Util.TraceInformation("Items: " + listItems.Length);

                var first = listItems.FirstOrDefault();
                if (first == null)
                {
                    return -1;
                }

                var anchor = first.Descendants("a").FirstOrDefault();
                if (anchor == null)
                {
                    return -1;
                }

                var link = anchor.GetAttributeValue("href", null);
                if (link == null)
                {
                    return -1;
                }

                int hltbId;
                if (!int.TryParse(link.Substring(12), out hltbId))
                {
                    return -1;
                }

                return hltbId;
            }
        }
    }
}
