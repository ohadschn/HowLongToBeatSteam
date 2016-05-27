using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    class FilterHelper
    {
        public static int GetFilterGameCount(IWebDriver driver)
        {
            Console.WriteLine("Extracting filter game count text...");

            int gameCount = Int32.MaxValue;
            driver.WaitUntil(d =>
                Int32.TryParse(driver.FindElement(By.Id(SiteConstants.FilterGameCountSpanId)).Text.Split(' ')[0], NumberStyles.Number, CultureInfo.InvariantCulture, out gameCount),
                "Could not determine filtered game count");

            return gameCount;
        }

        public static void SetTextFilter(IWebDriver driver, string filter)
        {
            Console.WriteLine($"Setting text filter to {filter}...");
            driver.FindElement(By.Id(SiteConstants.FilterInputId)).SetText(filter);
        }

        public static void ClearTextFilter(IWebDriver driver)
        {
            Console.WriteLine("Clearing filter...");
            driver.FindElement(By.Id(SiteConstants.FilterInputId)).Clear();
        }

        public static void SetAdvancedFilter(IWebDriver driver, int releaseYearFrom = -1, int releaseYearTo = -1, int metacriticFrom = -1, int metaCriticTo = -1, ICollection<string> genres = null)
        {
            DialogHelper.TestDialog(driver, SiteConstants.AdvancedFilterAnchorId, SiteConstants.AdvancedFilterModalId, () =>
            {
                if (releaseYearFrom >= 0)
                {
                    Console.WriteLine($"Setting advanced filter to releaseYearFrom: {releaseYearFrom}");
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearFromOptionsId), releaseYearFrom.ToString(CultureInfo.InvariantCulture));
                }

                if (releaseYearTo >= 0)
                {
                    Console.WriteLine($"Setting advanced filter to releaseYearTo: { releaseYearTo}");
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearToOptionsId), releaseYearTo.ToString(CultureInfo.InvariantCulture));
                }

                if (metacriticFrom >= 0)
                {
                    Console.WriteLine($"Setting advanced filter to metacriticFrom: { metacriticFrom}");
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacrticiFromOptionsId), metacriticFrom.ToString(CultureInfo.InvariantCulture));
                }

                if (metaCriticTo >= 0)
                {
                    Console.WriteLine($"Setting advanced filter to metaCriticTo: { metaCriticTo}");
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacriticToOptionsId), metaCriticTo.ToString(CultureInfo.InvariantCulture));
                }

                if (genres != null)
                {
                    Console.WriteLine($"Setting advanced filter to genres: {genres.StringJoin()}");
                    var genreSelect = new SelectElement(driver.FindElement(By.Id(SiteConstants.AdvancedFilterGenreOptionsId)));

                    genreSelect.DeselectAll();
                    Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.AdvancedFilterNoGenresSelectedSpanId)).Displayed,
                        "Expected no genre selected notification to be visible when no genres are selected");
                    Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Enabled,
                        "Expected advanced filter apply button to be disabled when no genres are selected");

                    foreach (var genre in genres)
                    {
                        genreSelect.SelectByValue(genre);
                    }
                }

                driver.FindElement(By.Id(SiteConstants.AdvancedFilterApplyButtonId)).Click();
            });
        }

        public static void ClearAdvancedFilter(IWebDriver driver)
        {
            DialogHelper.TestDialog(driver, SiteConstants.AdvancedFilterAnchorId, SiteConstants.AdvancedFilterModalId, () =>
            {
                Console.WriteLine("Clearing advanced filter...");
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearButtonId)).Click();
            });
        }

        public static void ClearAdvancedFilterExternally(IWebDriver driver)
        {
            Console.WriteLine("Clearing advanced filter (externally)...");
            driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearExternalSpanId)).Click();
        }
    }
}
