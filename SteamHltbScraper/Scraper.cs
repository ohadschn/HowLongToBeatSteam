using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using HtmlAgilityPack;

namespace SteamHltbScraper
{
    internal class Scraper
    {
        private const string SearchHltbUrl = @"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0";
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        private const string HltbGamePageFormat = @"http://www.howlongtobeat.com/game.php?id={0}";
        private const string HltbGameOverviewPageFormat = @"http://www.howlongtobeat.com/game_overview.php?id={0}";

        private static readonly int MaxDegreeOfConcurrency = Int32.Parse(ConfigurationManager.AppSettings["MaxDegreeOfConcurrency"]);

        private static void Main()
        {
            ScrapeHltb().Wait();
        }

        private static async Task ScrapeHltb()
        {
            Util.TraceInformation("Scraping HLTB...");
            var updates = new ConcurrentBag<AppEntity>();
            var apps = await TableHelper.GetAllApps(e => e);

            Util.TraceInformation("Scraping with a maximum degree of concurrency {0}", MaxDegreeOfConcurrency);
            Util.RunWithMaxDegreeOfConcurrency(MaxDegreeOfConcurrency, apps , async app => 
            {
                bool added = false;
                if (app.HltbId == -1)
                {
                    try
                    {
                        app.HltbId = await ScrapeHltbId(app.SteamName);
                    }
                    catch (Exception e)
                    {
                        Util.TraceError("Error scraping HLTB ID for app {0} / {1}: {2}", app.SteamAppId, app.SteamName, e);
                        return;
                    }

                    updates.Add(app);
                    added = true;
                }

                HltbInfo hltbInfo;
                try
                {
                    hltbInfo = await ScrapeHltbInfo(app.HltbId);
                }
                catch (Exception e)
                {
                    Util.TraceError("Error scraping HLTB info for app {0} / {1}: {2}", app.SteamAppId, app.SteamName, e);
                    return;
                }

                PopulateAppEntity(app, hltbInfo);
                if (!added)
                {
                    updates.Add(app);
                }
            });

            await TableHelper.InsertOrReplace(updates);
            Util.TraceInformation("Done Scraping HLTB");
        }

        private static async Task<HltbInfo> ScrapeHltbInfo(int hltbId)
        {
            Util.TraceInformation("Scraping HLTB info for id {0}...", hltbId);
            using (var client = new HttpClient())
            {
                var hltbName = await ScrapeHltbName(hltbId);

                var gameOverviewUrl = String.Format(HltbGameOverviewPageFormat, hltbId);
                Util.TraceInformation("Retrieving game overview URL from {0}...", gameOverviewUrl);

                var response = await client.GetAsync(gameOverviewUrl);
                response.EnsureSuccessStatusCode();
                var responseStream = await response.Content.ReadAsStreamAsync();

                Util.TraceInformation("Scraping HLTB info for game {0} from HTML...", hltbId);
                var doc = new HtmlDocument();
                doc.Load(responseStream);

                var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_times");
                if (list == null)
                {
                    throw new InvalidOperationException("Can't find list element - HLTB ID " + hltbId);
                }

                var listItems = list.Descendants("li").Take(4).ToArray();
                if (listItems.Length != 4)
                {
                    throw new InvalidOperationException("List element does not contain 4 entries - HLTB ID " + hltbId);
                }

                if (listItems.Any(hn => hn.InnerText == null) 
                    || !listItems[0].InnerText.Contains("Main") 
                    || !listItems[1].InnerText.Contains("Extras")
                    || !listItems[2].InnerText.Contains("Completionist") 
                    || !listItems[3].InnerText.Contains("Combined"))
                {
                    throw new InvalidOperationException("List element does not contain expected TTB text (Main, Extras, Completionist, Combined)- HLTB ID " + hltbId);                    
                }

                var mainTtb = GetMinutes(listItems[0]);
                var extrasTtb = GetMinutes(listItems[1]);
                var completionistTtb = GetMinutes(listItems[2]);
                var combinedTtb = GetMinutes(listItems[3]);

                Util.TraceInformation("Finished scraping HLTB info for hltb {0}: Main {1} Extras {2} Completionist {3} Combined {4}",
                    hltbId, mainTtb, extrasTtb, completionistTtb, combinedTtb);

                return new HltbInfo(hltbName, mainTtb, extrasTtb, completionistTtb, combinedTtb);
            }
        }

        private static async Task<string> ScrapeHltbName(int hltbId)
        {
            Util.TraceInformation("Scraping HLTB name for id {0}...", hltbId);
            using (var client = new HttpClient())
            {
                var gamePageUrl = String.Format(HltbGamePageFormat, hltbId);
                Util.TraceInformation("Retrieving HLTB game page from {0}...", gamePageUrl);

                var response = await client.GetAsync(gamePageUrl);
                response.EnsureSuccessStatusCode();
                var responseStream = await response.Content.ReadAsStreamAsync();

                Util.TraceInformation("Scraping name for HLTB game {0} from HTML...", hltbId);
                var doc = new HtmlDocument();
                doc.Load(responseStream);

                var headerDiv = doc.DocumentNode.Descendants().FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_header");
                if (headerDiv == null)
                {
                    throw new InvalidOperationException("Can't parse name for HLTB ID " + hltbId);
                }

                var hltbName = headerDiv.InnerText.Trim();
                Util.TraceInformation("Finished scraping HLTB name for ID {0}: {1}", hltbId, hltbName);
                return hltbName;
            }
        }

        private static int GetMinutes(HtmlNode listItem)
        {
            var hoursDiv = listItem.Descendants("div").FirstOrDefault();
            if (hoursDiv == null)
            {
                throw new InvalidOperationException("TTB div not found inside list item");
            }

            var hoursStr = hoursDiv.InnerText;
            if (hoursStr == null)
            {
                return 0;
            }

            int minutes = 0;
            var match = Regex.Match(hoursStr, @"\s*(.+) Hour");
            if (match.Success && match.Groups.Count == 2)
            {
                double hours;
                Double.TryParse(match.Groups[1].Value.Replace("&#189;", ".5"), out hours);
                minutes = (int) TimeSpan.FromHours(hours).TotalMinutes;
            }
            else
            {
                match = Regex.Match(hoursStr, @"\s*(.+) Min");
                if (match.Success && match.Groups.Count == 2)
                {
                    Int32.TryParse(match.Groups[1].Value, out minutes);
                }
                else
                {
                    Util.TraceWarning("Cannot parse duration from list item with text {0}", listItem.InnerText);                    
                }
            }

            return minutes;
        }

        private static async Task<int> ScrapeHltbId(string appName)
        {
            Util.TraceInformation("Scraping HLTB ID for {0}...", appName);
            using (var client = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, SearchHltbUrl)
                {
                    Content = new StringContent(String.Format(SearchHltbPostDataFormat, appName), Encoding.UTF8, "application/x-www-form-urlencoded")
                };
                Util.TraceInformation("Sending search query: {0}", req);

                var response = await client.SendAsync(req);
                response.EnsureSuccessStatusCode();
                var responseStream = await response.Content.ReadAsStreamAsync();

                Util.TraceInformation("Scraping HLTB ID for game {0} from HTML...", appName);
                var doc = new HtmlDocument();
                doc.Load(responseStream);

                var first = doc.DocumentNode.Descendants("li").FirstOrDefault();
                if (first == null)
                {
                    throw new InvalidOperationException("App not found in search");
                }

                var anchor = first.Descendants("a").FirstOrDefault();
                if (anchor == null)
                {
                    throw new InvalidOperationException("App anchor not found");
                }

                var link = anchor.GetAttributeValue("href", null);
                if (link == null)
                {
                    throw new InvalidOperationException("App anchor does not include href attribute");
                }

                var idStr = link.Substring(12);
                int hltbId;
                if (!int.TryParse(idStr, out hltbId))
                {
                    throw new InvalidOperationException("App link does not contain HLTB integer ID in expected location (expecting char12..end): " + idStr);
                }

                Util.TraceInformation("Scraped HLTB ID for {0} : {1}", appName, hltbId);
                return hltbId;
            }
        }

        private static void PopulateAppEntity(AppEntity app, HltbInfo hltbInfo)
        {
            app.HltbName = hltbInfo.Name;
            app.MainTtb = hltbInfo.MainTtb;
            app.ExtrasTtb = hltbInfo.ExtrasTtb;
            app.CompletionistTtb = hltbInfo.CompletionistTtb;
            app.CombinedTtb = hltbInfo.CombinedTtb;
        }
    }
}