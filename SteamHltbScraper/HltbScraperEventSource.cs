using System;
using System.Diagnostics.Tracing;

namespace SteamHltbScraper
{
    public interface IHltbScraperEventSource
    {
        void ScrapeHltbStart();
        void ScrapeHltbStop();
        void ScrapeGameStart(int steamAppId, int current);
        void ScrapeGameStop(int steamAppId, int current);
        void ScrapeHltbIdStart(string appName);
        void ScrapeHltbIdStop(string appName, int hltbId);
        void PostHltbSearchStart(Uri uri, string content);
        void PostHltbSearchStop(Uri uri, string content);
        void GameNotFoundInSearch(string game);
        void ErrorScrapingHltbId(int current, int steamAppId, string steamName, Exception exception);
        void ScrapeHltbNameStart(int hltbId);
        void ScrapeHltbNameStop(int hltbId, string hltbName);
        void GetHltbGamePageStart(Uri uri);
        void GetHltbGamePageStop(Uri uri);
        void ErrorScrapingHltbName(int current, int steamAppId, string steamName, int hltbId, Exception exception);
        void ScrapeHltbInfoStart(int hltbId);
        void ScrapeHltbInfoStop(int hltbId, int mainTtb, int extrasTtb, int completionistTtb, int combinedTtb, int solo, int coOp, int vs);
        void GetGameOverviewPageStart(Uri uri);
        void GetGameOverviewPageStop(Uri uri);
        void ErrorScrapingHltbInfo(int current, int steamAppId, string steamName, Exception exception);
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-Scraper")]
    public class HltbScraperEventSource : EventSource, IHltbScraperEventSource
    {
        public static readonly IHltbScraperEventSource Log = new HltbScraperEventSource();

        private HltbScraperEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords HltbScraper = (EventKeywords) 1;
            public const EventKeywords Http = (EventKeywords) 2;
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
        }
// ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start scraping HLTB",
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltb,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Start scraping HLTB",
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
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
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbId(int current, int steamAppId, string steamName, string exception)
        {
            WriteEvent(10, current, steamAppId, steamName, exception);
        }

        [Event(
            11,
            Message = "Start scraping HLTB name for id {0}",
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbName,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbNameStart(int hltbId)
        {
            WriteEvent(11, hltbId);
        }

        [NonEvent]
        public void ScrapeHltbNameStop(int hltbId, string hltbName)
        {
            ScrapeHltbNameStop(hltbName, hltbId);
        }

        [Event(
            12,
            Message = "Finished scraping HLTB name for id {0}: {1}",
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbName,
            Opcode = EventOpcode.Stop)]
        private void ScrapeHltbNameStop(string hltbName, int hltbId)
        {
            WriteEvent(12, hltbName, hltbId);
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
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbName(int current, int steamAppId, string steamName, int hltbId, string exception)
        {
            WriteEvent(15, current, steamAppId, steamName, hltbId, exception);
        }

        [Event(
            16,
            Message = "Start scraping HLTB info for HLTB ID {0}",
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbInfo,
            Opcode = EventOpcode.Start)]
        public void ScrapeHltbInfoStart(int hltbId)
        {
            WriteEvent(16, hltbId);
        }

        [Event(
            17,
            Message = "Finished scraping HLTB info for hltb {0}: Main {1} Extras {2} Completionist {3} Combined {4} Solo {5} Co-Op {6} Vs. {7}",
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Informational,
            Task = Tasks.ScrapeHltbInfo,
            Opcode = EventOpcode.Stop)]
        public void ScrapeHltbInfoStop(int hltbId, int mainTtb, int extrasTtb, int completionistTtb, int combinedTtb, int solo, int coOp, int vs)
        {
            WriteEvent(17, hltbId, mainTtb, extrasTtb, completionistTtb, combinedTtb, solo, coOp, vs);
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
            Keywords = Keywords.HltbScraper,
            Level = EventLevel.Error)]
        private void ErrorScrapingHltbInfo(int current, int steamAppId, string steamName, string exception)
        {
            WriteEvent(20, current, steamAppId, steamName, exception);
        }
    }
}
