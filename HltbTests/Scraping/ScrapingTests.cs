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
            TestScrapingSinglePlayer(400, "Portal", "Portal");
        }

        [TestMethod]
        public void TestLegoStarWarsTheCompleteSaga()
        {
            TestScrapingSinglePlayer(32440, "LEGO Star Wars: The Complete Saga", "Lego Star Wars: The Complete Saga");
        }

        [TestMethod]
        public void TestSpiralKnights()
        {
            TestScrapingMultiPlayer(99900, "Spiral Knights", "Spiral Knights");
        }

        private void TestScrapingSinglePlayer(int steamId, string steamName, string hltbName)
        {
            var app = GetApp(steamId, steamName, hltbName);
            Assert.IsTrue(app.MainTtb > 0, "non-positive main TTB for single player game with stats");
            Assert.IsTrue(app.ExtrasTtb > 0, "non-positive extras TTB for single player game with stats");
            Assert.IsTrue(app.CompletionistTtb > 0, "non-positive completionist TTB for single player game with stats");

            Assert.IsFalse(app.MainTtbImputed, "imputed main TTB for single player game with stats");
            Assert.IsFalse(app.ExtrasTtbImputed, "imputed extras TTB for single player game with stats");
            Assert.IsFalse(app.CompletionistTtbImputed, "imputed completionist TTB for single player game with stats");
        }

        private void TestScrapingMultiPlayer(int steamId, string steamName, string hltbName)
        {
            var app = GetApp(steamId, steamName, hltbName);
            Assert.IsTrue(app.MainTtb == 0, "non-zero main TTB for multiplayer game");
            Assert.IsTrue(app.ExtrasTtb == 0, "non-zero extras TTB for multiplayer game");
            Assert.IsTrue(app.CompletionistTtb == 0, "non-zero completionist TTB for multiplayer game");

            Assert.IsTrue(app.MainTtbImputed, "non-imputed main TTB for multiplayer game");
            Assert.IsTrue(app.ExtrasTtbImputed, "non-imputed extras TTB for multiplayer game");
            Assert.IsTrue(app.CompletionistTtbImputed, "non-imputed completionist TTB for multiplayer game");
        }

        private static AppEntity GetApp(int steamId, string steamName, string hltbName)
        {
            var app = new AppEntity(steamId, steamName, AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { app }).Wait();
            Assert.AreEqual(hltbName, app.HltbName, "Incorrect HLTB game name");
            return app;
        }
    }
}
