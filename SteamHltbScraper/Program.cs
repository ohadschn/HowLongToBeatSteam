using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SteamHltbScraper
{
    class Program
    {
        private const string GetHowLongUri = @"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0";
        private const string GetHowLongPostDataFormat = @"queryString={0}";

        static void Main()
        {
            Scrape().Wait();
        }

        private static async Task Scrape()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("http://api.steampowered.com/ISteamApps/GetAppList/v0001/");
                response.EnsureSuccessStatusCode();

                var allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>();
                var correlatedIds = allGamesRoot.applist.apps.app
                        .Select(a => new {Name = a.name, SteamId = a.appid, HltbId = ScrapeHltbId(a.name).Result});

                var sb = new StringBuilder();
                foreach (var correlatedId in correlatedIds)
                {
                    sb.AppendFormat("{0},{1},{2}{3}", correlatedId.Name, correlatedId.SteamId, correlatedId.HltbId, Environment.NewLine);
                }

                File.WriteAllText(@"F:\Downloads\steamHltb.csv", sb.ToString());
            }
        }

        private static async Task<int> ScrapeHltbId(string appName)
        {
            using (var client = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, GetHowLongUri)
                {
                    Content = new StringContent(string.Format(GetHowLongPostDataFormat, appName), Encoding.UTF8, "application/x-www-form-urlencoded")
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

// ReSharper disable InconsistentNaming
    public class App
    {
        public int appid { get; set; }
        public string name { get; set; }
    }

    public class Apps
    {
        public List<App> app { get; set; }
    }

    public class Applist
    {
        public Apps apps { get; set; }
    }

    public class AllGamesRoot
    {
        public Applist applist { get; set; }
    }
}
// ReSharper restore InconsistentNaming
