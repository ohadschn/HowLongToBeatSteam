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
        public void TestPortal()
        {
            TestScraping(400, "Portal", "Portal");
        }

        [TestMethod]
        public void TestLegoStarWarsTheCompleteSaga()
        {
            TestScraping(32440, "LEGO Star Wars: The Complete Saga", "Lego Star Wars: The Complete Saga");
        }

        private void TestScraping(int steamId, string steamName, string hltbName)
        {
            var app = new AppEntity(steamId, steamName, AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { app }).Wait();
            Assert.AreEqual(hltbName, app.HltbName, "Incorrect HLTB game name");
            Assert.IsTrue(app.MainTtb > 0, "Expected positive main TTB");
            Assert.IsTrue(app.ExtrasTtb > 0, "Expected positive extras TTB");
            Assert.IsTrue(app.CompletionistTtb > 0, "Expected positive completionist TTB");

            Assert.IsFalse(app.MainTtbImputed, "Expected non-imputed main TTB");
            Assert.IsFalse(app.ExtrasTtbImputed, "Expected non-imputed extras TTB");
            Assert.IsFalse(app.CompletionistTtbImputed, "Expected non-imputed completionist TTB");
        }
    }
}
