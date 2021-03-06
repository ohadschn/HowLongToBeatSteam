﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public static class TableHelper
    {
        private static double GetPlaytime(string playtime)
        {
            Console.WriteLine("Parsing playtime: {0}", playtime);
            if (String.IsNullOrWhiteSpace(playtime))
            {
                return -1;
            }
            return Double.Parse(playtime.Remove(playtime.Length - 1), CultureInfo.InvariantCulture);
        }

        private static void ParseMobilePlaytime(string text, out double steam, out double main, out double extras, out double completionist)
        {
            var match = Regex.Match(text, @"Current:\s*(.+)h.*Main:\s*(.+)h.*Extras:\s*(.+)h.*Completionist:\s*(.+)h.*");
            if (match.Groups.Count != 5 
                || !Double.TryParse(match.Groups[1].Value, out steam)
                || !Double.TryParse(match.Groups[2].Value, out main) 
                || !Double.TryParse(match.Groups[3].Value, out extras)
                || !Double.TryParse(match.Groups[4].Value, out completionist))
            {
                steam = main = extras = completionist = -1;
            }
        }

        private static TableGameInfo ParseMobileTableRow(IWebDriver driver, ISearchContext gameRow)
        {
            Console.WriteLine("Parsing mobile game row...");
            bool included = gameRow.FindElement(By.ClassName(SiteConstants.RowIncludedCheckboxClass)).Selected;
            string steamName = gameRow.FindElement(By.ClassName(SiteConstants.RowSteamNameSpanClass)).Text;

            var playtimeIndicator = gameRow.FindElement(By.ClassName(SiteConstants.RowMobilePlaytimeIndicatorSpanClass));
            Console.WriteLine("Waiting for playtime indicator tooltip...");
            TooltipHelper.AssertTooltip(driver, playtimeIndicator, "Current:");

            ParseMobilePlaytime(TooltipHelper.GetToolTipText(driver), out var steamPlaytime, out var main, out var extras, out var completionist);
            return new TableGameInfo(included, steamName, false, steamPlaytime, main, extras, completionist, false /*Not tested*/, null, false, UpdateState.None);
        }

        private static TableGameInfo ParseDesktopTableRow(ISearchContext gameRow)
        {
            Console.WriteLine("Parsing desktop game row...");
            bool included = gameRow.FindElement(By.ClassName(SiteConstants.RowIncludedCheckboxClass)).Selected;
            string steamName = gameRow.FindElement(By.ClassName(SiteConstants.RowSteamNameSpanClass)).Text;
            bool verifiedFinite = !gameRow.FindElement(By.ClassName(SiteConstants.RowVerifyGameAnchorId)).Displayed;
            double currentPlayTime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowSteamPlaytimeCellClass)).Text);
            double mainPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowMainPlaytimeCellClass)).Text);
            double extrasPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowExtrasPlaytimeCellClass)).Text);
            double completionistPlaytime = GetPlaytime(gameRow.FindElement(By.ClassName(SiteConstants.RowCompletionistPlaytimeCellClass)).Text);
            string hltbName = gameRow.FindElement(By.ClassName(SiteConstants.RowHltbNameAnchorClass)).Text;
            bool missingCorrelation = gameRow.FindElement(By.ClassName(SiteConstants.RowMissingCorrelationSpanClass)).Displayed;
            bool verifiedCorrelation = !gameRow.FindElement(By.ClassName(SiteConstants.RowWrongGameAnchorClass)).Displayed;
            UpdateState updateState = gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdatingSpanClass)).Displayed
                ? UpdateState.InProgress
                : (gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdateSubmittedClass)).Displayed
                    ? UpdateState.Submitted
                    : (gameRow.FindElement(By.ClassName(SiteConstants.RowCorrelationUpdateFailedClass)).Displayed ? UpdateState.Failure : UpdateState.None));

            return new
                TableGameInfo(included, steamName, verifiedFinite, currentPlayTime, mainPlaytime, extrasPlaytime, completionistPlaytime, missingCorrelation, hltbName, verifiedCorrelation, updateState);
        }

        public static TableGameInfo ParseGameRow([NotNull] IWebDriver driver, [NotNull] IWebElement gameRow)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (gameRow == null) throw new ArgumentNullException(nameof(gameRow));

            return driver.IsMobile() ? ParseMobileTableRow(driver, gameRow) : ParseDesktopTableRow(gameRow);
        }

        public static IWebElement FindTableBody([NotNull] IWebDriver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            return driver.FindElement(By.Id(SiteConstants.GameTableId)).FindElement(By.TagName("tbody"));
        }

        public static IEnumerable<IWebElement> FindGameRows([NotNull] IWebDriver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            return FindTableBody(driver).FindElements(By.TagName("tr")).Where(e => e.GetAttribute("class") != SiteConstants.RowBlankClass);
        }

        public static TableGameInfo[] ParseGameTable([NotNull] IWebDriver driver)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));

            Console.WriteLine("Parsing game table...");
            return FindGameRows(driver).Select(row => ParseGameRow(driver, row)).ToArray();
        }
    }
}