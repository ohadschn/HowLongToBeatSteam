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
                Int32.TryParse(driver.FindElement(By.Id(SiteConstants.FilterGameCountSpanId)).Text.Split(' ')[0], NumberStyles.Number, CultureInfo.InvariantCulture, out gameCount));

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
            Console.WriteLine($"Setting advanced filter to releaseYearFrom:{releaseYearFrom}, releaseYearTo:{releaseYearTo}, metacriticFrom:{metacriticFrom}, metaCriticTo:{metaCriticTo}, genres:{genres?.StringJoin()}  ");
            DialogHelper.TestDialog(driver, SiteConstants.AdvancedFilterAnchorId, SiteConstants.AdvancedFilterModalId, () =>
            {
                if (releaseYearFrom >= 0)
                {
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearFromOptionsId), releaseYearFrom.ToString(CultureInfo.InvariantCulture));
                }

                if (releaseYearTo >= 0)
                {
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterReleaseYearToOptionsId), releaseYearTo.ToString(CultureInfo.InvariantCulture));
                }

                if (metacriticFrom >= 0)
                {
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacrticiFromOptionsId), metacriticFrom.ToString(CultureInfo.InvariantCulture));
                }

                if (metaCriticTo >= 0)
                {
                    driver.SelectValue(By.Id(SiteConstants.AdvancedFilterMetacriticToOptionsId), metaCriticTo.ToString(CultureInfo.InvariantCulture));
                }

                if (genres != null)
                {
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
                driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearButtonId)).Click();
            });
        }

        public static void ClearAdvancedFilterExternally(IWebDriver driver)
        {
            driver.FindElement(By.Id(SiteConstants.AdvancedFilterClearExternalSpanId)).Click();
        }
    }
}
