using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.Serialization;
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
        private static readonly Uri SearchHltbUrl = new Uri(@"http://howlongtobeat.com/search_main.php?t=games&page=1&sorthead=popular&sortd=Normal%20Order&plat=&detail=0");
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        private const string HltbGamePageFormat = @"http://www.howlongtobeat.com/game.php?id={0}";
        private const string HltbGameOverviewPageFormat = @"http://www.howlongtobeat.com/game_overview.php?id={0}";

        private static readonly int ScrapingLimit = SiteUtil.GetOptionalValueFromConfig("ScrapingLimit", int.MaxValue);
        private const int Retries = 5;
        private static readonly HttpRetryClient Client = new HttpRetryClient(Retries);

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

            using (Client)
            {
                await ScrapeHltb(allApps).ConfigureAwait(false);
            }

            await Imputer.Impute(allApps).ConfigureAwait(false);

            //we're using Replace since the only other update to an existing game-typed entity would have to be manual which should take precedence
            await StorageHelper.ReplaceApps(allApps, 20).ConfigureAwait(false); 
        }

        internal static async Task ScrapeHltb(IEnumerable<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.ScrapeHltbStart();

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
                    app.HltbName = await ScrapeWithExponentialRetries(ScrapeHltbName, app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HltbScraperEventSource.Log.ErrorScrapingHltbName(current, app.SteamAppId, app.SteamName, app.HltbId, e);
                    return;
                }

                HltbInfo hltbInfo;
                try
                {
                    hltbInfo = await ScrapeWithExponentialRetries(ScrapeHltbInfo, app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    HltbScraperEventSource.Log.ErrorScrapingHltbInfo(current, app.SteamAppId, app.SteamName, e);
                    return;
                }

                PopulateAppEntity(app, hltbInfo);

                HltbScraperEventSource.Log.ScrapeGameStop(app.SteamAppId, current);
            }).ConfigureAwait(false);
            
            HltbScraperEventSource.Log.ScrapeHltbStop();
        }

        private static Task<T> ScrapeWithExponentialRetries<T>(Func<int, Task<T>> scraper, int hltbId)
        {
            return ExponentialBackoff.ExecuteAsyncWithExponentialRetries(
                        () => scraper(hltbId),
                        (le, retryCount, delay) => HltbScraperEventSource.Log.FlaggedGameEncountered(hltbId, retryCount, Retries, (int)delay.TotalSeconds),
                        ex => ex is FlaggedGameException,
                        Retries, HttpRetryClient.MinBackoff, HttpRetryClient.MaxBackoff, HttpRetryClient.DefaultClientBackoff,
                        CancellationToken.None);
        }

        private static FormatException GetFormatException(string message, string steamName, HtmlDocument doc)
        {
            return GetFormatExceptionCore(message, "Steam name: " + steamName, doc);
        }
        private static FormatException GetFormatException(string message, int hltbId, HtmlDocument doc)
        {
            return GetFormatExceptionCore(message, "HLTB ID: " + hltbId, doc);
        }
        private static FormatException GetFormatExceptionCore(string message, string id, HtmlDocument doc)
        {
            return new FormatException(String.Format(CultureInfo.InvariantCulture, "{0}. {1}. Document: {2}", message, id, doc.DocumentNode.OuterHtml));
        }

        private static void VerifyGame(HtmlDocument doc, int hltbId)
        {
            if (String.IsNullOrWhiteSpace(doc.DocumentNode.OuterHtml) 
                || doc.DocumentNode.OuterHtml.Contains("This game has been flagged", StringComparison.OrdinalIgnoreCase))
            {
                throw new FlaggedGameException(GetFormatException("Flagged game encountered", hltbId, doc));
            }
        }

        private static async Task<HltbInfo> ScrapeHltbInfo(int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbInfoStart(hltbId);

            var gameOverviewUrl = new Uri(String.Format(HltbGameOverviewPageFormat, hltbId));

            HltbScraperEventSource.Log.GetGameOverviewPageStart(gameOverviewUrl);
            var doc = await LoadDocument(() => Client.GetAsync(gameOverviewUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.GetGameOverviewPageStop(gameOverviewUrl);

            VerifyGame(doc, hltbId);

            var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_times");
            if (list == null)
            {
                throw GetFormatException("Can't find list element", hltbId, doc);
            }

            var listItems = list.Descendants("li").Take(4).ToArray();
            
            int mainTtb, extrasTtb, completionistTtb, soloTtb, coOp, vs;
            bool gotMain = TryGetMinutes(listItems, "main", hltbId, doc, out mainTtb);
            bool gotExtras = TryGetMinutes(listItems, "extras", hltbId, doc, out extrasTtb);
            bool gotCompletionist = TryGetMinutes(listItems, "completionist", hltbId, doc, out completionistTtb);
            bool gotSolo = TryGetMinutes(listItems, "solo", hltbId, doc, out soloTtb);
            bool gotCoOp = TryGetMinutes(listItems, "co-op", hltbId, doc, out coOp);
            bool gotVs = TryGetMinutes(listItems, "vs", hltbId, doc, out vs);

            if (!gotMain && !gotExtras && !gotCompletionist && !gotSolo && !gotCoOp && !gotVs)
            {
                throw GetFormatException("Could not find any TTB list item", hltbId, doc);
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

            VerifyGame(doc, hltbId);

            var headerDiv = doc.DocumentNode.Descendants().FirstOrDefault(n => n.GetAttributeValue("class", null) == "gprofile_header");
            if (headerDiv == null)
            {
                throw GetFormatException("Can't parse name", hltbId, doc);
            }

            var hltbName = headerDiv.InnerText.Trim();
            if (String.IsNullOrWhiteSpace(hltbName))
            {
                throw GetFormatException("Empty name parsed", hltbId, doc);
            }

            HltbScraperEventSource.Log.ScrapeHltbNameStop(hltbId, hltbName);
            return hltbName;
        }

        private static bool TryGetMinutes(IEnumerable<HtmlNode> listItems, string type, int hltbId, HtmlDocument doc, out int minutes)
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
                throw GetFormatException("TTB div not found inside list item", hltbId, doc);
            }

            var hoursStr = hoursDiv.InnerText;
            if (hoursStr == null)
            {
                throw GetFormatException("Hours div inner text is null", hltbId, doc);
            }

            var match = Regex.Match(hoursStr, @"\s*(.+) Hour");
            if (match.Success && match.Groups.Count == 2)
            {
                double hours;
                if (!Double.TryParse(match.Groups[1].Value.Replace("&#189;", ".5"), out hours))
                {
                    throw GetFormatException("Cannot parse duration from list item with text " + listItem.InnerText, hltbId, doc);                                        
                }
                minutes = (int) TimeSpan.FromHours(hours).TotalMinutes;
                return true;
            }
            
            match = Regex.Match(hoursStr, @"\s*(.+) Min");
            if (match.Success && match.Groups.Count == 2)
            {
                if (!Int32.TryParse(match.Groups[1].Value, out minutes))
                {
                    throw GetFormatException("Cannot parse duration from list item with text " + listItem.InnerText, hltbId, doc);                                        
                }
                return true;
            }
            
            if (hoursStr.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                minutes = 0;
                return true;
            }

            throw GetFormatException("Cannot parse duration from list item with text " + listItem.InnerText, hltbId, doc);                    
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
                throw GetFormatException("App anchor not found", appName, doc);
            }

            var link = anchor.GetAttributeValue("href", null);
            if (link == null)
            {
                throw GetFormatException("App anchor does not include href attribute", appName, doc);
            }

            var idStr = link.Substring(12);
            int hltbId;
            if (!int.TryParse(idStr, out hltbId))
            {
                throw GetFormatException("App link does not contain HLTB integer ID in expected location (expecting char12..end): " + idStr, appName, doc);
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
            var mainTtb = GetTtb(app, "main", app.MainTtb, app.MainTtbImputed, hltbInfo.MainTtb);
            app.SetMainTtb(mainTtb, mainTtb == 0);

            var extrasTtb = GetTtb(app, "extras", app.ExtrasTtb, app.ExtrasTtbImputed, hltbInfo.ExtrasTtb);
            app.SetExtrasTtb(extrasTtb, extrasTtb == 0);

            var completionistTtb = GetTtb(app, "completionist", app.CompletionistTtb, app.CompletionistTtbImputed, hltbInfo.CompletionistTtb);
            app.SetCompletionistTtb(completionistTtb, completionistTtb == 0);
        }

        private static int GetTtb(AppEntity app, string ttbType, int currentTtb, bool currentTtbImputed, int scrapedTtb)
        {
            if (!currentTtbImputed && scrapedTtb == 0)
            {
                HltbScraperEventSource.Log.PreviouslyRecordedTtbNotOnHltb(app.SteamName, app.SteamAppId, ttbType, currentTtb, app.HltbName, app.HltbId);
                return currentTtb;
            }
            
            return scrapedTtb;
        }
    }

    [Serializable]
    public class FlaggedGameException : Exception
    {
        public FlaggedGameException() : this((string)null)
        {

        }
        public FlaggedGameException(Exception exception) : this(exception == null ? String.Empty : exception.Message, exception)
        {
        }

        public FlaggedGameException(string message) : this(message, null)
        {
        }

        public FlaggedGameException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FlaggedGameException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}