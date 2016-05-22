using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class TableTests
    {
        [TestMethod]
        public void TestTableEntries()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser, WaitType.PageLoad);

                var games = TableHelper.ParseGameTable(driver);
                foreach (var game in games)
                {
                    Assert.IsTrue(game.Included, $"Expected all games to be included but the following was not: {game.SteamName}");
                    Assert.AreEqual(0, game.SteamPlaytime, $"Expected zero playtime for: {game.SteamName}");

                    Assert.IsTrue(game.MainPlaytime > 0, $"Expected main playtime to be greater than zero: {game.SteamName}");
                    Assert.IsTrue(game.ExtrasPlaytime > 0, $"Expected extras playtime to be greater than zero: {game.SteamName}");
                    Assert.IsTrue(game.CompletionistPlaytime > 0, $"Expected completionist playtime to be greater than zero: {game.SteamName}");

                    Assert.IsTrue(game.MainPlaytime <= game.ExtrasPlaytime, $"Main playtime exceeds extras playtime for: {game.SteamName}");
                    Assert.IsTrue(game.ExtrasPlaytime <= game.CompletionistPlaytime, $"Extras playtime exceeds completionist playtime for: {game.SteamName}");

                    Assert.AreEqual(game.SteamName == GameConstants.RoninSteamName, game.VerifiedFinite, $"Expected verified finite for: {game.SteamName}");
                    Assert.IsFalse(game.VerifiedCorrelation, $"Unexpected verified correlation game: {game.SteamName}");
                }

                var expectedGames = new []
                {
                    new {SteamName = GameConstants.AFistfulOfGunSteamName, HltbName = GameConstants.AFistfulOfGunHltbName},
                    new {SteamName = GameConstants.GodsWillBeWatchingSteamName, HltbName = GameConstants.GodsWillBeWatchingHltbName},
                    new {SteamName = GameConstants.RoninSteamName, HltbName = GameConstants.RoninHltbName}
                };

                AssertExtentions.AssertEqualSets(expectedGames, games.Select(g => new { g.SteamName, g.HltbName }));
            });
        }

        [TestMethod]
        public void TestTableInclusion()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                var originalMain = GameSummaryHelper.GetRemainingMainPlaytime(driver);
                var originalExtras = GameSummaryHelper.GetRemainingExtrasPlaytime(driver);
                var originalCompletionist = GameSummaryHelper.GetRemainingCompletionistPlaytime(driver);

                var inclusionCheckboxes = TableHelper.FindTableBody(driver).FindElements(By.ClassName(SiteConstants.RowIncludedCheckboxClass));

                Console.WriteLine("Excluding a game...");
                inclusionCheckboxes.First().Click();
                Assert.AreEqual(2, GameSummaryHelper.GetGameCount(driver), "Expected exclusion of game to reduce game count");

                var mainPostExclusion = GameSummaryHelper.GetRemainingMainPlaytime(driver);
                var extrasPostExclusion = GameSummaryHelper.GetRemainingExtrasPlaytime(driver);
                var completionistPostExclusion = GameSummaryHelper.GetRemainingCompletionistPlaytime(driver);

                Assert.IsTrue(mainPostExclusion < originalMain, "Expected exclusion of game to reduce original remaining main playtime");
                Assert.IsTrue(extrasPostExclusion < originalExtras, "Expected exclusion of game to reduce original remaining main playtime");
                Assert.IsTrue(completionistPostExclusion < originalCompletionist, "Expected exclusion of game to reduce original remaining main playtime");

                Console.WriteLine("Excluding remaining games...");
                foreach (var inclusionCheckbox in inclusionCheckboxes.Skip(1))
                {
                    inclusionCheckbox.Click();
                }

                Assert.AreEqual(0, GameSummaryHelper.GetGameCount(driver), "Expected zero game count when all games are excluded");
                Assert.AreEqual(TimeSpan.Zero, GameSummaryHelper.GetRemainingMainPlaytime(driver), "Expected zero main remaining playtime");
                Assert.AreEqual(TimeSpan.Zero, GameSummaryHelper.GetRemainingExtrasPlaytime(driver), "Expected zero extras remaining playtime");
                Assert.AreEqual(TimeSpan.Zero, GameSummaryHelper.GetRemainingCompletionistPlaytime(driver), "Expected zero completionist remaining playtime");
            });
        }

        private static void TestColumnSort<T>(IWebDriver driver, string headerId, Func<TableGameInfo, T> selector, TableGameInfo[] originalGames, bool reverse)
        {
            TableGameInfo[] sortedGames = null;
            driver.FindElement(By.Id(headerId)).Click();
            driver.WaitUntil(d =>
            {
                sortedGames = TableHelper.ParseGameTable(driver);
                var sortedValues = sortedGames.Select(selector).ToArray();
                return (reverse ? sortedValues.OrderBy(n => n).Reverse() : sortedValues.OrderBy(n => n)).SequenceEqual(sortedValues);
            });
            AssertExtentions.AssertEqualSets(originalGames, sortedGames);
        }

        private static void TestColumnSort<T>(IWebDriver driver, string headerId, Func<TableGameInfo, T> selector)
        {
            var originalGames = TableHelper.ParseGameTable(driver);
            TestColumnSort(driver, headerId, selector, originalGames, false);
            TestColumnSort(driver, headerId, selector, originalGames, true);
        }

        [TestMethod]
        public void TestTableSort()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                TestColumnSort(driver, SiteConstants.SteamNameTitle, g => g.SteamName);
                TestColumnSort(driver, SiteConstants.SteamPlaytimeTitle, g => g.SteamPlaytime);
                TestColumnSort(driver, SiteConstants.MainPlaytimeTitle, g => g.MainPlaytime);
                TestColumnSort(driver, SiteConstants.ExtrasPlaytimeTitle, g => g.ExtrasPlaytime);
                TestColumnSort(driver, SiteConstants.CompletionistPlaytimeTitle, g=> g.CompletionistPlaytime);
                TestColumnSort(driver, SiteConstants.HltbNameTitle, g => g.HltbName);
            });
        }

        private static int GetTablePageCount(IWebDriver driver)
        {
            return TableHelper.FindGameRows(driver).Count();
        }

        private static void Navigate(IWebDriver driver, string navigationElementId)
        {
            var firstGameRow = TableHelper.FindGameRows(driver).First();
            driver.FindElement(By.Id(navigationElementId)).Click();
            driver.WaitUntil(ExpectedConditions.StalenessOf(firstGameRow));
        }

        private static bool NavigationEnabled(IWebElement element)
        {
            return !element.GetAttribute("class").Contains("cursor-default");
        }

        [TestMethod]
        public void TestPageNavigation()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.FirstPageAnchorId))), "Expected first page button to be disabled");
                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.PreviousPageAnchorId))), "Expected previous page button to be disabled");
                var firstPageGames = TableHelper.ParseGameTable(driver);

                Navigate(driver, SiteConstants.NextPageAnchorId);
                var secondPageGames = TableHelper.ParseGameTable(driver);
                AssertExtentions.AssertDistinctSets(firstPageGames, secondPageGames);

                Navigate(driver, SiteConstants.FixedPageAnchorIdPrefix + "4");
                var fourthPageGames = TableHelper.ParseGameTable(driver);
                AssertExtentions.AssertDistinctSets(firstPageGames, fourthPageGames);
                AssertExtentions.AssertDistinctSets(secondPageGames, fourthPageGames);

                Navigate(driver, SiteConstants.LastPageAnchorId);
                var lastPageGames = TableHelper.ParseGameTable(driver);
                AssertExtentions.AssertDistinctSets(firstPageGames, lastPageGames);
                AssertExtentions.AssertDistinctSets(secondPageGames, lastPageGames);
                AssertExtentions.AssertDistinctSets(fourthPageGames, lastPageGames);

                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.NextPageAnchorId))), "Expected last page button to be disabled");
                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.LastPageAnchorId))), "Expected next page button to be disabled");

                Navigate(driver, SiteConstants.PreviousPageAnchorId);
                var secondLastPageGames = TableHelper.ParseGameTable(driver);
                AssertExtentions.AssertDistinctSets(firstPageGames, secondLastPageGames);
                AssertExtentions.AssertDistinctSets(secondPageGames, secondLastPageGames);
                AssertExtentions.AssertDistinctSets(fourthPageGames, secondLastPageGames);
                AssertExtentions.AssertDistinctSets(lastPageGames, secondLastPageGames);

                Navigate(driver, SiteConstants.FirstPageAnchorId);
                AssertExtentions.AssertEqualSequences(firstPageGames, TableHelper.ParseGameTable(driver));

                foreach (var gamesPerPage in SiteConstants.GamesPerPageOptions)
                {
                    new SelectElement(driver.FindElement(By.Id(SiteConstants.GamesPerPageSelectId))).SelectByValue(gamesPerPage.ToString());
                    Assert.AreEqual(gamesPerPage, GetTablePageCount(driver), "Unexpected page game count");
                }
            });
        }

        [TestMethod]
        public void TestUpdateSuggestions()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                var gameRows = TableHelper.FindGameRows(driver).ToArray();

                Console.WriteLine("Updating HLTB correlation...");
                DialogHelper.TestDialog(driver, gameRows[0].FindElement(By.ClassName(SiteConstants.RowWrongGameAnchorClass)), SiteConstants.HltbUpdateModalId, () =>
                {
                    driver.FindElement(By.Id(SiteConstants.HltbUpdateInputId)).SetText("123");
                    driver.FindElement(By.Id(SiteConstants.HltbUpdateSubmitButtonId)).Click();
                });

                Console.WriteLine("Waiting for correlation suggestion to be submitted...");
                driver.WaitUntil(d => TableHelper.ParseGameRow(gameRows[0]).UpdateState == UpdateState.Submitted);

                Console.WriteLine("Suggesting non-game...");
                DialogHelper.TestDialog(driver, gameRows[1].FindElement(By.ClassName(SiteConstants.RowVerifyGameAnchorId)), SiteConstants.NonGameUpdateModalId, () =>
                {
                    driver.FindElement(By.Id(SiteConstants.NonGameUpdateButtonId)).Click();
                });

                Console.WriteLine("Waiting for non-game suggestion to be submitted...");
                driver.WaitUntil(d =>
                {
                    var gameInfo = TableHelper.ParseGameRow(gameRows[1]);
                    return gameInfo.UpdateState == UpdateState.Submitted && !gameInfo.Included;
                });
            });
        }

        private void AssertActiveFilterNotifications(IWebDriver driver, bool displayed)
        {
            Assert.AreEqual(displayed, driver.FindElement(By.Id(SiteConstants.SummaryFilterActive)).Displayed, "unexpected summary filter active notification visibility");
            Assert.AreEqual(displayed, driver.FindElement(By.Id(SiteConstants.SlicingFilterActive)).Displayed, "unexpected slicing filter active notification visibility");
        }

        [TestMethod]
        public void TestTextFilter()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                Console.WriteLine("Setting filter to include two games...");
                FilterHelper.SetTextFilter(driver, "in");
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2);
                AssertActiveFilterNotifications(driver, true);
                AssertExtentions.AssertEqualSets(new[] {GameConstants.RoninSteamName, GameConstants.GodsWillBeWatchingSteamName},
                    TableHelper.ParseGameTable(driver).Select(g => g.SteamName));

                Console.WriteLine("Setting filter to exclude all games...");
                FilterHelper.SetTextFilter(driver, Guid.NewGuid().ToString());
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 0);
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter...");
                FilterHelper.ClearTextFilter(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);
            });
        }

        [TestMethod]
        public void TestAdvancedFilter()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                Console.WriteLine("Setting advanced filter by release year...");
                FilterHelper.SetAdvancedFilter(driver, 2015, 2016);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2);
                AssertExtentions.AssertEqualSets(new[] {GameConstants.RoninSteamName, GameConstants.AFistfulOfGunSteamName},
                    TableHelper.ParseGameTable(driver).Select(g => g.SteamName));
                AssertActiveFilterNotifications(driver, true);

                FilterHelper.ClearAdvancedFilter(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting advanced filter by Metacritic score...");
                FilterHelper.SetAdvancedFilter(driver, 2014, -1, 60, 70);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 1);
                AssertExtentions.AssertEqualSets(new[] {GameConstants.GodsWillBeWatchingSteamName }, TableHelper.ParseGameTable(driver).Select(g => g.SteamName));
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Setting advanced filter by genre...");
                FilterHelper.SetAdvancedFilter(driver, -1, -1, -1, -1, new [] { GameConstants.ActionGenre });
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 0);
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter (externally)...");
                FilterHelper.ClearAdvancedFilterExternally(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting combined filter and advanced filter...");
                FilterHelper.SetAdvancedFilter(driver, -1, -1, -1, -1, new[] { GameConstants.ActionGenre });
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2);
                FilterHelper.SetTextFilter(driver, "gun");
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 1);
                AssertExtentions.AssertEqualSets(new[] {GameConstants.AFistfulOfGunSteamName }, TableHelper.ParseGameTable(driver).Select(g => g.SteamName));
            });
        }
    }
}
