﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
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
        private static readonly Uri SearchHltbUrl = new Uri(@"http://howlongtobeat.com/search_main.php?t=games&page=1&sorthead=popular&sortd=Normal%20Order&plat=&detail=0");
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        public const string HltbGamePageFormat = @"http://www.howlongtobeat.com/game.php?id={0}";

        private static readonly int ScrapingLimit = SiteUtil.GetOptionalValueFromConfig("ScrapingLimit", int.MaxValue);
        private static readonly int ScrapingRetries = SiteUtil.GetOptionalValueFromConfig("HltbScraperScrapingRetries", 5);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("HltbScraperStorageRetries", 20);

        private static readonly HashSet<int> NoTtbGames =
            new HashSet<int>(SiteUtil.GetOptionalValueFromConfig("NoTtbGames", "27859,27913").Split(',').Select(Int32.Parse));

        private static HttpRetryClient s_client;

        private static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("HltbScraper");
                MainAsync().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task MainAsync()
        {
            var tickCount = Environment.TickCount;
            SiteUtil.SetDefaultConnectionLimit();

            var allMeasuredApps = (await StorageHelper.GetAllApps(AppEntity.MeasuredFilter, StorageRetries).ConfigureAwait(false)).Take(ScrapingLimit).ToArray();

            await ScrapeHltb(allMeasuredApps).ConfigureAwait(false);

            await Imputer.Impute(allMeasuredApps).ConfigureAwait(false);

            //we're using Replace since the only other update to an existing game-typed entity would have to be manual which should take precedence
            await StorageHelper.Replace(allMeasuredApps, "updating scraped gametimes", StorageHelper.SteamToHltbTableName, StorageRetries).ConfigureAwait(false);

            await SiteUtil.SendSuccessMail("HLTB scraper", allMeasuredApps.Length + " game(s) scraped", tickCount).ConfigureAwait(false);
        }

        public static async Task ScrapeHltb(AppEntity[] allApps, Action<AppEntity, Exception> errorHandler = null)
        {
            using (s_client = new HttpRetryClient(ScrapingRetries))
            {
                await ScrapeHltbCore(allApps, errorHandler).ConfigureAwait(false);
            }
        }

        internal static async Task ScrapeHltbCore(AppEntity[] allApps, Action<AppEntity, Exception> errorHandler = null)
        {
            errorHandler = errorHandler ?? ( (a,e) => { });
            HltbScraperEventSource.Log.ScrapeHltbStart();

            int count = 0;

            await allApps.ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async app =>
            {
                var current = Interlocked.Increment(ref count);
                HltbScraperEventSource.Log.ScrapeGameStart(app.SteamAppId, current, allApps.Length);

                if (app.HltbId == -1)
                {
                    try
                    {
                        app.HltbId = await ScrapeHltbId(app.SteamName).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        HltbScraperEventSource.Log.ErrorScrapingHltbId(current, app.SteamAppId, app.SteamName, e);
                        errorHandler(app, e);
                        return;
                    }

                    if (app.HltbId == -1)
                    {
                        return; //app not found in search
                    }
                }

                HltbInfo hltbInfo;
                try
                {
                    hltbInfo = await ScrapeWithExponentialRetries(hltbId => ScrapeHltbInfo(app.SteamAppId, hltbId), app.HltbId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    MakeVerbose(e);
                    HltbScraperEventSource.Log.ErrorScrapingHltbInfo(current, app.SteamAppId, app.SteamName, e);
                    errorHandler(app, e);
                    return;
                }

                PopulateAppEntity(app, hltbInfo);

                HltbScraperEventSource.Log.ScrapeGameStop(app.SteamAppId, current, allApps.Length);
            }).ConfigureAwait(false);
            
            HltbScraperEventSource.Log.ScrapeHltbStop();
        }

        public static Task<T> ScrapeWithExponentialRetries<T>(Func<int, Task<T>> scraper, int hltbId)
        {
            return ExponentialBackoff.ExecuteAsyncWithExponentialRetries(
                        () => scraper(hltbId),
                        (lastException, retryCount, delay) => 
                            HltbScraperEventSource.Log.TransientHltbFault(hltbId, lastException.Message, retryCount, ScrapingRetries, (int)delay.TotalSeconds),
                        ex => ex is TransientHltbFaultException,
                        ScrapingRetries, HttpRetryClient.MinBackoff, HttpRetryClient.MaxBackoff, HttpRetryClient.DefaultClientBackoff,
                        CancellationToken.None);
        }

        private static void MakeVerbose(Exception e)
        {
            var transientFaultException = e as TransientHltbFaultException;
            if (transientFaultException != null)
            {
                transientFaultException.PrintDocument = true;
            }
        }

        private static FormatException GetFormatException(string message, string steamName, HtmlDocument doc, Exception inner = null)
        {
            return GetFormatExceptionCore(message, "Steam name: " + steamName, doc, inner);
        }
        private static FormatException GetFormatException(string message, int hltbId, HtmlDocument doc, Exception inner = null)
        {
            return GetFormatExceptionCore(message, "HLTB ID: " + hltbId, doc, inner);
        }
        private static FormatException GetFormatExceptionCore(string message, string id, HtmlDocument doc, Exception inner = null)
        {
            return new FormatException(String.Format(CultureInfo.InvariantCulture, "{0}. {1}. Document: {2}", message, id, doc.DocumentNode.OuterHtml), inner);
        }

        private static async Task<HltbInfo> ScrapeHltbInfo(int steamAppId, int hltbId)
        {
            HltbScraperEventSource.Log.ScrapeHltbInfoStart(hltbId);

            var gamePageUrl = new Uri(String.Format(HltbGamePageFormat, hltbId));

            HltbScraperEventSource.Log.GetHltbGamePageStart(gamePageUrl);
            var doc = await LoadDocument(() => s_client.GetAsync(gamePageUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.GetHltbGamePageStop(gamePageUrl);

            var headerDiv = doc.DocumentNode.Descendants().FirstOrDefault(n => n.GetAttributeValue("class", null) == "profile_header");
            if (headerDiv == null)
            {
                throw GetFormatException("Can't parse name", hltbId, doc);
            }

            var hltbName = headerDiv.InnerText.Trim();
            if (String.IsNullOrWhiteSpace(hltbName))
            {
                throw new TransientHltbFaultException("Empty name parsed for HLTB ID " + hltbId, doc);
            }

            var releaseDate = ScrapeReleaseDate(hltbId, doc);

            int mainTtb = 0, extrasTtb = 0, completionistTtb = 0;
            if (!NoTtbGames.Contains(hltbId))
            {
                ScrapeTtbs(hltbId, doc, out mainTtb, out extrasTtb, out completionistTtb);                
            }

            if (doc.DocumentNode.InnerHtml.Contains("This game has been flagged as an endless title"))
            {
                HltbScraperEventSource.Log.GameFlaggedAsEndless(hltbName, hltbId);

                //we only submit a suggestion because we can't be sure this game has been correlated correctly to begin with
                await StorageHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId, AppEntity.EndlessTitleTypeName)).ConfigureAwait(false);
            }

            HltbScraperEventSource.Log.ScrapeHltbInfoStop(hltbId, mainTtb, extrasTtb, completionistTtb, releaseDate.Year);
            return new HltbInfo(hltbName, mainTtb, extrasTtb, completionistTtb, releaseDate);
        }

        private static DateTime ScrapeReleaseDate(int hltbId, HtmlDocument doc)
        {
            var releaseDatesTitle = doc.DocumentNode.Descendants("h5").FirstOrDefault(n => n.InnerText.Contains("Release Date(s):"));
            if (releaseDatesTitle == null)
            {
                return AppEntity.UnknownDate;
            }

            var releaseDateDiv = releaseDatesTitle.ParentNode;
            if (releaseDateDiv == null)
            {
                throw GetFormatException("Release date title has no parent", hltbId, doc);
            }

            var minDate = DateTime.MaxValue;
            foreach (var dateNode in releaseDateDiv.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text))
            {
                DateTime date;
                if (!DateTime.TryParse(dateNode.InnerText, out date))
                {
                    int year;
                    if (!Int32.TryParse(dateNode.InnerText, out year))
                    {
                        continue;
                    }
                    date = new DateTime(year, 1, 1);
                }
                
                if (date < minDate)
                {
                    minDate = date;
                }
            }

            return (minDate == DateTime.MaxValue) ? AppEntity.UnknownDate : minDate;
        }

        private static void ScrapeTtbs(int hltbId, HtmlDocument doc, out int mainTtb, out int extrasTtb, out int completionistTtb)
        {
            var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "game_times");
            if (list == null)
            {
                throw GetFormatException("Can't find list element", hltbId, doc);
            }

            var listItems = list.Descendants("li").Take(4).ToArray();

            int mainTtbInitial, extrasTtbInitial, soloTtb, coOp, vs, singlePlayerMain, singlePlayerExtras, dummy;

            bool gotMain = TryGetMinutes(listItems, "main", hltbId, doc, out mainTtbInitial, out dummy);
            bool gotExtras = TryGetMinutes(listItems, "extras", hltbId, doc, out extrasTtbInitial, out dummy);
            bool gotCompletionist = TryGetMinutes(listItems, "completionist", hltbId, doc, out completionistTtb, out dummy);
            bool gotSolo = TryGetMinutes(listItems, "solo", hltbId, doc, out soloTtb, out dummy);
            bool gotCoOp = TryGetMinutes(listItems, "co-op", hltbId, doc, out coOp, out dummy);
            bool gotVs = TryGetMinutes(listItems, "vs", hltbId, doc, out vs, out dummy);
            bool gotSinglePlayer = TryGetMinutes(listItems, "single", hltbId, doc, out singlePlayerMain, out singlePlayerExtras);

            if (!gotMain && !gotExtras && !gotCompletionist && !gotSolo && !gotCoOp && !gotVs && !gotSinglePlayer)
            {
                throw new TransientHltbFaultException("Could not find any TTB list item for HLTB ID " + hltbId, doc);
            }

            mainTtb = Math.Max(mainTtbInitial, Math.Max(singlePlayerMain, soloTtb));
            extrasTtb = (singlePlayerExtras > singlePlayerMain) ? singlePlayerExtras : extrasTtbInitial;
        }

        private static bool TryGetMinutes(IEnumerable<HtmlNode> listItems, string type, int hltbId, HtmlDocument doc, out int minutesFrom, out int minutesTo)
        {
            var durationListItem = listItems.FirstOrDefault(hn => hn.InnerText != null && hn.InnerText.Contains(type, StringComparison.OrdinalIgnoreCase));
            if (durationListItem == null)
            {
                minutesFrom = minutesTo = 0;
                return false;
            }

            var durationDiv = durationListItem.Descendants("div").FirstOrDefault();
            if (durationDiv == null)
            {
                throw GetFormatException("TTB div not found inside list item", hltbId, doc);
            }

            var durationText = durationDiv.InnerText;
            if (durationText == null)
            {
                throw GetFormatException("Hours div inner text is null", hltbId, doc);
            }

            var durationTexts = durationText.Split(new [] {" - "}, StringSplitOptions.RemoveEmptyEntries);
            if (durationTexts.Length > 2)
            {
                throw GetFormatException("Cannot parse duration (invalid range) from list item with text: " + durationListItem.InnerText, hltbId, doc);                                        
            }

            try 
	        {
                minutesFrom = GetMinutes(durationTexts[0]);
                minutesTo = durationTexts.Length == 1 ? minutesFrom : GetMinutes(durationTexts[1]);
	            if (minutesTo < minutesFrom)
	            {
	                SiteUtil.Swap(ref minutesFrom, ref minutesTo);
	            }
	        }
	        catch (FormatException e)
	        {
                throw GetFormatException("Cannot parse duration from list item with text: " + durationListItem.InnerText, hltbId, doc, e);                                        
	        }
            catch (OverflowException e)
            {
                throw GetFormatException("Cannot parse duration (overflow) from list item with text: " + durationListItem.InnerText, hltbId, doc, e);                                        
            }

            return true;
        }

        private static int GetMinutes(string durationText)
        {
            if (durationText.Contains("--") || durationText.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            double hours;
            if (TryGetDuration(durationText, @"\s*(.+) Hour", out hours))
            {
                return (int)TimeSpan.FromHours(hours).TotalMinutes;
            }

            double minutes;
            if (TryGetDuration(durationText, @"\s*(.+) Min", out minutes))
            {
                return (int)minutes;
            }

            throw new FormatException("Could not find duration specifier");
        }

        private static bool TryGetDuration(string durationText, string pattern, out double duration)
        {
            var match = Regex.Match(durationText, pattern);
            if (match.Success && match.Groups.Count == 2)
            {
                duration = Double.Parse(match.Groups[1].Value.Replace("&#189;", ".5"), CultureInfo.InvariantCulture);
                return true;
            }

            duration = 0;
            return false;
        }

        private static async Task<int> ScrapeHltbId(string appName)
        {
            HltbScraperEventSource.Log.ScrapeHltbIdStart(appName);

            var doc = await GetHltbSearchResults(appName).ConfigureAwait(false);
            var firstResultAnchor = doc.DocumentNode.Descendants("a").FirstOrDefault();

            if (firstResultAnchor == null)
            {
                var alphanumericName = SiteUtil.ReplaceNonAlphanumericWithSpaces(appName);
                if (alphanumericName != appName)
                {
                    HltbScraperEventSource.Log.SearchingForAlphanumericName(appName, alphanumericName);
                    doc = await GetHltbSearchResults(alphanumericName).ConfigureAwait(false);
                    firstResultAnchor = doc.DocumentNode.Descendants("a").FirstOrDefault(); 
                }

                if (firstResultAnchor == null)
                {
                    HltbScraperEventSource.Log.GameNotFoundInSearch(appName);
                    return -1;   
                }
            }

            var link = firstResultAnchor.GetAttributeValue("href", null);
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

        private static async Task<HtmlDocument> GetHltbSearchResults(string appName)
        {
            var content = String.Format(SearchHltbPostDataFormat, appName);
            Func<HttpRequestMessage> requestFactory = () => new HttpRequestMessage(HttpMethod.Post, SearchHltbUrl)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            HltbScraperEventSource.Log.PostHltbSearchStart(SearchHltbUrl, content);
            var doc = await LoadDocument(() => s_client.SendAsync(requestFactory, SearchHltbUrl)).ConfigureAwait(false);
            HltbScraperEventSource.Log.PostHltbSearchStop(SearchHltbUrl, content);
            
            return doc;
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
            app.HltbName = hltbInfo.Name;

            var mainTtb = GetTtb(app, "main", app.MainTtb, app.MainTtbImputed, hltbInfo.MainTtb);
            app.SetMainTtb(mainTtb, mainTtb == 0);

            var extrasTtb = GetTtb(app, "extras", app.ExtrasTtb, app.ExtrasTtbImputed, hltbInfo.ExtrasTtb);
            app.SetExtrasTtb(extrasTtb, extrasTtb == 0);

            var completionistTtb = GetTtb(app, "completionist", app.CompletionistTtb, app.CompletionistTtbImputed, hltbInfo.CompletionistTtb);
            app.SetCompletionistTtb(completionistTtb, completionistTtb == 0);

            if (hltbInfo.ReleaseDate != AppEntity.UnknownDate)
            {
                app.ReleaseDate = hltbInfo.ReleaseDate;
            }
        }

        private static int GetTtb(AppEntity app, string ttbType, int currentTtb, bool currentTtbImputed, int scrapedTtb)
        {
            if (!currentTtbImputed && scrapedTtb == 0)
            {
                HltbScraperEventSource.Log.PreviouslyRecordedTtbNotOnHltb(app.SteamName, app.SteamAppId, ttbType, currentTtb, app.HltbName, app.HltbId);
            }
            
            return scrapedTtb;
        }
    }

    public class TransientHltbFaultException : Exception
    {
        public HtmlDocument Document { get; private set; }
        public bool PrintDocument { get; set; }

        public TransientHltbFaultException(string message, HtmlDocument doc) : base(message)
        {
            Document = doc;
            PrintDocument = false;
        }

        public override string ToString()
        {
            return base.ToString() + (PrintDocument ? (Environment.NewLine + Document.DocumentNode.OuterHtml) : String.Empty);
        }
    }
}