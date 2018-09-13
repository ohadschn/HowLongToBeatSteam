using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;
using CollectionAssert = UITests.Util.CollectionAssert;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;
using static System.FormattableString;

namespace UITests.Tests
{
    [TestClass]
    public class TableTests
    {
        private static void AssertHltbsUserTable(TableGameInfo[] games, bool mobile)
        {
            foreach (var game in games)
            {
                Assert.IsTrue(game.Included, Invariant($"Expected all games to be included but the following was not: {game.SteamName}"));
                Assert.AreEqual(0, game.SteamPlaytime, Invariant($"Expected zero playtime for: {game.SteamName}"));

                Assert.IsTrue(game.MainPlaytime > 0, Invariant($"Expected main playtime to be greater than zero: {game.SteamName}"));
                Assert.IsTrue(game.ExtrasPlaytime > 0, Invariant($"Expected extras playtime to be greater than zero: {game.SteamName}"));
                Assert.IsTrue(game.CompletionistPlaytime > 0, Invariant($"Expected completionist playtime to be greater than zero: {game.SteamName}"));

                Assert.IsTrue(game.MainPlaytime <= game.ExtrasPlaytime, Invariant($"Main playtime exceeds extras playtime for: {game.SteamName}"));
                Assert.IsTrue(game.ExtrasPlaytime <= game.CompletionistPlaytime, Invariant($"Extras playtime exceeds completionist playtime for: {game.SteamName}"));

                Assert.AreEqual(!mobile && (game.SteamName == GameConstants.RoninSteamName), game.VerifiedFinite, Invariant($"Expected verified finite for: {game.SteamName}"));
                Assert.IsFalse(game.VerifiedCorrelation, Invariant($"Unexpected verified correlation game: {game.SteamName}"));
            }

            var expectedGames = new[]
            {
                    new {SteamName = GameConstants.AFistfulOfGunSteamName, HltbName = mobile ? null : GameConstants.AFistfulOfGunHltbName},
                    new {SteamName = GameConstants.GodsWillBeWatchingSteamName, HltbName = mobile ? null : GameConstants.GodsWillBeWatchingHltbName},
                    new {SteamName = GameConstants.RoninSteamName, HltbName = mobile ? null : GameConstants.RoninHltbName}
                };

            CollectionAssert.AssertEqualSets(expectedGames, games.Select(g => new { g.SteamName, g.HltbName }), "Unexpected games in table");
        }

        [TestMethod]
        public void TestTableEntries()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);
                AssertHltbsUserTable(TableHelper.ParseGameTable(driver), false);
            });
        }

        [TestMethod]
        public void TestMobileTableEntries()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);
                AssertHltbsUserTable(TableHelper.ParseGameTable(driver, true), true);
            }, Browsers.Nexus7Chrome);
        }

        [TestMethod]
        public void TestTableInclusion()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

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

        [TestMethod]
        public void TestMissingGames()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToMissingGamesPage(driver);
                driver.WaitUntil(d =>
                {
                    var games = TableHelper.ParseGameTable(d);
                    return games.Length >= 10 && games.All(g => g.MissingCorrelation);
                }, Invariant($"Could not verify missing games page"));
            });
        }

        private static void TestColumnSort<T>(IWebDriver driver, string headerId, Func<TableGameInfo, T> selector, IEnumerable<TableGameInfo> originalGames, bool reverse)
        {
            TableGameInfo[] sortedGames = null;
            driver.FindElement(By.Id(headerId)).Click();
            driver.WaitUntil(d =>
            {
                sortedGames = TableHelper.ParseGameTable(driver);
                var sortedValues = sortedGames.Select(selector).ToArray();
                return (reverse ? sortedValues.OrderBy(n => n).Reverse() : sortedValues.OrderBy(n => n)).SequenceEqual(sortedValues);
            }, Invariant($"Could not verify column sort: {headerId}"));
            CollectionAssert.AssertEqualSets(originalGames, sortedGames, Invariant($"Column sorting by '{headerId}' affected games in table"));
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
                SignInHelper.SignInWithId(driver);

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
            driver.WaitUntil(ExpectedConditions.StalenessOf(firstGameRow), Invariant($"Could not verify navigation by staleness of element: {firstGameRow}"));
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
                CollectionAssert.AssertDistinctSets(firstPageGames, secondPageGames, "Common games found in first and second table pages");

                Navigate(driver, SiteConstants.FixedPageAnchorIdPrefix + "4");
                var fourthPageGames = TableHelper.ParseGameTable(driver);
                CollectionAssert.AssertDistinctSets(firstPageGames, fourthPageGames, "Common games found in first and fourth table pages");
                CollectionAssert.AssertDistinctSets(secondPageGames, fourthPageGames, "Common games found in second and fourth game pages");

                Navigate(driver, SiteConstants.LastPageAnchorId);
                var lastPageGames = TableHelper.ParseGameTable(driver);
                CollectionAssert.AssertDistinctSets(firstPageGames, lastPageGames, "Common games found in first and last table pages");
                CollectionAssert.AssertDistinctSets(secondPageGames, lastPageGames, "Common games found in second and last table pages");
                CollectionAssert.AssertDistinctSets(fourthPageGames, lastPageGames, "Common games found in fourth and last table pages");

                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.NextPageAnchorId))), "Expected last page button to be disabled");
                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.LastPageAnchorId))), "Expected next page button to be disabled");

                Navigate(driver, SiteConstants.PreviousPageAnchorId);
                var secondLastPageGames = TableHelper.ParseGameTable(driver);
                CollectionAssert.AssertDistinctSets(firstPageGames, secondLastPageGames, "Common games found in first and second last table pages");
                CollectionAssert.AssertDistinctSets(secondPageGames, secondLastPageGames, "Common games found in second and second last table pages");
                CollectionAssert.AssertDistinctSets(fourthPageGames, secondLastPageGames, "Common games found in fourth and second last table pages");
                CollectionAssert.AssertDistinctSets(lastPageGames, secondLastPageGames, "Common games found in last and second last table pages");

                Navigate(driver, SiteConstants.FirstPageAnchorId);
                CollectionAssert.AssertEqualSequences(firstPageGames, TableHelper.ParseGameTable(driver), "Inconsistent games in first table page");

                foreach (var gamesPerPage in SiteConstants.GamesPerPageOptions)
                {
                    new SelectElement(driver.FindElement(By.Id(SiteConstants.GamesPerPageSelectId))).SelectByValue(gamesPerPage.ToString(CultureInfo.InvariantCulture));
                    Assert.AreEqual(gamesPerPage, GetTablePageCount(driver), "Unexpected page game count");
                }
            });
        }

        [TestMethod]
        public void TestUpdateSuggestions()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                var gameRows = TableHelper.FindGameRows(driver).ToArray();

                Console.WriteLine("Updating HLTB correlation...");
                DialogHelper.TestDialog(driver, gameRows[0].FindElement(By.ClassName(SiteConstants.RowWrongGameAnchorClass)), SiteConstants.HltbUpdateModalId, () =>
                {
                    driver.FindElement(By.Id(SiteConstants.HltbUpdateInputId)).SetText("123");
                    driver.FindElement(By.Id(SiteConstants.HltbUpdateSubmitButtonId)).Click();
                });

                Console.WriteLine("Waiting for correlation suggestion to be submitted...");
                driver.WaitUntil(d => TableHelper.ParseGameRow(driver, gameRows[0]).UpdateState == UpdateState.Submitted, "Could not verify successful correlation submission");

                Console.WriteLine("Suggesting non-game...");
                DialogHelper.TestDialog(driver, gameRows[1].FindElement(By.ClassName(SiteConstants.RowVerifyGameAnchorId)), SiteConstants.NonGameUpdateModalId, () =>
                {
                    driver.FindElement(By.Id(SiteConstants.NonGameUpdateButtonId)).Click();
                });

                Console.WriteLine("Waiting for non-game suggestion to be submitted...");
                driver.WaitUntil(d =>
                {
                    var gameInfo = TableHelper.ParseGameRow(driver, gameRows[1]);
                    return gameInfo.UpdateState == UpdateState.Submitted && !gameInfo.Included;
                }, "Could not verify successful non-game submission");
            });
        }

        private static void AssertActiveFilterNotifications(ISearchContext driver, bool displayed)
        {
            Assert.AreEqual(displayed, driver.FindElement(By.Id(SiteConstants.SummaryFilterActive)).Displayed, "unexpected summary filter active notification visibility");
            Assert.AreEqual(displayed, driver.FindElement(By.Id(SiteConstants.SlicingFilterActive)).Displayed, "unexpected slicing filter active notification visibility");
        }

        [TestMethod]
        public void TestTextFilter()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                Console.WriteLine("Setting filter to include two games...");
                var filter = "in";
                FilterHelper.SetTextFilter(driver, filter);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2, Invariant($"Could not verify filter {filter}"));
                AssertActiveFilterNotifications(driver, true);
                CollectionAssert.AssertEqualSets(new[] {GameConstants.RoninSteamName, GameConstants.GodsWillBeWatchingSteamName},
                    TableHelper.ParseGameTable(driver).Select(g => g.SteamName), Invariant($"Could not verify filter {filter}"));

                Console.WriteLine("Setting filter to exclude all games...");
                filter = Guid.NewGuid().ToString();
                FilterHelper.SetTextFilter(driver, filter);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 0, Invariant($"Could not verify filter {filter}"));
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter...");
                FilterHelper.ClearTextFilter(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3, "Could not verify cleared filter");
                AssertActiveFilterNotifications(driver, false);
            });
        }

        [TestMethod]
        public void TestAdvancedFilter()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                Console.WriteLine("Setting advanced filter by release year...");
                FilterHelper.SetAdvancedFilter(driver, 2015, 2016);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2, "Could not verify release year advanced filter");
                CollectionAssert.AssertEqualSets(new[] {GameConstants.RoninSteamName, GameConstants.AFistfulOfGunSteamName},
                    TableHelper.ParseGameTable(driver).Select(g => g.SteamName), "Could not verify release year advanced filter");
                AssertActiveFilterNotifications(driver, true);

                FilterHelper.ClearAdvancedFilter(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3, "Could not verify cleared advanced filter");
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting advanced filter by Metacritic score...");
                FilterHelper.SetAdvancedFilter(driver, 2014, -1, 60, 70);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 1, "Could not verify metacritic advanced filter");
                CollectionAssert.AssertEqualSets(new[] {GameConstants.GodsWillBeWatchingSteamName}, TableHelper.ParseGameTable(driver).Select(g => g.SteamName),
                    "Could not verify metacritic advanced filter");
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Setting advanced filter by genre...");
                FilterHelper.SetAdvancedFilter(driver, -1, -1, -1, -1, new [] { GameConstants.ActionGenre });
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 0, "Could not verify metacritic+genre advanced filter");
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter (externally)...");
                FilterHelper.ClearAdvancedFilterExternally(driver);
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 3, "Could not verify externally cleared advanced filter");
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting combined filter and advanced filter...");
                FilterHelper.SetAdvancedFilter(driver, -1, -1, -1, -1, new[] { GameConstants.ActionGenre });
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 2, "Could not verify genre advanced filter");
                FilterHelper.SetTextFilter(driver, "gun");
                driver.WaitUntil(d => GameSummaryHelper.GetGameCount(driver) == 1, "Could not verify combined filter and advanced filter");
                CollectionAssert.AssertEqualSets(new[] {GameConstants.AFistfulOfGunSteamName}, TableHelper.ParseGameTable(driver).Select(g => g.SteamName),
                    "Could not verify combined filter and advanced filter");
            });
        }
    }
}