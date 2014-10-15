using Common;
using HowLongToBeatSteam;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MissingGamesUpdater;
using SteamHltbScraper;
using UnknownUpdater;

namespace HltbTests
{
    [TestClass]
    public class Tests
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
    }
}
