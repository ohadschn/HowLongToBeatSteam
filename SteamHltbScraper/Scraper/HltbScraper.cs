using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Util;
using HtmlAgilityPack;
using SteamHltbScraper.Imputation;
using SteamHltbScraper.Logging;

namespace SteamHltbScraper.Scraper
{
    public static class HltbScraper
    {
        private static readonly Uri SearchHltbUrl = new Uri(@"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0");
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        private const string HltbGamePageFormat = @"http://www.howlongtobeat.com/game.php?id={0}";
        private const string HltbGameOverviewPageFormat = @"http://www.howlongtobeat.com/game_overview.php?id={0}";

        private static readonly int ScrapingLimit = SiteUtil.GetOptionalValueFromConfig("ScrapingLimit", int.MaxValue);
        private static readonly HttpRetryClient Client = new HttpRetryClient(4);

        private static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                MainAsync().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task MainAsync()
        {
            SiteUtil.SetDefaultConnectionLimit();

            var allApps = (await StorageHelper.GetAllApps(e => e, AppEntity.MeasuredFilter, 20).ConfigureAwait(false)).Take(ScrapingLimit).ToArray();

            ConcurrentBag<AppEntity> updates;
            using (Client)
            {
                updates = await ScrapeHltb(allApps).ConfigureAwait(false);
            }

            await Imputer.Impute(allApps.ToArray(), updates.ToArray()).ConfigureAwait(false);

            //we're using Replace since the only other update to an existing game-typed entity would have to be manual which should take precedence
            await StorageHelper.ReplaceApps(allApps, 20).ConfigureAwait(false); 
        }

        private static async Task<ConcurrentBag<AppEntity>> ScrapeHltb(IEnumerable<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.ScrapeHltbStart();

            var updates = new ConcurrentBag<AppEntity>();
            int count = 0;

            await allApps.ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async app =>
            {
                var current = Interlocked.Increment(ref count);
                HltbScraperEventSource.Log.ScrapeGameStart(app.SteamAppId, current);

                if (app.HltbId == -1)
                {
                    try
                    {
                        app.HltbId = await ScrapeHltbId(app.SteamName).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        HltbScraperEventSource.Log.ErrorScrapingHltbId(current, app.SteamAppId, app.SteamName, e);
                        return;
                    }

                    if (app.HltbId == -1)
                    {
                        return; //app not found in search
                    }
                }

                try
                {
                    app.HltbName = await ScrapeHltbName(app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HltbScraperEventSource.Log.ErrorScrapingHltbName(current, app.SteamAppId, app.SteamName, app.HltbId, e);
                    return;
                }

                HltbInfo hltbInfo;
                try
                {
                    hltbInfo = await ScrapeHltbInfo(app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HltbScraperEventSource.Log.ErrorScrapingHltbInfo(current, app.SteamAppId, app.SteamName, e);
                    return;
                }

                PopulateAppEntity(app, hltbInfo);
                updates.Add(app);

                HltbScraperEventSource.Log.ScrapeGameStop(app.SteamAppId, current);
            }).ConfigureAwait(false);
            
            HltbScraperEventSource.Log.ScrapeHltbStop();
            return updates;
        }

        private static async Task<HltbInfo> ScrapeHltbInfo(int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbInfoStart(hltbId);

            var gameOverviewUrl = new Uri(String.Format(HltbGameOverviewPageFormat, hltbId));

            HltbScraperEventSource.Log.GetGameOverviewPageStart(gameOverviewUrl);
            var doc = await LoadDocument(() => Client.GetAsync(gameOverviewUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.GetGameOverviewPageStop(gameOverviewUrl);

            var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_times");
            if (list == null)
            {
                throw new FormatException("Can't find list element - HLTB ID " + hltbId);
            }

            var listItems = list.Descendants("li").Take(4).ToArray();
            
            int mainTtb, extrasTtb, completionistTtb, soloTtb, coOp, vs;
            bool gotMain = TryGetMinutes(listItems, "main", out mainTtb);
            bool gotExtras = TryGetMinutes(listItems, "extras", out extrasTtb);
            bool gotCompletionist = TryGetMinutes(listItems, "completionist", out completionistTtb);
            bool gotSolo = TryGetMinutes(listItems, "solo", out soloTtb);
            bool gotCoOp = TryGetMinutes(listItems, "co-op", out coOp);
            bool gotVs = TryGetMinutes(listItems, "vs", out vs);

            if (!gotMain && !gotExtras && !gotCompletionist && !gotSolo && !gotCoOp && !gotVs)
            {
                throw new FormatException("Could not find any TTB list item for HLTB ID " + hltbId);
            }

            HltbScraperEventSource.Log.ScrapeHltbInfoStop(hltbId, Math.Max(mainTtb, soloTtb), extrasTtb, completionistTtb);
            return new HltbInfo(mainTtb, extrasTtb, completionistTtb);
        }

        private static async Task<string> ScrapeHltbName(int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbNameStart(hltbId);

            var gamePageUrl = new Uri(String.Format(HltbGamePageFormat, hltbId));

            HltbScraperEventSource.Log.GetHltbGamePageStart(gamePageUrl);
            var doc = await LoadDocument(() => Client.GetAsync(gamePageUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.GetHltbGamePageStop(gamePageUrl);

            var headerDiv = doc.DocumentNode.Descendants().FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_header");
            if (headerDiv == null)
            {
                throw new FormatException("Can't parse name for HLTB ID " + hltbId);
            }

            var hltbName = headerDiv.InnerText.Trim();

            HltbScraperEventSource.Log.ScrapeHltbNameStop(hltbId, hltbName);
            return hltbName;
        }

        private static bool TryGetMinutes(IEnumerable<HtmlNode> listItems, string type, out int minutes)
        {
            var listItem = listItems.FirstOrDefault(hn => hn.InnerText != null && hn.InnerText.Contains(type, StringComparison.OrdinalIgnoreCase));
            if (listItem == null)
            {
                minutes = 0;
                return false;
            }

            var hoursDiv = listItem.Descendants("div").FirstOrDefault();
            if (hoursDiv == null)
            {
                throw new FormatException("TTB div not found inside list item");
            }

            var hoursStr = hoursDiv.InnerText;
            if (hoursStr == null)
            {
                throw new FormatException("Hours div inner text is null");
            }

            var match = Regex.Match(hoursStr, @"\s*(.+) Hour");
            if (match.Success && match.Groups.Count == 2)
            {
                double hours;
                if (!Double.TryParse(match.Groups[1].Value.Replace("&#189;", ".5"), out hours))
                {
                    throw new FormatException("Cannot parse duration from list item with text " + listItem.InnerText);                                        
                }
                minutes = (int) TimeSpan.FromHours(hours).TotalMinutes;
                return true;
            }
            
            match = Regex.Match(hoursStr, @"\s*(.+) Min");
            if (match.Success && match.Groups.Count == 2)
            {
                if (!Int32.TryParse(match.Groups[1].Value, out minutes))
                {
                    throw new FormatException("Cannot parse duration from list item with text " + listItem.InnerText);                                        
                }
                return true;
            }
            
            if (hoursStr.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                minutes = 0;
                return true;
            }

            throw new FormatException("Cannot parse duration from list item with text " + listItem.InnerText);                    
        }

        private static async Task<int> ScrapeHltbId(string appName)
        {
            HltbScraperEventSource.Log.ScrapeHltbIdStart(appName);

            var content = String.Format(SearchHltbPostDataFormat, appName);
            Func<HttpRequestMessage> requestFactory = () => new HttpRequestMessage(HttpMethod.Post, SearchHltbUrl)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            HltbScraperEventSource.Log.PostHltbSearchStart(SearchHltbUrl, content);
            var doc = await LoadDocument(() => Client.SendAsync(requestFactory, SearchHltbUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.PostHltbSearchStop(SearchHltbUrl, content);

            var first = doc.DocumentNode.Descendants("li").FirstOrDefault();
            if (first == null)
            {
                HltbScraperEventSource.Log.GameNotFoundInSearch(appName);
                return -1;
            }

            var anchor = first.Descendants("a").FirstOrDefault();
            if (anchor == null)
            {
                throw new FormatException("App anchor not found");
            }

            var link = anchor.GetAttributeValue("href", null);
            if (link == null)
            {
                throw new FormatException("App anchor does not include href attribute");
            }

            var idStr = link.Substring(12);
            int hltbId;
            if (!int.TryParse(idStr, out hltbId))
            {
                throw new FormatException("App link does not contain HLTB integer ID in expected location (expecting char12..end): " + idStr);
            }

            HltbScraperEventSource.Log.ScrapeHltbIdStop(appName, hltbId);
            return hltbId;
        }

        private static async Task<HtmlDocument> LoadDocument(Func<Task<HttpResponseMessage>> httpRequester)
        {
            var doc = new HtmlDocument();
            using (var response = await httpRequester().ConfigureAwait(false))
            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                doc.Load(responseStream);
            }
            return doc;
        }

        private static void PopulateAppEntity(AppEntity app, HltbInfo hltbInfo)
        {
            app.MainTtb = hltbInfo.MainTtb;
            app.ExtrasTtb = hltbInfo.ExtrasTtb;
            app.CompletionistTtb = hltbInfo.CompletionistTtb;
        }
    }
}