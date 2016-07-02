using System;
using System.Globalization;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public static class GameSummaryHelper
    {
        public static int GetGameCount(IWebDriver driver)
        {
            Console.WriteLine("Locating game summary title...");

            int gameCount = Int32.MaxValue;
            driver.WaitUntil(d =>
                Int32.TryParse(driver.FindElement(By.Id(SiteConstants.GamesFoundTitleId)).Text, NumberStyles.Number, CultureInfo.InvariantCulture, out gameCount) &&
                FilterHelper.GetFilterGameCount(driver) == gameCount, "Could not determine game count");

            return gameCount;
        }

        private static TimeSpan GetPlaytime(IWebDriver driver, By by)
        {
            return TestUtil.FreetextDurationToTimespan(driver.FindElement(by).Text);
        }

        public static TimeSpan GetRemainingMainPlaytime(IWebDriver driver)
        {
            Console.WriteLine("Getting main playtime...");
            return GetPlaytime(driver, By.Id(SiteConstants.MainPlaytimeRemainingSpan));
        }

        public static TimeSpan GetRemainingExtrasPlaytime(IWebDriver driver)
        {
            Console.WriteLine("Getting extras playtime...");
            return GetPlaytime(driver, By.Id(SiteConstants.ExtrasPlaytimeRemainingSpan));
        }

        public static TimeSpan GetRemainingCompletionistPlaytime(IWebDriver driver)
        {
            Console.WriteLine("Getting completionist playtime...");
            return GetPlaytime(driver, By.Id(SiteConstants.CompletionistPlaytimeRemainingSpan));
        }
    }
}
