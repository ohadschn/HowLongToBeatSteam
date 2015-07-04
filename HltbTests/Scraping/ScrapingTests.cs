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
            TestScraping("Portal", 2007, true, true, true);
        }

        [TestMethod]
        public void TestMultiplayerOnly()
        {
            TestScraping("Spiral Knights", 2009, false, false, false);
        }

        [TestMethod]
        public void TestEndlessTitle()
        {
            TestScraping("World of Guns: Gun Disassembly", 2014, false, false, false);
        }

        [TestMethod]
        public void TestNonAlphanumericName()
        {
            TestScraping("Air Conflicts - Secret Wars", 2011, true, true, false, "Air Conflicts: Secret Wars");
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStat()
        {
            TestScraping("Commander Keen Collection (Ep. 1-5)", 1990, true, true, false);
            TestScraping("A Bird Story", 2014, true, false, false);
            TestScraping("The Secret of Hildegards", 2011, true, false, false);
            TestScraping("Cognition: An Erica Reed Thriller", 2013, true, false, false);
            TestScraping("Gearcrack Arena", 2014, true, true, false);
            TestScraping("The Plan (2013)", 2013, true, false, false);
            TestScraping("Crystals of Time", 2014, true, true, false);
            TestScraping("The Wolf Among Us", 2013, true, true, false);
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStatRange()
        {
            TestScraping("The Walking Dead: Season 2", 2013, true, true, false);
        }

        [TestMethod]
        public void TestGameNotFoundInSearch()
        {
            var app = ScrapeApp("{3F883F0C-A91E-4B99-A4E7-F4AA873AA3FF}"); //just a random GUID that won't be found
            Assert.AreEqual(-1, app.HltbId);
        }

        private static void TestScraping(string name, int releaseYear, bool hasMain, bool hasExtras, bool hasCompletionist, string hltbName = null)
        {
            var app = ScrapeApp(name);
            Assert.AreEqual(hltbName ?? name, app.HltbName, "Incorrect HLTB game name");
            Assert.AreEqual(releaseYear, app.ReleaseDate.Year, "Incorrect release year");
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

        private static AppEntity ScrapeApp(string name)
        {
            var app = new AppEntity(0, name, AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { app }).Wait();
            return app;
        }
    }
}
