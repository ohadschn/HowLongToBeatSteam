﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Scraper;
using static System.FormattableString;

namespace HltbTests.Scraping
{
    [TestClass]
    public class ScrapingTests
    {
        [ClassCleanup]
        [SuppressMessage("Sonar.Bug", "S3168:AsyncMethodsShouldNotReturnVoid", Justification = "MSTest Cleanup method")]
        public static async void Cleanup()
        {
            await Task.Delay(2000); //give Azure Storage time to complete deletion of the suggestions
            await DrainAllSuggestionsForSteamApp();
            EventSourceRegistrar.DisposeEventListeners();
        }

        [TestMethod]
        public void TestNoStats()
        {
            TestScraping("Music Inside: A VR Rhythm Game", 2016, false, false, false);
        }

        [TestMethod]
        public void TestGameLiteralInName()
        {
            TestScraping("Jurassic Park: The Game", 2011, true, true, true, "Jurassic Park: The Game");
        }

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
        public void TestAtSymbolInTitle()
        {
            var app = ScrapeApp("Tales of Berseria™ - Idolm@ster Costumes Set");
            AppAssertAreEqual(app, -1, app.HltbId, "expected no HLTB ID due to no search results");
        }

        [TestMethod]
        public async Task TestEndlessTitle()
        {
            //delete existing suggestions for game - the Steam app ID will be 0
            await DrainAllSuggestionsForSteamApp().ConfigureAwait(false);

            TestScraping("World of Guns: Gun Disassembly", 2014, false, false, false);
            TestScraping("Gearcrack Arena", 2014, false, false, false);

            //verify a new endless suggestion was auto-generated for it
            var suggestions = await DrainAllSuggestionsForSteamApp().ConfigureAwait(false);
            Assert.AreEqual(2, suggestions.Count, "expected automatic suggestion for endless title");
            Assert.IsFalse(new [] {20228, 18966}.Except(suggestions.Select(s => s.HltbId)).Any(), "incorrect HLTB ID in automatic endless title suggestions");
            Assert.IsTrue(suggestions.All(s => s.SteamAppId == 0), "incorrect Steam App ID in automatic endless title suggestions");
            Assert.IsTrue(suggestions.All(s => s.AppType == AppEntity.EndlessTitleTypeName), "incorrect type in automatic endless title suggestions");
        }

        private static async Task<ConcurrentBag<SuggestionEntity>> DrainAllSuggestionsForSteamApp()
        {
            var suggestions = await StorageHelper.GetAllSuggestions(
                StorageHelper.StartsWithFilter(StorageHelper.RowKey, SuggestionEntity.SuggestionPrefix + "_0")).ConfigureAwait(false);

            foreach (var suggestion in suggestions)
            {
                Assert.AreEqual(0, suggestion.SteamAppId, "Unexpected Steam app ID for test suggestion:");
                await StorageHelper.DeleteSuggestion(suggestion).ConfigureAwait(false);
            }

            return suggestions;
        }

        [TestMethod]
        public void TestNonLetterNumberName()
        {
            TestScraping("Air Conflicts - Secret Wars", 2011, true, true, true, "Air Conflicts: Secret Wars");
        }

        [TestMethod]
        public void TestExclusiveNonLetterNumberName()
        {
            var app = ScrapeApp("+++++");
            AppAssertAreEqual(app, -1, app.HltbId, "expected no HLTB ID due to no search results");
        }

        [TestMethod]
        public void TestTooManySearchResults()
        {
            var app = ScrapeApp("O.R.B.");
            AppAssertAreEqual(app, -1, app.HltbId, "expected no HLTB ID due to too many search results");
        }

        [TestMethod]
        public void TestMetadataParsableAsReleaseYear()
        {
            //The following was developed by '773' which can be parsed as the date 773 A.D.
            TestScraping("Cherry Tree High Comedy Club", 2012, true, true, true);
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStat()
        {
            TestScraping("Commander Keen Collection (Ep. 1-5)", 1990, true, true, false);
            TestScraping("A Bird Story", 2014, true, true, false);
            TestScraping("The Secret of Hildegards", 2011, true, false, false);
            TestScraping("Cognition: An Erica Reed Thriller", 2012, true, true, false);
            TestScraping("The Plan (2013)", 2013, true, true, false);
            TestScraping("Crystals of Time", 2014, true, true, false);
            TestScraping("The Wolf Among Us", 2013, true, true, false);
        }

        [TestMethod]
        public void TestMonthYearDate()
        {
            TestScraping("Wizard of Legend", 2017, true, true, true);
        }

        [TestMethod]
        public void TestSinglePlayerUnifiedStatRange()
        {
            TestScraping("The Walking Dead: Season 2", 2013, true, true, false);
        }

        [TestMethod]
        public void TestGameNotFoundInSearch()
        {
            var app = ScrapeApp("Farming Simulator 15 - ITRunner");
            AppAssertAreEqual(app, -1, app.HltbId, "expected no HLTB ID");
        }

        private static void TestScraping(string name, int releaseYear, bool hasMain, bool hasExtras, bool hasCompletionist, string hltbName = null)
        {
            var app = ScrapeApp(name);
            AppAssertAreEqual(app, hltbName ?? name, app.HltbName, "Incorrect HLTB game name");
            AppAssertAreEqual(app, releaseYear, app.ReleaseDate.Year, "Incorrect release year");
            if (hasMain)
            {
                AppAssertIsTrue(app, app.MainTtb > 0, Invariant($"expected positive main TTB (actual: {app.MainTtb})"));
                AppAssertIsFalse(app, app.MainTtbImputed, "expected non-imputed main TTB");
            }
            else
            {
                AppAssertAreEqual(app, 0, app.MainTtb, Invariant($"expected zero main TTB (actual: {app.MainTtb})"));
                AppAssertIsTrue(app, app.MainTtbImputed, "expected imputed main TTB");
            }

            if (hasExtras)
            {
                AppAssertIsTrue(app, app.ExtrasTtb > 0, Invariant($"expected positive extras TTB (actual: {app.ExtrasTtb})"));
                AppAssertIsFalse(app, app.ExtrasTtbImputed, "expected non-imputed extras TTB");
            }
            else
            {
                AppAssertAreEqual(app, 0, app.ExtrasTtb, Invariant($"expected zero extras TTB (actual: {app.ExtrasTtb})"));
                AppAssertIsTrue(app, app.ExtrasTtbImputed, "expected imputed extras TTB");
            }

            if (hasCompletionist)
            {
                AppAssertIsTrue(app, app.CompletionistTtb > 0, Invariant($"expected positive completionist TTB (actual: {app.CompletionistTtb})"));
                AppAssertIsFalse(app, app.CompletionistTtbImputed, "expected non-imputed completionist TTB");
            }
            else
            {
                AppAssertAreEqual(app, 0, app.CompletionistTtb, Invariant($"expected zero completionist TTB (actual: {app.CompletionistTtb})"));
                AppAssertIsTrue(app, app.CompletionistTtbImputed, "expected imputed completionist TTB");
            }
        }

        [AssertionMethod]
        private static void AppAssertIsFalse(AppEntity app, bool condition, string message)
        {
            Assert.IsFalse(condition, AddGameInformationToAssertMessage(message, app));
        }

        [AssertionMethod]
        private static void AppAssertIsTrue(AppEntity app, bool condition, string message)
        {
            Assert.IsTrue(condition, AddGameInformationToAssertMessage(message, app));
        }

        private static void AppAssertAreEqual<T>(AppEntity app, T expected, T actual, string message)
        {
            Assert.AreEqual(expected, actual, AddGameInformationToAssertMessage(message, app));
        }

        private static string AddGameInformationToAssertMessage(string message, AppEntity app)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0} for game {1} (HLTB ID {2})", message, app.HltbName, app.HltbId);
        }

        private static AppEntity ScrapeApp(string name)
        {
            var app = new AppEntity(0, name, AppEntity.GameTypeName);
            HltbScraper.ScrapeHltb(new[] { app }, (a,e) => throw new InvalidOperationException("error during scraping", e)).Wait();
            return app;
        }
    }
}
