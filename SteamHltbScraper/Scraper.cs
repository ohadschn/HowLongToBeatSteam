﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common;
using HtmlAgilityPack;

namespace SteamHltbScraper
{
    public static class Scraper
    {
        private const string SearchHltbUrl = @"http://www.howlongtobeat.com/search_main.php?t=games&page=1&sorthead=&sortd=Normal&plat=&detail=0";
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        private const string HltbGamePageFormat = @"http://www.howlongtobeat.com/game.php?id={0}";
        private const string HltbGameOverviewPageFormat = @"http://www.howlongtobeat.com/game_overview.php?id={0}";

        private static readonly int ScrapingLimit = Int32.Parse(ConfigurationManager.AppSettings["ScrapingLimit"], CultureInfo.InvariantCulture);
        private static readonly HttpRetryClient Client = new HttpRetryClient(4);

        private static void Main()
        {
            SiteUtil.SetDefaultConnectionLimit();
            using (Client)
            {
                ScrapeHltb().Wait();
            }
        }

        private static async Task ScrapeHltb()
        {
            HltbScraperEventSource.Log.ScrapeHltbStart();

            var updates = new ConcurrentBag<AppEntity>();
            var apps = await TableHelper.GetAllApps(e => e, AppEntity.MeasuredFilter, 20).ConfigureAwait(false);
            int count = 0;

            await apps.Take(ScrapingLimit).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async app =>
            {
                var current = Interlocked.Increment(ref count);
                HltbScraperEventSource.Log.ScrapeGameStart(app.SteamAppId, current);
                
                bool added = false;
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

                    updates.Add(app);
                    added = true;
                }

                string hltbName;
                try
                {
                    hltbName = await ScrapeHltbName(app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HltbScraperEventSource.Log.ErrorScrapingHltbName(current, app.SteamAppId, app.SteamName, app.HltbId, e);
                    return;
                }

                if (hltbName != app.HltbName)
                {
                    app.HltbName = hltbName;
                    if (!added)
                    {
                        updates.Add(app);
                        added = true;
                    }
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
                if (!added)
                {
                    updates.Add(app);
                }
                HltbScraperEventSource.Log.ScrapeGameStop(app.SteamAppId, current);

            }).ConfigureAwait(false);

            //we're using Replace since the only other update to an existing game-typed entity would have to be manual which should take precedence
            await TableHelper.Replace(updates, 20).ConfigureAwait(false); 
            
            HltbScraperEventSource.Log.ScrapeHltbStop();
        }

        private static async Task<HltbInfo> ScrapeHltbInfo(int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbInfoStart(hltbId);

            var gameOverviewUrl = String.Format(HltbGameOverviewPageFormat, hltbId);

            HltbScraperEventSource.Log.GetGameOverviewPageStart(gameOverviewUrl);
            var doc = await LoadDocument(() => Client.GetAsync(gameOverviewUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.GetGameOverviewPageStop(gameOverviewUrl);

            var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_times");
            if (list == null)
            {
                throw new FormatException("Can't find list element - HLTB ID " + hltbId);
            }

            var listItems = list.Descendants("li").Take(4).ToArray();
            
            int mainTtb, extrasTtb, completionistTtb, combinedTtb, solo, coOp, vs;
            bool gotMain = TryGetMinutes(listItems, "main", out mainTtb);
            bool gotExtras = TryGetMinutes(listItems, "extras", out extrasTtb);
            bool gotCompletionist = TryGetMinutes(listItems, "completionist", out completionistTtb);
            bool gotCombined = TryGetMinutes(listItems, "combined", out combinedTtb);
            bool gotSolo = TryGetMinutes(listItems, "solo", out solo);
            bool gotCoOp = TryGetMinutes(listItems, "co-op", out coOp);
            bool gotVs = TryGetMinutes(listItems, "vs", out vs);

            if (!gotMain && !gotExtras && !gotCompletionist && !gotCombined && !gotSolo && !gotCoOp && !gotVs)
            {
                throw new FormatException("Could not find any TTB list item for HLTB ID " + hltbId);
            }

            HltbScraperEventSource.Log.ScrapeHltbInfoStop(hltbId, mainTtb, extrasTtb, completionistTtb, combinedTtb, solo, coOp, vs);
            return new HltbInfo(mainTtb, extrasTtb, completionistTtb, combinedTtb, solo, coOp, vs);
        }

        private static async Task<string> ScrapeHltbName(int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbNameStart(hltbId);

            var gamePageUrl = String.Format(HltbGamePageFormat, hltbId);

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
                Content = new StringContent(content, Encoding.UTF8,"application/x-www-form-urlencoded")
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
            app.CombinedTtb = hltbInfo.CombinedTtb;
            app.SoloTtb = hltbInfo.Solo;
            app.CoOpTtb = hltbInfo.CoOp;
            app.VsTtb = hltbInfo.Vs;
        }
    }
}