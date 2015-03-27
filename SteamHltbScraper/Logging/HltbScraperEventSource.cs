using System;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace SteamHltbScraper.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-Scraper")]
    public class HltbScraperEventSource : EventSourceBase
    {
        public static readonly HltbScraperEventSource Log = new HltbScraperEventSource();

        private HltbScraperEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords Scraping = (EventKeywords) 1;
            public const EventKeywords Http = (EventKeywords)2;
            public const EventKeywords Imputation = (EventKeywords)4;
            public const EventKeywords AzureML = (EventKeywords)8;
            public const EventKeywords BlobStorage = (EventKeywords)16;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask ScrapeHltb = (EventTask) 1;
            public const EventTask ScrapeGame = (EventTask) 2;
            public const EventTask ScrapeHltbId = (EventTask) 3;
            public const EventTask PostHltbSearch = (EventTask) 4;
            public const EventTask ScrapeHltbName = (EventTask) 5;
            public const EventTask GetHltbGamePage = (EventTask) 6;
            public const EventTask ScrapeHltbInfo = (EventTask) 7;
            public const EventTask GetGameOverviewPage = (EventTask) 8;
            public const EventTask CalculateImputation = (EventTask) 9;
            public const EventTask SubmitImputationJob = (EventTask) 10;
            public const EventTask Impute = (EventTask) 11;
            public const EventTask UploadTtbToBlob = (EventTask) 12;
            public const EventTask PollImputationJobStatus = (EventTask) 13;
            public const EventTask ImputeGenre = (EventTask) 14;
        }
// ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start scraping HLTB",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltb,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished scraping HLTB",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltb,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbStop()
        {
            WriteEvent(2);
        }

        [Event(
            3,
            Message = "Start scraping Steam ID {0} (#{1})",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeGame,
            Opcode = EventOpcode.Start)]
        public void ScrapeGameStart(int steamAppId, int current)
        {
            WriteEvent(3, steamAppId, current);
        }

        [Event(
            4,
            Message = "Finished scraping Steam ID {0} (#{1})",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeGame,
            Opcode = EventOpcode.Stop)]
        public void ScrapeGameStop(int steamAppId, int current)
        {
            WriteEvent(4, steamAppId, current);
        }

        [Event(
            5,
            Message = "Start scraping HLTB ID for '{0}'",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbId,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbIdStart(string appName)
        {
            WriteEvent(5, appName);
        }

        [Event(
            6,
            Message = "Finished scraping HLTB ID for '{0}': {1}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbId,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbIdStop(string appName, int hltbId)
        {
            WriteEvent(6, appName, hltbId);
        }

        [NonEvent]
        public void PostHltbSearchStart(Uri uri, string content)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            PostHltbSearchStart(uri.ToString(), content);     
        }

        [Event(
            7,
            Message = "Start posting search query to {0} (content: {1})",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.PostHltbSearch,
            Opcode = EventOpcode.Start)]
        private void PostHltbSearchStart(string uri, string content)
        {
            WriteEvent(7, uri, content);
        }

        [NonEvent]
        public void PostHltbSearchStop(Uri uri, string content)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            PostHltbSearchStop(uri.ToString(), content);
            
        }

        [Event(
            8,
            Message = "Finished posting search query to {0} (content: {1})",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.PostHltbSearch,
            Opcode = EventOpcode.Stop)]
        private void PostHltbSearchStop(string uri, string content)
        {
            WriteEvent(8, uri, content);
        }

        [Event(
            9,
            Message = "Game '{0}' not found in search",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Warning)]
        public void GameNotFoundInSearch(string game)
        {
            WriteEvent(9, game);
        }

        [NonEvent]
        public void ErrorScrapingHltbId(int current, int steamAppId, string steamName, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (!IsEnabled())
            {
                return;
            }

            ErrorScrapingHltbId(current, steamAppId, steamName, exception.ToString());
        }

        [Event(
            10,
            Message = "Scraping #{0} - Error resolving HLTB ID for app {1} / {2}: {3}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbId(int current, int steamAppId, string steamName, string exception)
        {
            WriteEvent(10, current, steamAppId, steamName, exception);
        }

        [Event(
            11,
            Message = "Start scraping HLTB name for id {0}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbName,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbNameStart(int hltbId)
        {
            WriteEvent(11, hltbId);
        }

        [Event(
            12,
            Message = "Finished scraping HLTB name for id {0}: {1}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbName,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbNameStop(int hltbId, string hltbName)
        {
            WriteEvent(12, hltbId, hltbName);
        }

        [NonEvent]
        public void GetHltbGamePageStart(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            GetHltbGamePageStart(uri.ToString());
        }

        [Event(
            13,
            Message = "Start getting HLTB game page from {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.GetHltbGamePage,
            Opcode = EventOpcode.Start)]
        private void GetHltbGamePageStart(string uri)
        {
            WriteEvent(13, uri);
        }

        [NonEvent]
        public void GetHltbGamePageStop(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            GetHltbGamePageStop(uri.ToString());
        }

        [Event(
            14,
            Message = "Finished getting HLTB game page from {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.GetHltbGamePage,
            Opcode = EventOpcode.Stop)]
        private void GetHltbGamePageStop(string uri)
        {
            WriteEvent(14, uri);
        }

        [Event(
            140,
            Message = "Empty game name parsed for HLTB ID {0} - retrying attempt #{1} / {2} will take place in {3} seconds",
            Keywords=Keywords.Scraping,
            Level=EventLevel.Warning)]
        public void EmptyGameNameParsed(int hltbId, int retryCount, int totalRetries, int delaySeconds)
        {
            WriteEvent(140, hltbId, retryCount, totalRetries, delaySeconds);
        }

        [NonEvent]
        public void ErrorScrapingHltbName(int current, int steamAppId, string steamName, int hltbId, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (!IsEnabled())
            {
                return;
            }

            ErrorScrapingHltbName(current, steamAppId, steamName, hltbId, exception.ToString());
        }

        [Event(
            15,
            Message = "Scraping #{0} - Error resolving HLTB name for app {1} / {2} / HLTB {3}: {4}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbName(int current, int steamAppId, string steamName, int hltbId, string exception)
        {
            WriteEvent(15, current, steamAppId, steamName, hltbId, exception);
        }

        [Event(
            16,
            Message = "Start scraping HLTB info for HLTB ID {0}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbInfo,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbInfoStart(int hltbId)
        {
            WriteEvent(16, hltbId);
        }

        [Event(
            17,
            Message = "Finished scraping HLTB info for hltb {0}: Main {1} Extras {2} Completionist {3}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbInfo,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbInfoStop(int hltbId, int mainTtb, int extrasTtb, int completionistTtb)
        {
            WriteEvent(17, hltbId, mainTtb, extrasTtb, completionistTtb);
        }

        [Event(
            117,
            Message = "{0} ({1}) - previously recorded {2} TTB of {3} is no longer present in HLTB for matched {4} ({5}). Using previous value...",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Error)]
        public void PreviouslyRecordedTtbNotOnHltb(string steamName, int steamId, string ttbType, int ttbValue, string hltbName, int hltbId)
        {
            WriteEvent(117, steamName, steamId, ttbType, ttbValue, hltbName, hltbId);
        }

        [NonEvent]
        public void GetGameOverviewPageStart(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            GetGameOverviewPageStart(uri.ToString());
        }

        [Event(
            18,
            Message = "Start getting game overview URL from {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.GetGameOverviewPage,
            Opcode = EventOpcode.Start)]
        private void GetGameOverviewPageStart(string uri)
        {
            WriteEvent(18, uri);
        }

        [NonEvent]
        public void GetGameOverviewPageStop(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            GetGameOverviewPageStop(uri.ToString());
        }

        [Event(
            19,
            Message = "Finished getting game overview URL from {0}",
            Keywords = Keywords.Http,
            Level = EventLevel.Informational,
            Task = Tasks.GetGameOverviewPage,
            Opcode = EventOpcode.Stop)]
        private void GetGameOverviewPageStop(string uri)
        {
            WriteEvent(19, uri);
        }

        [NonEvent]
        public void ErrorScrapingHltbInfo(int current, int steamAppId, string steamName, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (!IsEnabled())
            {
                return;
            }

            ErrorScrapingHltbInfo(current, steamAppId, steamName, exception.ToString());
        }

        [Event(
            20,
            Message = "Scraping #{0} - Error resolving HLTB info for app {1} / {2}: {3}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbInfo(int current, int steamAppId, string steamName, string exception)
        {
            WriteEvent(20, current, steamAppId, steamName, exception);
        }

        [Event(
            21,
            Message = "Start calculating imputed values ({0} not completely missing games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.CalculateImputation,
            Opcode = EventOpcode.Start)]
        public void CalculateImputationStart(int notCompletelyMissingCount)
        {
            WriteEvent(21, notCompletelyMissingCount);   
        }

        [Event(
            22,
            Message = "Finished calculating imputed values  ({0} not completely missing games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.CalculateImputation,
            Opcode = EventOpcode.Stop)]
        public void CalculateImputationStop(int notCompletelyMissingCount)
        {
            WriteEvent(22, notCompletelyMissingCount);
        }

        [Event(
            23,
            Message = "Start submitting imputation job",
            Keywords = Keywords.Imputation | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.SubmitImputationJob,
            Opcode = EventOpcode.Start)]
        public void SubmitImputationJobStart()
        {
            WriteEvent(23);
        }

        [Event(
            24,
            Message = "Finished submitting imputation job, ID: {0}",
            Keywords = Keywords.Imputation | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.SubmitImputationJob,
            Opcode = EventOpcode.Stop)]
        public void SubmitImputationJobStop(string jobId)
        {
            WriteEvent(24, jobId);
        }

        [Event(
            25,
            Message = "Start imputing",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.Impute,
            Opcode = EventOpcode.Start)]
        public void ImputeStart()
        {
            WriteEvent(25);
        }

        [Event(
            26,
            Message = "Finished imputing",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.Impute,
            Opcode = EventOpcode.Stop)]
        public void ImputeStop()
        {
            WriteEvent(26);
        }

        [Event(
            125,
            Message = "Start imputing genre {0} ({1} games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.ImputeGenre,
            Opcode = EventOpcode.Start)]
        public void ImputeGenreStart(string genre, int count)
        {
            WriteEvent(125, genre, count);
        }

        [Event(
            126,
            Message = "Finished imputing genre {0} ({1} games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.ImputeGenre,
            Opcode = EventOpcode.Stop)]
        public void ImputeGenreStop(string genre, int count)
        {
            WriteEvent(126, genre, count);
        }

        [Event(
            27,
            Message = "Imputation miss in game {0} ({1}): {2}/{3}/{4} (imputed: {8}/{9}/{10}) Substituted with: {5}/{6}/{7}",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Warning)]
        public void ImputationMiss(
            string steamName, int steamId, int originalImputedMain, int originalImputedExtras, int originalImputedCompletionist, 
            int main, int extras, int completionist, bool mainImputed, bool extrasImputed, bool completionistImputed)
        {
            WriteEvent(27, steamName, steamId, originalImputedMain, originalImputedExtras, originalImputedCompletionist, 
                main, extras, completionist, mainImputed, extrasImputed, completionistImputed);
        }

        [Event(
            127,
            Message = "Error imputing genre {0} - falling back to unified imputation values",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Warning)]
        public void ImputationError(string genre)
        {
            WriteEvent(127, genre);
        }

        [Event(
            227,
            Message = "Imputation overrode original value. Game: {0}/{1}. Original {2}: {3}. Imputed {2}: {4}",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Error)]
        public void ImputationOverrodeOriginalValue(int steamId, string steamName, string ttbType, int original, int overriden)
        {
            WriteEvent(227, steamId, steamName, ttbType, original, overriden);
        }

        [Event(
            327,
            Message = "Genre {0} has no TTBs",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Error)]
        public void GenreHasNoTtbs(string genre)
        {
            WriteEvent(327, genre);
        }

        [Event(
            427,
            Message = "Imputation produced zero TTB for game {0} ({1}) : {2}/{3}/{4} ({5}/{6}/{7})",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Error)]
        public void ImputationProducedZeroTtb(string steamName, int steamId, 
            int mainTtb, int extrasTtb, int completionistTtb, bool mainImputed, bool extrasImputed, bool completionistImputed)
        {
            WriteEvent(427, steamName, steamId, mainTtb, extrasTtb, completionistTtb, mainImputed, extrasImputed, completionistImputed);
        }

        [Event(
            28,
            Message = "Start uploading TTB input to blob {0}",
            Keywords = Keywords.Imputation | Keywords.BlobStorage | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.UploadTtbToBlob,
            Opcode = EventOpcode.Start)]
        public void UploadTtbToBlobStart(string blobName)
        {
            WriteEvent(28, blobName);
        }

        [Event(
            29,
            Message = "Finished uploading TTB input to blob {0}",
            Keywords = Keywords.Imputation | Keywords.BlobStorage | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.UploadTtbToBlob,
            Opcode = EventOpcode.Stop)]
        public void UploadTtbToBlobStop(string blobName)
        {
            WriteEvent(29, blobName);
        }

        [Event(
            30,
            Message = "Start polling imputation job status",
            Keywords = Keywords.Imputation | Keywords.Http | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.PollImputationJobStatus,
            Opcode = EventOpcode.Start)]
        public void PollImputationJobStatusStart()
        {
            WriteEvent(30);
        }

        [Event(
            31,
            Message = "Finished polling imputation job status",
            Keywords = Keywords.Imputation | Keywords.Http | Keywords.AzureML,
            Level = EventLevel.Informational,
            Task = Tasks.PollImputationJobStatus,
            Opcode = EventOpcode.Stop)]
        public void PollImputationJobStatusStop()
        {
            WriteEvent(31);
        }

        [Event(
            32,
            Message = "Imputation job status is: {0}",
            Keywords = Keywords.Imputation | Keywords.AzureML,
            Level = EventLevel.Informational)]
        public void ExpectedPollingStatusRetrieved(string status)
        {
            WriteEvent(32, status);
        }

        [Event(
            33,
            Message = "Imputation job status is: {0} ({1})",
            Keywords = Keywords.Imputation | Keywords.AzureML,
            Level = EventLevel.Error)]
        public void UnexpectedPollingStatusRetrieved(string status, string details)
        {
            WriteEvent(33, status, details);
        }

        //[Event(
        //    34,
        //    Message = "Setting completely missing app {0} ({1}) to average: {2}/{3}/{4}",
        //    Keywords = Keywords.Imputation,
        //    Level = EventLevel.Verbose)]
        //public void SettingCompletelyMissingApp(string steamName, int steamAppId, int mainAverage, int extrasAverage, int completionistAverage)
        //{
        //    WriteEvent(34, steamName, steamAppId, mainAverage, extrasAverage, completionistAverage);
        //}
    }
}
