using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Scraper;
using Common.Entities;

namespace HltbTests.Scraping
{
    [TestClass]
    public class ScrapingTests
    {
        [TestMethod]
        public void TestFullStats()
        {
            TestScraping("Portal", true, true, true);
        }

        [TestMethod]
        public void TestMultiplayerOnly()
        {
            TestScraping("Spiral Knights", false, false, false);
        }

        [TestMethod]
        public void TestEndlessTitle()
        {
            TestScraping("World of Guns: Gun Disassembly", false, false, false);
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStat()
        {
            TestScraping("A Bird Story", true, false, false);
            TestScraping("The Secret of Hildegards", true, false, false);
            TestScraping("Cognition: An Erica Reed Thriller", true, false, false);
            TestScraping("Gearcrack Arena", true, false, false);
            TestScraping("The Plan (2013)", true, false, false);
            TestScraping("Crystals of Time", true, true, false);
            TestScraping("The Wolf Among Us", true, true, false);
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStatRange()
        {
            TestScraping("The Walking Dead: Season 2", true, true, false);
        }

        private void TestScraping(string name, bool hasMain, bool hasExtras, bool hasCompletionist)
        {
            var app = GetApp(name);
            if (hasMain)
            {
                Assert.IsTrue(app.MainTtb > 0, "expected positive main TTB");
                Assert.IsFalse(app.MainTtbImputed, "expected non-imputed main TTB");
            }
            else
            {
                Assert.AreEqual(0, app.MainTtb, "expected zero main TTB");
                Assert.IsTrue(app.MainTtbImputed, "expected imputed main TTB");
            }

            if (hasExtras)
            {
                Assert.IsTrue(app.ExtrasTtb > 0, "expected positive extras TTB");
                Assert.IsFalse(app.ExtrasTtbImputed, "expected non-imputed extras TTB");
            }
            else
            {
                Assert.AreEqual(0, app.ExtrasTtb, "expected zero extras TTB");
                Assert.IsTrue(app.ExtrasTtbImputed, "expected imputed extras TTB");
            }

            if (hasCompletionist)
            {
                Assert.IsTrue(app.CompletionistTtb > 0, "expected positive completionist TTB");
                Assert.IsFalse(app.CompletionistTtbImputed, "expected non-imputed completionist TTB");
            }
            else
            {
                Assert.AreEqual(0, app.CompletionistTtb, "expected zero completionist TTB");
                Assert.IsTrue(app.CompletionistTtbImputed, "expected imputed completionist TTB");
            }
        }

        private static AppEntity GetApp(string name)
        {
            var app = new AppEntity(0, name, AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { app }).Wait();
            Assert.AreEqual(name, app.HltbName, "Incorrect HLTB game name");
            return app;
        }
    }
}
