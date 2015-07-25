using Common.Logging;
using HowLongToBeatSteam.Logging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissingGamesUpdater.Logging;
using SteamHltbScraper.Logging;
using StorageBackupUploader.Logging;
using SuggestionWatcher.Logging;
using TestRunner.Logging;
using UnknownUpdater.Logging;

namespace HltbTests.Logging
{
    [TestClass]
    public class EventSourceTests
    {
        [TestMethod]
        public void TestCommonEventSource()
        {
            EventSourceAnalyzer.InspectAll(CommonEventSource.Log);
        }

        [TestMethod]
        public void TestSiteEventSource()
        {
            EventSourceAnalyzer.InspectAll(SiteEventSource.Log);
        }

        [TestMethod]
        public void TestMissingUpdaterEventSource()
        {
            EventSourceAnalyzer.InspectAll(MissingUpdaterEventSource.Log);
        }

        [TestMethod]
        public void TestUnknownUpdaterEventSource()
        {
            EventSourceAnalyzer.InspectAll(UnknownUpdaterEventSource.Log);
        }

        [TestMethod]
        public void TestHltbScraperEventSource()
        {
            EventSourceAnalyzer.InspectAll(HltbScraperEventSource.Log);
        }

        [TestMethod]
        public void TestSuggestionWatcherEventSource()
        {
            EventSourceAnalyzer.InspectAll(SuggestionWatcherEventSource.Log);
        }

        [TestMethod]
        public void TestStorageBackupUploaderEventSource()
        {
            EventSourceAnalyzer.InspectAll(StorageBackupUploaderEventSource.Log);
        }

        [TestMethod]
        public void TestTestRunnerEventSource()
        {
            EventSourceAnalyzer.InspectAll(TestRunnerEventSource.Log);
        }
    }
}
