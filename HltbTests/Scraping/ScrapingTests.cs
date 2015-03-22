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
        public void TestHltbScraper()
        {
            var portalApp = new AppEntity(400, "Portal", AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { portalApp }).Wait();
            Assert.AreEqual("Portal", portalApp.HltbName, "Incorrect HLTB game name");
            Assert.IsTrue(portalApp.MainTtb > 0, "Expected positive main TTB");
            Assert.IsTrue(portalApp.ExtrasTtb > 0, "Expected positive extras TTB");
            Assert.IsTrue(portalApp.CompletionistTtb > 0, "Expected positive completionist TTB");

            Assert.IsFalse(portalApp.MainTtbImputed, "Expected non-imputed main TTB");
            Assert.IsFalse(portalApp.ExtrasTtbImputed, "Expected non-imputed extras TTB");
            Assert.IsFalse(portalApp.CompletionistTtbImputed, "Expected non-imputed completionist TTB");
        }
    }
}
