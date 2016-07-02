using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using UITests.Constants;

namespace UITests.Helpers
{
    public static class TableHelper
    {
        private static double GetPlaytime(string playtime)
        {
            Console.WriteLine("Parsing playtime...");
            return Double.Parse(playtime.Remove(playtime.Length - 1));
        }

        public static TableGameInfo ParseGameRow(IWebElement gameRow)
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
                TableGameInfo(included, steamName, verifiedFinite, currentPlayTime, mainPlaytime, extrasPlaytime, completionistPlaytime, hltbName, verifiedCorrelation, updateState);
        }

        public static IWebElement FindTableBody(IWebDriver driver)
        {
            return driver.FindElement(By.Id(SiteConstants.GameTableId)).FindElement(By.TagName("tbody"));
        }

        public static IEnumerable<IWebElement> FindGameRows(IWebDriver driver)
        {
            return FindTableBody(driver).FindElements(By.TagName("tr")).Where(e => e.GetAttribute("class") != SiteConstants.RowBlankClass);
        }

        public static TableGameInfo[] ParseGameTable(IWebDriver driver)
        {
            Console.WriteLine("Parsing game table...");
            return FindGameRows(driver).Select(ParseGameRow).ToArray();
        }
    }
}
