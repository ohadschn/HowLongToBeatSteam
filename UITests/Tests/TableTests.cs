using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    enum UpdateState
    {
        None,
        InProgress,
        Submitted,
        Failure
    }

    class GameInfo : IEquatable<GameInfo>
    {
        public bool Included { get;  }
        public string SteamName { get; }
        public bool VerifiedFinite { get; }
        public double SteamPlaytime { get; }
        public double MainPlaytime { get; }
        public double ExtrasPlaytime { get; }
        public double CompletionistPlaytime { get; }
        public string HltbName { get; }
        public bool VerifiedCorrelation { get; }
        public UpdateState UpdateState { get; }

        public GameInfo(bool included, string steamName, bool verifiedFinite, double steamPlaytime,
            double mainPlaytime, double extrasPlaytime, double completionistPlaytime, string hltbName, bool verifiedCorrelation, UpdateState updateState)
        {
            Included = included;
            SteamName = steamName;
            VerifiedFinite = verifiedFinite;
            SteamPlaytime = steamPlaytime;
            MainPlaytime = mainPlaytime;
            ExtrasPlaytime = extrasPlaytime;
            CompletionistPlaytime = completionistPlaytime;
            VerifiedCorrelation = verifiedCorrelation;
            HltbName = hltbName;
            UpdateState = updateState;
        }

        public bool Equals(GameInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Included == other.Included && string.Equals(SteamName, other.SteamName) && VerifiedFinite == other.VerifiedFinite && SteamPlaytime.Equals(other.SteamPlaytime) &&
                   MainPlaytime.Equals(other.MainPlaytime) && ExtrasPlaytime.Equals(other.ExtrasPlaytime) && CompletionistPlaytime.Equals(other.CompletionistPlaytime) &&
                   string.Equals(HltbName, other.HltbName) && VerifiedCorrelation == other.VerifiedCorrelation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((GameInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Included.GetHashCode();
                hashCode = (hashCode*397) ^ (SteamName?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ VerifiedFinite.GetHashCode();
                hashCode = (hashCode*397) ^ SteamPlaytime.GetHashCode();
                hashCode = (hashCode*397) ^ MainPlaytime.GetHashCode();
                hashCode = (hashCode*397) ^ ExtrasPlaytime.GetHashCode();
                hashCode = (hashCode*397) ^ CompletionistPlaytime.GetHashCode();
                hashCode = (hashCode*397) ^ (HltbName?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ VerifiedCorrelation.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"Included: {Included}, SteamName: {SteamName}, VerifiedFinite: {VerifiedFinite}, SteamPlaytime: {SteamPlaytime}, MainPlaytime: {MainPlaytime}, ExtrasPlaytime: {ExtrasPlaytime}, CompletionistPlaytime: {CompletionistPlaytime}, HltbName: {HltbName}, VerifiedCorrelation: {VerifiedCorrelation}";
        }
    }

    [TestClass]
    public class TableTests
    {
        private static double GetPlaytime(string playtime)
        {
            Console.WriteLine("Parsing playtime...");
            return Double.Parse(playtime.Remove(playtime.Length - 1));
        }

        private static GameInfo ParseGameRow(IWebElement gameRow)
        {
            Console.WriteLine("Parsing game row...");
            bool included = gameRow.FindElement(By.ClassName(SiteConstants.RowIncludedCheckboxClass)).Selected;
            string steamName = gameRow.FindElement(By.ClassName(SiteConstants.RowSteamNameSpanClass)).Text;
            bool verifiedFinite = gameRow.FindElement(By.ClassName(SiteConstants.RowVerifiedGameSpanClass)).Displayed;
            double currentPlayTime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowSteamPlaytimeCellClass)).Text);
            double mainPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowMainPlaytimeCellClass)).Text);
            double extrasPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowExtrasPlaytimeCellClass)).Text);
            double completionistPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowCompletionistPlaytimeCellClass)).Text);
            string hltbName = gameRow.FindElement(By.ClassName(SiteConstants.RowHltbNameAnchorClass)).Text;
            bool verifiedCorrelation = !gameRow.FindElement(By.ClassName(SiteConstants.RowWrongGameAnchorClass)).Displayed;
            UpdateState updateState = gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdatingSpanClass)).Displayed
                ? UpdateState.InProgress
                : (gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdateSubmittedClass)).Displayed
                    ? UpdateState.Submitted
                    : (gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdateFailedClass)).Displayed ? UpdateState.Failure : UpdateState.None));

            return new 
                GameInfo(included, steamName, verifiedFinite, currentPlayTime, mainPlaytime, extrasPlaytime, completionistPlaytime, hltbName, verifiedCorrelation, updateState);
        }

        private static IEnumerable<IWebElement> FindGameRows(IWebDriver driver)
        {
            return FindTableBody(driver).FindElements(By.TagName("tr")).Where(e => e.GetAttribute("class") != SiteConstants.RowBlankClass);
        }

        private static IWebElement FindTableBody(IWebDriver driver)
        {
            return driver.FindElement(By.Id(SiteConstants.GameTableId)).FindElement(By.TagName("tbody"));
        }

        private static GameInfo[] ParseGameTable(IWebDriver driver)
        {
            Console.WriteLine("Parsing game table...");
            return FindGameRows(driver).Select(ParseGameRow).ToArray();
        }

        [TestMethod]
        public void TestTableEntries()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser, WaitType.PageLoad);

                var games = ParseGameTable(driver);
                foreach (var game in games)
                {
                    Assert.IsTrue(game.Included, $"Expected all games to be included but the following was not: {game.SteamName}");
                    Assert.AreEqual(0, game.SteamPlaytime, $"Expected zero playtime for: {game.SteamName}");

                    Assert.IsTrue(game.MainPlaytime > 0, $"Expected main playtime to be greater than zero: {game.SteamName}");
                    Assert.IsTrue(game.ExtrasPlaytime > 0, $"Expected extras playtime to be greater than zero: {game.SteamName}");
                    Assert.IsTrue(game.CompletionistPlaytime > 0, $"Expected completionist playtime to be greater than zero: {game.SteamName}");

                    Assert.IsTrue(game.MainPlaytime <= game.ExtrasPlaytime, $"Main playtime exceeds extras playtime for: {game.SteamName}");
                    Assert.IsTrue(game.ExtrasPlaytime <= game.CompletionistPlaytime, $"Extras playtime exceeds completionist playtime for: {game.SteamName}");

                    if (game.SteamName == "RONIN")
                    {
                        Assert.IsTrue(game.VerifiedFinite, $"Expected verified finite for: {game.SteamName}");
                    }
                    else
                    { 
                        Assert.IsFalse(game.VerifiedFinite, $"Unexpected verified finite for: {game.SteamName}");
                    }

                    Assert.IsFalse(game.VerifiedCorrelation, $"Unexpected verified correlation game: {game.SteamName}");
                }

                var tableGames = games.OrderBy(g => g.SteamName).Select(g => new {g.SteamName, g.HltbName}).ToArray();
                var expectedGames = new []
                {
                    new {SteamName = "A Fistful of Gun", HltbName = "A Fistful of Gun"},
                    new {SteamName = "Gods Will Be Watching", HltbName = "Gods Will Be Watching"},
                    new {SteamName = "RONIN", HltbName = "Ronin"}
                };
                Assert.IsTrue(expectedGames.SequenceEqual(tableGames), 
                    $"Mismatched games. Expected: {String.Join(",", expectedGames.Select(a => a.ToString()))} Actual: {String.Join(",", tableGames.Select(g => g.ToString()))}");
            });
        }

        [TestMethod]
        public void TestTableInclusion()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser);

                var originalMain = SiteHelper.GetRemainingMainPlaytime(driver);
                var originalExtras = SiteHelper.GetRemainingExtrasPlaytime(driver);
                var originalCompletionist = SiteHelper.GetRemainingCompletionistPlaytime(driver);

                var inclusionCheckboxes = FindTableBody(driver).FindElements(By.ClassName(SiteConstants.RowIncludedCheckboxClass));

                Console.WriteLine("Excluding a game...");
                inclusionCheckboxes.First().Click();
                Assert.AreEqual(2, SiteHelper.GetGameCount(driver), "Expected exclusion of game to reduce game count");

                var mainPostExclusion = SiteHelper.GetRemainingMainPlaytime(driver);
                var extrasPostExclusion = SiteHelper.GetRemainingExtrasPlaytime(driver);
                var completionistPostExclusion = SiteHelper.GetRemainingCompletionistPlaytime(driver);

                Assert.IsTrue(mainPostExclusion < originalMain, "Expected exclusion of game to reduce original remaining main playtime");
                Assert.IsTrue(extrasPostExclusion < originalExtras, "Expected exclusion of game to reduce original remaining main playtime");
                Assert.IsTrue(completionistPostExclusion < originalCompletionist, "Expected exclusion of game to reduce original remaining main playtime");

                Console.WriteLine("Excluding remaining games...");
                foreach (var inclusionCheckbox in inclusionCheckboxes.Skip(1))
                {
                    inclusionCheckbox.Click();
                }

                Assert.AreEqual(0, SiteHelper.GetGameCount(driver), "Expected zero game count when all games are excluded");
                Assert.AreEqual(TimeSpan.Zero, SiteHelper.GetRemainingMainPlaytime(driver), "Expected zero main remaining playtime");
                Assert.AreEqual(TimeSpan.Zero, SiteHelper.GetRemainingExtrasPlaytime(driver), "Expected zero extras remaining playtime");
                Assert.AreEqual(TimeSpan.Zero, SiteHelper.GetRemainingCompletionistPlaytime(driver), "Expected zero completionist remaining playtime");
            });
        }

        private static void TestColumnSort<T>(IWebDriver driver, string headerId, Func<GameInfo, T> selector, GameInfo[] originalGames, bool reverse)
        {
            GameInfo[] sortedGames = null;
            driver.FindElement(By.Id(headerId)).Click();
            driver.WaitUntil(d =>
            {
                sortedGames = ParseGameTable(driver);
                var sortedValues = sortedGames.Select(selector).ToArray();
                return (reverse ? sortedValues.OrderBy(n => n).Reverse() : sortedValues.OrderBy(n => n)).SequenceEqual(sortedValues);
            });
            TestUtil.AssertEqualSets(originalGames, sortedGames);
        }

        private static void TestColumnSort<T>(IWebDriver driver, string headerId, Func<GameInfo, T> selector)
        {
            var originalGames = ParseGameTable(driver);
            TestColumnSort(driver, headerId, selector, originalGames, false);
            TestColumnSort(driver, headerId, selector, originalGames, true);
        }

        [TestMethod]
        public void TestTableSort()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser);

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
            return FindGameRows(driver).Count();
        }

        private static void Navigate(IWebDriver driver, string navigationElementId)
        {
            var firstGameRow = FindGameRows(driver).First();
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
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.FirstPageAnchorId))), "Expected first page button to be disabled");
                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.PreviousPageAnchorId))), "Expected previous page button to be disabled");
                var firstPageGames = ParseGameTable(driver);

                Navigate(driver, SiteConstants.NextPageAnchorId);
                var secondPageGames = ParseGameTable(driver);
                TestUtil.AssertDistinctSets(firstPageGames, secondPageGames);

                Navigate(driver, SiteConstants.FixedPageAnchorIdPrefix + "4");
                var fourthPageGames = ParseGameTable(driver);
                TestUtil.AssertDistinctSets(firstPageGames, fourthPageGames);
                TestUtil.AssertDistinctSets(secondPageGames, fourthPageGames);

                Navigate(driver, SiteConstants.LastPageAnchorId);
                var lastPageGames = ParseGameTable(driver);
                TestUtil.AssertDistinctSets(firstPageGames, lastPageGames);
                TestUtil.AssertDistinctSets(secondPageGames, lastPageGames);
                TestUtil.AssertDistinctSets(fourthPageGames, lastPageGames);

                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.NextPageAnchorId))), "Expected last page button to be disabled");
                Assert.IsFalse(NavigationEnabled(driver.FindElement(By.Id(SiteConstants.LastPageAnchorId))), "Expected next page button to be disabled");

                Navigate(driver, SiteConstants.PreviousPageAnchorId);
                var secondLastPageGames = ParseGameTable(driver);
                TestUtil.AssertDistinctSets(firstPageGames, secondLastPageGames);
                TestUtil.AssertDistinctSets(secondPageGames, secondLastPageGames);
                TestUtil.AssertDistinctSets(fourthPageGames, secondLastPageGames);
                TestUtil.AssertDistinctSets(lastPageGames, secondLastPageGames);

                Navigate(driver, SiteConstants.FirstPageAnchorId);
                TestUtil.AssertEqualSequences(firstPageGames, ParseGameTable(driver));

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
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser);

                var gameRows = FindGameRows(driver).ToArray();

                Console.WriteLine("Updating HLTB correlation...");
                gameRows[0].FindElement(By.ClassName(SiteConstants.RowWrongGameAnchorClass)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.HltbUpdateModalId), 3);

                driver.FindElement(By.Id(SiteConstants.HltbUpdateInputId)).SetText("123");
                driver.FindElement(By.Id(SiteConstants.HltbUpdateSubmitButtonId)).Click();

                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.HltbUpdateModalId)).Displayed);
                driver.WaitUntil(d => ParseGameRow(gameRows[0]).UpdateState == UpdateState.Submitted);

                Console.WriteLine("Suggesting non-game...");
                gameRows[1].FindElement(By.ClassName(SiteConstants.RowVerifyGameAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.NonGameUpdateModalId), 3);

                driver.FindElement(By.Id(SiteConstants.NonGameUpdateButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.HltbUpdateModalId)).Displayed);
                driver.WaitUntil(d =>
                {
                    var gameInfo = ParseGameRow(gameRows[1]);
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
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser);

                Console.WriteLine("Setting filter to include two games...");
                driver.FindElement(By.Id(SiteConstants.FilterInputId)).SetText("in");
                driver.WaitUntil(d => SiteHelper.GetGameCount(driver) == 2);
                AssertActiveFilterNotifications(driver, true);
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.SummaryFilterActive)).Displayed);
                TestUtil.AssertEqualSets(new[] { "RONIN", "Gods Will Be Watching" }, ParseGameTable(driver).Select(g => g.SteamName));

                Console.WriteLine("Setting filter to include zero games...");
                driver.FindElement(By.Id(SiteConstants.FilterInputId)).SetText("sdlkajsdhl");
                driver.WaitUntil(d => SiteHelper.GetGameCount(driver) == 0);
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter...");
                driver.FindElement(By.Id(SiteConstants.FilterInputId)).Clear();
                driver.WaitUntil(d => SiteHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);
            });
        }

        [TestMethod]
        public void TestAdvancedFilter()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.HltbsUser);

                Console.WriteLine("Setting advanced filter by release year...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.AdvancedFilterModalId), 3);

                driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearFromOptionsId), "2015");
                driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearToOptionsId), "2016");

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.AdvancedFilterModalId)).Displayed && SiteHelper.GetGameCount(driver) == 2);

                TestUtil.AssertEqualSets(new[] { "RONIN", "A Fistful of Gun" }, ParseGameTable(driver).Select(g => g.SteamName));
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.AdvancedFilterModalId), 3);

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.AdvancedFilterModalId)).Displayed && SiteHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting advanced filter by Metacritic score...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.AdvancedFilterModalId), 3);

                driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearFromOptionsId), "2014");
                driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacrticiFromOptionsId), "60");
                driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacriticToOptionsId), "70");

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.AdvancedFilterModalId)).Displayed && SiteHelper.GetGameCount(driver) == 1);
                TestUtil.AssertEqualSets(new[] { "Gods Will Be Watching" }, ParseGameTable(driver).Select(g => g.SteamName));
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Setting advanced filter by genre...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.AdvancedFilterModalId), 3);

                var genreSelect = new SelectElement(driver.FindElement(By.Id(SiteConstants.AdvancedFilterGenreOptionsId)));
                genreSelect.DeselectAll();
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.AdvancedFilterNoGenresSelectedSpanId)).Displayed,
                    "Expected no genre selected notification to be visible when no genres are selected");
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Enabled,
                    "Expected advanced filter apply button to be disabled when no genres are selected");
                genreSelect.SelectByValue("Action");

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.AdvancedFilterModalId)).Displayed && SiteHelper.GetGameCount(driver) == 0);
                AssertActiveFilterNotifications(driver, true);

                Console.WriteLine("Clearing filter (externally)...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearExternalSpanId)).Click();
                driver.WaitUntil(d => SiteHelper.GetGameCount(driver) == 3);
                AssertActiveFilterNotifications(driver, false);

                Console.WriteLine("Setting combined filter and advanced filter...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterAnchorId)).Click();
                driver.WaitUntilElementIsStationary(By.Id(SiteConstants.AdvancedFilterModalId), 3);

                genreSelect.DeselectAll();
                genreSelect.SelectByValue("Action");

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Click();
                driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.AdvancedFilterModalId)).Displayed && SiteHelper.GetGameCount(driver) == 2);
                driver.FindElement(By.Id(SiteConstants.FilterInputId)).SetText("gun");
                driver.WaitUntil(d => SiteHelper.GetGameCount(driver) == 1);
                TestUtil.AssertEqualSets(new[] { "A Fistful of Gun" }, ParseGameTable(driver).Select(g => g.SteamName));
            });
        }
    }
}
