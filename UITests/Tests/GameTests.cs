﻿using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class GameTests
    {
        private static int GetExcludedGameCount(IWebDriver driver)
        {
            Console.WriteLine("Locating excluded game count span...");
            var findElement = driver.FindElement(By.Id(SiteConstants.ExcludedGameCountSpanId));

            Console.WriteLine("Parsing excluded game count...");
            return findElement.Displayed ? Int32.Parse(findElement.Text, NumberStyles.Number) : 0;
        }

        private static void AssertValidPlaytimes(IWebDriver driver)
        {
            Console.WriteLine("Locating current playtime text...");
            var currentPlaytimeSpan = driver.FindElement(By.Id(SiteConstants.CurrentPlaytimeSpanId));
            Assert.AreEqual("0 hours", currentPlaytimeSpan.Text);

            Console.WriteLine("Parsing remaining playtimes...");
            var mainRemainingPlaytime = SiteHelper.GetRemainingMainPlaytime(driver);
            var extrasRemainingPlaytime = SiteHelper.GetRemainingExtrasPlaytime(driver);
            var completionistRemainingPlaytime = SiteHelper.GetRemainingCompletionistPlaytime(driver);

            Assert.IsTrue(completionistRemainingPlaytime > extrasRemainingPlaytime, "completionist playtime does not exceed extras playtime");
            Assert.IsTrue(extrasRemainingPlaytime > mainRemainingPlaytime, "extras playtime does not exceed completionist playtime");
        }

        private static void AssertValidPercentages(IWebDriver driver)
        {
            Console.WriteLine("Locating and asserting playtime percentages...");

            var mainRemainingPlaytimePercent = driver.FindElement(By.Id(SiteConstants.MainPlaytimeRemainingPercentSpan)).Text;
            Assert.AreEqual("100.00%", mainRemainingPlaytimePercent, "expected 100% main playtime to remain");

            var extrasRemainingPlaytimePercent = driver.FindElement(By.Id(SiteConstants.MainPlaytimeRemainingPercentSpan)).Text;
            Assert.AreEqual("100.00%", extrasRemainingPlaytimePercent, "expected 100% extras playtime to remain");

            var completionistRemainingPlaytimePercent = driver.FindElement(By.Id(SiteConstants.CompletionistPlaytimeRemainingPercentSpan)).Text;
            Assert.AreEqual("100.00%", completionistRemainingPlaytimePercent, "expected 100% completionist playtime to remain");
        }

        [TestMethod]
        public void TestGameSummary()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser, WaitType.PageLoad);

                Assert.AreEqual(UserConstants.HltbUserGameCount, SiteHelper.GetGameCount(driver), "incorrect game count");
                Assert.AreEqual(UserConstants.HltbUserExludedGameCount, GetExcludedGameCount(driver), "incorrect excluded game count");

                AssertValidPlaytimes(driver);
                AssertValidPercentages(driver);
            });
        }

        [TestMethod]
        public void TestCachedGameSummary()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.GoToCachedGamesPage(driver);

                var gameCount = SiteHelper.GetGameCount(driver);
                Assert.IsTrue(gameCount > 10000, $"too few games in cache: {gameCount}");
                Assert.AreEqual(0, GetExcludedGameCount(driver), "incorrect excluded game count");

                AssertValidPlaytimes(driver);
                AssertValidPercentages(driver);
            });
        }
    }
}