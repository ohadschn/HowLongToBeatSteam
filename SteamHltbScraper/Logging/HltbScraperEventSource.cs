﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace SteamHltbScraper.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-Scraper")]
    public sealed class HltbScraperEventSource : EventSourceBase
    {
        public static readonly HltbScraperEventSource Log = new HltbScraperEventSource();

        private HltbScraperEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords Scraping = (EventKeywords) 1;
            public const EventKeywords Http = (EventKeywords)2;
            public const EventKeywords Imputation = (EventKeywords)4;
            public const EventKeywords AzureMl = (EventKeywords)8;
            public const EventKeywords BlobStorage = (EventKeywords)16;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
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
            public const EventTask CalculateImputation = (EventTask) 9;
            public const EventTask SubmitImputationJob = (EventTask) 10;
            public const EventTask Impute = (EventTask) 11;
            public const EventTask UploadTtbToBlob = (EventTask) 12;
            public const EventTask PollImputationJobStatus = (EventTask) 13;
            public const EventTask ImputeGenre = (EventTask) 14;
            public const EventTask UpdateGenreStats = (EventTask) 15;
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
            Message = "Start scraping Steam ID {0} (#{1} / {2})",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeGame,
            Opcode = EventOpcode.Start)]
        public void ScrapeGameStart(int steamAppId, int current, int total)
        {
            WriteEvent(3, steamAppId, current, total);
        }

        [Event(
            4,
            Message = "Finished scraping Steam ID {0} (#{1} / {2})",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeGame,
            Opcode = EventOpcode.Stop)]
        public void ScrapeGameStop(int steamAppId, int current, int total)
        {
            WriteEvent(4, steamAppId, current, total);
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
                throw new ArgumentNullException(nameof(uri));
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
                throw new ArgumentNullException(nameof(uri));
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

        [Event(
            90,
            Message = "Could not find exact game name '{0}' - trying letter/number stripped version: '{1}'",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational)]
        public void SearchingForLetterNumberStrippedName(string original, string letterNumberStrippedName)
        {
            WriteEvent(90, original, letterNumberStrippedName);
        }

        [Event(
            91,
            Message = "Games found in HLTB search for '{0}' : {1}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational)]
        public void GamesFoundInSearch(string game, int resultsFound)
        {
            WriteEvent(91, game, resultsFound);
        }

        [Event(
            92,
            Message = "Too many games found in HLTB search for '{0}' - result count of {1} exceeds threshold of {2}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Warning)]
        public void ResultCountExceedsConfidenceThreshold(string game, int resultsFound, int threshold)
        {
            WriteEvent(92, game, resultsFound, threshold);
        }

        [NonEvent]
        public void ErrorScrapingHltbId(int current, int steamAppId, string steamName, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
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
                throw new ArgumentNullException(nameof(uri));
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
                throw new ArgumentNullException(nameof(uri));
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
            Message = "Encountered transient HLTB fault while parsing HLTB ID {0} ({1}) - retrying attempt #{2} / {3} will take place in {4} seconds",
            Keywords=Keywords.Scraping,
            Level=EventLevel.Warning)]
        public void TransientHltbFault(int hltbId, string error, int retryCount, int totalRetries, int delaySeconds)
        {
            WriteEvent(140, hltbId, error, retryCount, totalRetries, delaySeconds);
        }

        [NonEvent]
        public void ErrorScrapingHltbName(int current, int steamAppId, string steamName, int hltbId, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
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
            Level = EventLevel.Warning)]
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
            Message = "Finished scraping HLTB info for hltb {0}: Main {1} Extras {2} Completionist {3} Release Year {4}",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbInfo,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbInfoStop(int hltbId, int mainTtb, int extrasTtb, int completionistTtb, int releaseYear)
        {
            WriteEvent(17, hltbId, mainTtb, extrasTtb, completionistTtb, releaseYear);
        }

        [Event(
            117,
            Message = "{0} ({1}) - previously recorded {2} TTB of {3} is no longer present in HLTB for matched {4} ({5}). Using previous value...",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Warning)]
        public void PreviouslyRecordedTtbNotOnHltb(string steamName, int steamId, string ttbType, int ttbValue, string hltbName, int hltbId)
        {
            WriteEvent(117, steamName, steamId, ttbType, ttbValue, hltbName, hltbId);
        }

        [NonEvent]
        public void ErrorScrapingHltbInfo(int current, int steamAppId, string steamName, Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
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
            Message = "Start calculating imputed values for genre {0} ({1} not completely missing games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.CalculateImputation,
            Opcode = EventOpcode.Start)]
        public void CalculateImputationStart(string genre, int notCompletelyMissingCount)
        {
            WriteEvent(21, genre, notCompletelyMissingCount);   
        }

        [Event(
            22,
            Message = "Finished calculating imputed values for genre {0} ({1} not completely missing games)",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.CalculateImputation,
            Opcode = EventOpcode.Stop)]
        public void CalculateImputationStop(string genre, int notCompletelyMissingCount)
        {
            WriteEvent(22, genre, notCompletelyMissingCount);
        }

        [Event(
            23,
            Message = "Start submitting imputation job",
            Keywords = Keywords.Imputation | Keywords.AzureMl,
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
            Keywords = Keywords.Imputation | Keywords.AzureMl,
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
            270,
            Message = "Imputation produced too many TTBs misses for genre {0}: {1}/{2}",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Error)]
        public void ImputationProducedTooManyMisses(string genre, int misses, int genreCount)
        {
            WriteEvent(270, genre, misses, genreCount);
        }

        [Event(
            127,
            Message = "Error imputing genre {0} - falling back to unified imputation values: {1}",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Warning)]
        public void ImputationError(string genre, string message)
        {
            WriteEvent(127, genre, message);
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
            Level = EventLevel.Warning)]
        public void ImputationProducedZeroTtb(string steamName, int steamId, 
            int mainTtb, int extrasTtb, int completionistTtb, bool mainImputed, bool extrasImputed, bool completionistImputed)
        {
            WriteEvent(427, steamName, steamId, mainTtb, extrasTtb, completionistTtb, mainImputed, extrasImputed, completionistImputed);
        }

        [Event(
            428,
            Message = "Imputation produced too many zero TTBs for genre {0}: {1}/{2}",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Error)]
        public void ImputationProducedTooManyZeroTtbs(string genre, int zeros, int genreCount)
        {
            WriteEvent(428, genre, zeros, genreCount);
        }

        [Event(
            28,
            Message = "Start uploading TTB input to blob {0}",
            Keywords = Keywords.Imputation | Keywords.BlobStorage | Keywords.AzureMl,
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
            Keywords = Keywords.Imputation | Keywords.BlobStorage | Keywords.AzureMl,
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
            Keywords = Keywords.Imputation | Keywords.Http | Keywords.AzureMl,
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
            Keywords = Keywords.Imputation | Keywords.Http | Keywords.AzureMl,
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
            Keywords = Keywords.Imputation | Keywords.AzureMl,
            Level = EventLevel.Informational)]
        public void ExpectedPollingStatusRetrieved(string status)
        {
            WriteEvent(32, status);
        }

        [Event(
            33,
            Message = "Imputation job status is: {0} ({1})",
            Keywords = Keywords.Imputation | Keywords.AzureMl,
            Level = EventLevel.Error)]
        public void UnexpectedPollingStatusRetrieved(string status, string details)
        {
            WriteEvent(33, status, details);
        }

        [Event(
            35,
            Message = "Start updating genre stats (genre count: {0})",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateGenreStats,
            Opcode = EventOpcode.Start)]
        public void UpdateGenreStatsStart(int genreCount)
        {
            WriteEvent(35, genreCount);
        }

        [Event(
            36,
            Message = "Finished updating genre stats (genre count: {0})",
            Keywords = Keywords.Imputation,
            Level = EventLevel.Informational,
            Task = Tasks.UpdateGenreStats,
            Opcode = EventOpcode.Stop)]
        public void UpdateGenreStatsStop(int genreCount)
        {
            WriteEvent(36, genreCount);
        }

        [Event(
            37,
            Message = "Game {0} (HLTB ID {1}) contains invalid TTBs: {2}/{3}/{4} (imputed: {5}/{6}/{7}) Substituted with: {8}/{9}/{10}",
            Keywords = Keywords.Imputation | Keywords.Scraping,
            Level = EventLevel.Warning)]
        public void InvalidTtbsScraped(string name, int hltbId, int main, int extras, int completionist,
            bool mainImputed, bool extrasImputed, bool completionistImputed, int newMain, int newExtras, int newCompletionist)
        {
            WriteEvent(37, name, hltbId, main, extras, completionist, mainImputed, extrasImputed, completionistImputed, newMain, newExtras, newCompletionist);
        }

        [Event(
            38,
            Message = "Too many games contain invalid TTBs: {0} out of {1}",
            Keywords = Keywords.Imputation | Keywords.Scraping,
            Level = EventLevel.Error)]
        public void TooManyInvalidTtbsScraped(int invalidCount, int totalCount)
        {
            WriteEvent(38, invalidCount, totalCount);
        }

        [Event(
            39,
            Message = "Game {0} (HLTB ID {1}) flagged as endless - generating exclusion suggestion",
            Keywords = Keywords.Scraping,
            Level = EventLevel.Informational)]
        public void GameFlaggedAsEndless(string name, int hltbId)
        {
            WriteEvent(39, name, hltbId);
        }
    }
}
