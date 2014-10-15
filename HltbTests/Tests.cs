using System.Diagnostics.Tracing;
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
            EventSourceAnalyzer.InspectAll((EventSource)CommonEventSource.Log);
        }

        [TestMethod]
        public void TestSiteEventSource()
        {
            EventSourceAnalyzer.InspectAll((EventSource)SiteEventSource.Log);
        }

        [TestMethod]
        public void TestMissingUpdaterEventSource()
        {
            EventSourceAnalyzer.InspectAll((EventSource)MissingUpdaterEventSource.Log);
        }

        [TestMethod]
        public void TestUnknownUpdaterEventSource()
        {
            EventSourceAnalyzer.InspectAll((EventSource)UnknownUpdaterEventSource.Log);
        }

        [TestMethod]
        public void TestHltbScraperEventSource()
        {
            EventSourceAnalyzer.InspectAll((EventSource)HltbScraperEventSource.Log);
        }
    }
}
