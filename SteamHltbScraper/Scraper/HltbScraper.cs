using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
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
using static System.FormattableString;

namespace SteamHltbScraper.Scraper
{
    public static class HltbScraper
    {
        private static readonly Uri SearchHltbUrl = new Uri(@"https://howlongtobeat.com/search_main.php?t=games&page=1&sorthead=popular&sortd=Normal%20Order&plat=&detail=0");
        private const string SearchHltbPostDataFormat = @"queryString={0}";

        public const string HltbGamePageFormat = @"https://www.howlongtobeat.com/game.php?id={0}";

        private static readonly int ScrapingLimit = SiteUtil.GetOptionalValueFromConfig("ScrapingLimit", int.MaxValue);
        private static readonly int ScrapingRetries = SiteUtil.GetOptionalValueFromConfig("HltbScraperScrapingRetries", 5);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("HltbScraperStorageRetries", 20);
        private static readonly int ResultCountConfidenceThreshold = SiteUtil.GetOptionalValueFromConfig("ResultCountConfidenceThreshold", 1000);

        private static readonly HashSet<int> NoTtbGames =
            new HashSet<int>(SiteUtil.GetOptionalValueFromConfig("NoTtbGames", "27859,27913,32770,42334").Split(',').Select(Int32.Parse));

        private static readonly HashSet<int> MalformedDateGames = 
            new HashSet<int>(SiteUtil.GetOptionalValueFromConfig("MalformedDateGames", "26294").Split(',').Select(Int32.Parse));

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
            var doc = new HtmlDocument();
            using (var response = await s_client.GetAsync<Stream>(gamePageUrl).ConfigureAwait(false))
            using (var stream = response.Content)
            {
                doc.Load(stream);    
            }
            
            HltbScraperEventSource.Log.GetHltbGamePageStop(gamePageUrl);

            var headerDiv = doc.DocumentNode.Descendants().FirstOrDefault(n => n.GetAttributeValue("class", null) == "profile_header");
            if (headerDiv == null)
            {
                throw new TransientHltbFaultException("Can't parse name for HLTB ID " + hltbId, doc);
            }

            var hltbName = headerDiv.InnerText.Trim();
            if (String.IsNullOrWhiteSpace(hltbName))
            {
                throw new TransientHltbFaultException("Empty name parsed for HLTB ID " + hltbId, doc);
            }

            var releaseDate = ScrapeReleaseDate(hltbId, doc);

            int mainTtb = 0, extrasTtb = 0, completionistTtb = 0;
            if (doc.DocumentNode.InnerHtml.Contains("This game has been flagged as an endless title")
                || doc.DocumentNode.InnerHtml.Contains("This game has been flagged as sports/unbeatable")
                || doc.DocumentNode.InnerHtml.Contains("This game has been flagged as multi-player only"))
            {
                HltbScraperEventSource.Log.GameFlaggedAsEndless(hltbName, hltbId);

                //we only submit a suggestion because we can't be sure this game has been correlated correctly to begin with
                await StorageHelper.InsertSuggestion(new SuggestionEntity(steamAppId, hltbId, AppEntity.EndlessTitleTypeName)).ConfigureAwait(false);
            }
            else if (!NoTtbGames.Contains(hltbId))
            {
                ScrapeTtbs(hltbId, doc, out mainTtb, out extrasTtb, out completionistTtb);                
            }

            HltbScraperEventSource.Log.ScrapeHltbInfoStop(hltbId, mainTtb, extrasTtb, completionistTtb, releaseDate.Year);
            return new HltbInfo(hltbName, mainTtb, extrasTtb, completionistTtb, releaseDate);
        }

        private static void ThrowOnInvalidDate(string releaseDate, DateTime date, int hltbId, HtmlDocument doc)
        {
            if (!StorageHelper.IsValid(date))
            {
                throw GetFormatException(Invariant($"Invalid release date: {releaseDate} (parsed as {date})"), hltbId, doc);
            }
        }

        private static DateTime ParseReleaseDate(string releaseDate, int hltbId, HtmlDocument doc)
        {
            DateTime date;
            if (DateTime.TryParse(releaseDate, out date))
            {
                ThrowOnInvalidDate(releaseDate, date, hltbId, doc);
                return date;
            }

            int year;
            if (Int32.TryParse(releaseDate, out year))
            {
                var ret = new DateTime(year, 1, 1);
                ThrowOnInvalidDate(releaseDate, ret, hltbId, doc);
                return ret;
            }

            if (MalformedDateGames.Contains(hltbId))
            {
                return AppEntity.UnknownDate;
            }

            throw GetFormatException(Invariant($"Could not parse release date: {releaseDate}"), hltbId, doc);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "profileinfo")]
        private static DateTime ScrapeReleaseDate(int hltbId, HtmlDocument doc)
        {
            var potentialDateNodes = doc.DocumentNode
                .Descendants("div")
                .Where(n => n.Attributes["class"]?.Value == "profile_info" && !string.IsNullOrWhiteSpace(n.InnerText))
                .ToArray();
                           
            if (potentialDateNodes.Length == 0)
            {
                throw GetFormatException("No potential date text nodes found (div .profile_info)", hltbId, doc);
            }

            var potentialDates = potentialDateNodes
                .Select(n => Regex.Match(n.InnerText.Trim(), "^[A-Z]{2}: (.*)$"))
                .Where(m => m.Groups.Count == 2)
                .Select(m => ParseReleaseDate(m.Groups[1].Value, hltbId, doc))
                .Where(d => d > AppEntity.UnknownDate) //discard known malformed dates
                .ToArray();

            return (potentialDates.Length == 0) ? AppEntity.UnknownDate : potentialDates.Min();
        }

        private static void ScrapeTtbs(int hltbId, HtmlDocument doc, out int mainTtb, out int extrasTtb, out int completionistTtb)
        {
            var list = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", null) == "game_times");
            if (list == null)
            {
                throw GetFormatException("Can't find list element", hltbId, doc);
            }

            var listItems = list.Descendants("li").Take(4).ToArray();

            bool gotMain = TryGetMinutes(listItems, "main", hltbId, doc, out var mainTtbInitial, out var dummy);
            bool gotExtras = TryGetMinutes(listItems, "extras", hltbId, doc, out var extrasTtbInitial, out dummy);
            bool gotCompletionist = TryGetMinutes(listItems, "completionist", hltbId, doc, out completionistTtb, out dummy);
            bool gotSolo = TryGetMinutes(listItems, "solo", hltbId, doc, out var soloTtb, out dummy);
            bool gotCoOp = TryGetMinutes(listItems, "co-op", hltbId, doc, out _, out dummy);
            bool gotVs = TryGetMinutes(listItems, "vs", hltbId, doc, out _, out dummy);
            bool gotSinglePlayer = TryGetMinutes(listItems, "single", hltbId, doc, out var singlePlayerMain, out var singlePlayerExtras);

            if (!gotMain && !gotExtras && !gotCompletionist && !gotSolo && !gotCoOp && !gotVs && !gotSinglePlayer)
            {
                throw new TransientHltbFaultException("Could not find any TTB list item for HLTB ID " + hltbId, doc);
            }

            mainTtb = Math.Max(mainTtbInitial, Math.Max(singlePlayerMain, soloTtb));
            extrasTtb = (singlePlayerExtras >= singlePlayerMain) ? singlePlayerExtras : extrasTtbInitial;
        }

        private static bool TryGetMinutes(IEnumerable<HtmlNode> listItems, string type, int hltbId, HtmlDocument doc, out int minutesFrom, out int minutesTo)
        {
            var durationListItem = listItems.FirstOrDefault(hn => hn.InnerText != null && hn.InnerText.Contains(type, StringComparison.OrdinalIgnoreCase));
            if (durationListItem == null)
            {
                minutesFrom = 0;
                minutesTo = -1;
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
	            if (durationTexts.Length == 1)
	            {
	                minutesTo = -1;
	            }
	            else
	            {
                    minutesTo = GetMinutes(durationTexts[1]);
                    if (minutesTo < minutesFrom)
                    {
                        SiteUtil.Swap(ref minutesFrom, ref minutesTo);
                    }  
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

            if (!ResultsFound(doc))
            {
                var letterNumberStrippedName = SiteUtil.ReplaceNonLettersAndNumbersWithSpaces(appName);
                if (letterNumberStrippedName == appName || String.IsNullOrWhiteSpace(letterNumberStrippedName))
                {
                    HltbScraperEventSource.Log.GameNotFoundInSearch(appName);
                    return -1;
                }

                HltbScraperEventSource.Log.SearchingForLetterNumberStrippedName(appName, letterNumberStrippedName);
                doc = await GetHltbSearchResults(letterNumberStrippedName).ConfigureAwait(false);
                if (!ResultsFound(doc))
                {
                    HltbScraperEventSource.Log.GameNotFoundInSearch(appName);
                    return -1;
                }
            }

            var searchResultsTitle = doc.DocumentNode.Descendants("h3").FirstOrDefault();
            if (String.IsNullOrWhiteSpace(searchResultsTitle?.InnerText))
            {
                throw GetFormatException("Could not find h3 search results title for confidence evaluation", appName, doc);
            }

            var match = Regex.Match(searchResultsTitle.InnerText, "We Found (.*?) Game");
            if (!match.Success || !Int32.TryParse(match.Groups[1].Value, out var resultCount))
            {
                throw GetFormatException("Unexpected search results title format: " + searchResultsTitle.InnerText, appName, doc);
            }

            HltbScraperEventSource.Log.GamesFoundInSearch(appName, resultCount);
            if (resultCount > ResultCountConfidenceThreshold)
            {
                HltbScraperEventSource.Log.ResultCountExceedsConfidenceThreshold(appName, resultCount, ResultCountConfidenceThreshold);
                return -1;
            }

            var link = doc.DocumentNode.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", null);
            if (link == null)
            {
                throw GetFormatException("Could not find app anchor, or it does not include an href attribute", appName, doc);
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

        private static bool ResultsFound(HtmlDocument doc)
        {
            return !doc.DocumentNode.InnerText.StartsWith("No results for", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<HtmlDocument> GetHltbSearchResults(string appName)
        {
            var content = String.Format(SearchHltbPostDataFormat, appName);

            HttpRequestMessage RequestFactory() => new HttpRequestMessage(HttpMethod.Post, SearchHltbUrl)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            HltbScraperEventSource.Log.PostHltbSearchStart(SearchHltbUrl, content);

            var doc = new HtmlDocument();
            using (var response = await s_client.SendAsync<Stream>(RequestFactory, SearchHltbUrl))
            using (var stream = response.Content)
            {
                doc.Load(stream);
            }
            HltbScraperEventSource.Log.PostHltbSearchStop(SearchHltbUrl, content);
            
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
        public HtmlDocument Document { get; }
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