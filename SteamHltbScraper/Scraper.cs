using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace SteamHltbScraper
{
    internal class Scraper
    {
        private const string GetSteamAppListUrl = "http://api.steampowered.com/ISteamApps/GetAppList/v0001/";

        private const string SearchHltbUrl =
            @"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0";

        private const string SearchHltbPostDataFormat = @"queryString={0}";

        //TODO move to configuration
        private const string TableStorageConnectionString =
            @"DefaultEndpointsProtocol=https;AccountName=hltbs;AccountKey=XhDjB312agakHZdi2+xFCe5Dd3j2KAcl+yTtAiCyinCIOYuAKphKjaP0psCm83t/+iLKdMii/uxmUhMetZ7Hiw==";

        private const string SteamToHltbTableName = "steamToHltb";

        private static void Main()
        {
            Trace.TraceInformation("Scraping Steam->HLTB correlation...");
            ScrapeCorrelation().Wait();
            Trace.TraceInformation("Scraping Steam->HLTB correlation done.");
        }

        private static async Task ScrapeCorrelation()
        {
            Trace.TraceInformation("Connecting to table {0}...", SteamToHltbTableName);
            var table = CloudStorageAccount.Parse(TableStorageConnectionString).CreateCloudTableClient().GetTableReference(SteamToHltbTableName);
            await table.CreateIfNotExistsAsync();

            foreach (var app in (await GetAllSteamApps()).Take(4)) //TODO Remove Take(4) + PLINQ
            {
                Trace.TraceInformation("Querying for app ID {0}...", app.appid);
                var query = new TableQuery<GameEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey",QueryComparisons.Equal, app.appid.ToString(CultureInfo.InvariantCulture)));

                IEnumerable<GameEntity> queryResults;
                try
                {
                    queryResults = table.ExecuteQuery(query); //TODO ExecuteQueryAsync when available
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error querying for app ID {0}: {1}", app.appid, e);
                    continue;
                }

                if (queryResults.Any()) 
                {
                    Trace.TraceInformation("App ID {0} already exists in DB - skipping...", app.appid);
                    continue;
                }

                //TODO batch operations
                Trace.TraceInformation("Adding app Id {0}...", app.appid);
                try
                {
                    await table.ExecuteAsync(TableOperation.Insert(new GameEntity(app.appid, app.name, await ScrapeHltbId(app.name))));
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error adding app ID {0}: {1}", app.appid, e);
                    continue;
                }

                Trace.TraceInformation("Finished processing app ID {0}", app.appid);
            }
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            Trace.TraceInformation("Getting list of all Steam apps from {0}...", GetSteamAppListUrl);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(GetSteamAppListUrl);
                response.EnsureSuccessStatusCode();

                return (await response.Content.ReadAsAsync<AllGamesRoot>()).applist.apps.app;
            }
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
                Console.WriteLine("Items: " + listItems.Length);

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
