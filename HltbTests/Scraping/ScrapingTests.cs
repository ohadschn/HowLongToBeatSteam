using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Scraper;
using Common.Entities;

namespace HltbTests.Scraping
{
    [TestClass]
    public class ScrapingTests
    {
        [TestMethod]
        public void TestPortalScraping()
        {
            var portalApp = new AppEntity(400, "Portal", AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { portalApp }).Wait();
            Assert.AreEqual("Portal", portalApp.HltbName, "Incorrect HLTB game name");
        }
    }
}
