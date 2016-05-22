using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum PlayStyle
    {
        Main,
        Extras,
        Completionist
    }

    public enum WaitType
    {
        None,
        PageLoad,
        Ads
    }

    public class SiteHelper
    {
        private const int MinAdHeight = 250;

        public static void WaitForSafeClicking(IWebDriver driver)
        {
            Console.WriteLine("Waiting for ads to display so that elements below them are not moved and can therefore be clicked reliably...");
            driver.WaitUntil(d => d.FindElement(By.Id(SiteConstants.AdsensePlaytimeDivId)).Size.Height > MinAdHeight &&
                                  d.FindElement(By.Id(SiteConstants.AdsenseSliceDivId)).Size.Height > MinAdHeight);
        }

        private static void WaitForLoad(IWebDriver driver, WaitType waitType)
        {
            if (waitType == WaitType.None)
            {
                return;
            }

            Console.WriteLine("Waiting for user page to load...");
            driver.WaitUntilElementIsVisible(By.Id(SiteConstants.PersonaAvatarImgId));
            Console.WriteLine("User page loaded");

            if (waitType == WaitType.Ads)
            {
                WaitForSafeClicking(driver);
            }
        }

        public static void SignInWithId(IWebDriver driver, string steamId, WaitType waitType = WaitType.Ads)
        {
            Console.WriteLine($"Navigating to deployment URL: {SiteConstants.LocalDeploymentUrl}...");
            driver.Url = SiteConstants.LocalDeploymentUrl;

            Console.WriteLine("Locating Steam ID textbox...");
            var steamIdText = driver.FindElement(By.Id(SiteConstants.SteamIdTextId));

            Console.WriteLine("Inputting Steam ID...");
            steamIdText.SendKeys(steamId);

            Console.WriteLine("Submitting form...");
            steamIdText.Submit();

            WaitForLoad(driver, waitType);
        }

        public static void SignInThroughSteam(IWebDriver driver, string steamUser, string steamPassword, WaitType waitType = WaitType.Ads)
        {
            Console.WriteLine($"Navigating to deployment URL: {SiteConstants.LocalDeploymentUrl}...");
            driver.Url = SiteConstants.LocalDeploymentUrl;

            Console.WriteLine("Clicking Steam sign in button...");
            driver.FindElement(By.Id(SiteConstants.SteamSignInButtonId)).Click();

            try
            {
                driver.WaitUntil(ExpectedConditions.UrlContains("Authentication"), TimeSpan.FromSeconds(3));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Navigation to authentication page did not take place (probably because the page wasn't fully loaded) - retrying...");
                driver.FindElement(By.Id(SiteConstants.SteamSignInButtonId)).Click();
            }

            Console.WriteLine("Waiting for Valve's Steam login button...");
            driver.WaitUntil(ExpectedConditions.ElementExists(By.Id(SiteConstants.ValveSteamLoginButtonId)), TimeSpan.FromSeconds(30));

            Console.WriteLine("Typing in Valve Steam user name..");
            var steamUsernameText = driver.FindElement(By.Id(SiteConstants.ValveSteamUsername));
            steamUsernameText.SetText(steamUser);

            Console.WriteLine("Typing in Valve Steam password...");
            var steamPasswordText = driver.FindElement(By.Id(SiteConstants.ValveSteamPassword));
            steamPasswordText.SetText(steamPassword);

            Console.WriteLine("Submitting Valve Steam login form...");
            steamPasswordText.Submit();

            WaitForLoad(driver, waitType);
        }

        public static void GoToCachedGamesPage(IWebDriver driver, WaitType waitType = WaitType.Ads)
        {
            Console.WriteLine($"Navigating to cached games page URL: {SiteConstants.CachedGamePage}...");
            driver.Url = SiteConstants.CachedGamePage;

            WaitForLoad(driver, waitType);
        }

        public static void CalculateSurvival(IWebDriver driver, Gender gender, int birthYear, int weeklyPlaytime, PlayStyle playStyle)
        {
            Console.WriteLine("Clicking survival calculator...");
            driver.FindElement(By.Id(SiteConstants.SurvivalCalculatorAnchorId)).Click();

            Console.WriteLine("Waiting for survival calculator to open...");
            var genderSelect = driver.WaitUntilElementIsVisible(By.Id(SiteConstants.SurvivalGenderSelectId));

            Console.WriteLine("Populating calculator settings...");
            new SelectElement(genderSelect).SelectByValue(gender.ToString());
            new SelectElement(driver.FindElement(By.Id(SiteConstants.SurvivalBirthYearSelectId))).SelectByValue(birthYear.ToString());
            new SelectElement(driver.FindElement(By.Id(SiteConstants.SurvivalWeeklyPlaytimeSelectId))).SelectByValue(weeklyPlaytime.ToString());
            new SelectElement(driver.FindElement(By.Id(SiteConstants.SurvivalPlayStyleSelectId))).SelectByValue(playStyle.ToString());

            Console.WriteLine("Starting calculation...");
            driver.FindElement(By.Id(SiteConstants.SurvivalCalculatorButtonId)).Click();

            Console.WriteLine("Waiting for calculation to complete...");
            driver.WaitUntilElementCondition(By.Id(SiteConstants.SurvivalBacklogCompletionLabelId), e => e.Text != SiteConstants.SurvivalNotCalculatedText);
        }

        private static int GetFilterGameCount(IWebDriver driver)
        {
            Console.WriteLine("Extracting filter game count text...");

            int gameCount = Int32.MaxValue;
            driver.WaitUntil(d =>
                Int32.TryParse(driver.FindElement(By.Id(SiteConstants.FilterGameCountSpanId)).Text.Split(' ')[0], NumberStyles.Number, CultureInfo.InvariantCulture, out gameCount));

            return gameCount;
        }

        public static int GetGameCount(IWebDriver driver)
        {
            Console.WriteLine("Locating game summary title...");

            int gameCount = Int32.MaxValue;
            driver.WaitUntil(d =>
                Int32.TryParse(driver.FindElement(By.Id(SiteConstants.GamesFoundTitleId)).Text, NumberStyles.Number, CultureInfo.InvariantCulture, out gameCount));

            Assert.AreEqual(gameCount, GetFilterGameCount(driver), "Expected game count and filter game count to be the same");
            return gameCount;
        }

        public static TimeSpan GetRemainingMainPlaytime(IWebDriver driver)
        {
            return GetPlaytime(driver, By.Id(SiteConstants.MainPlaytimeRemainingSpan));
        }

        public static TimeSpan GetRemainingExtrasPlaytime(IWebDriver driver)
        {
            return GetPlaytime(driver, By.Id(SiteConstants.ExtrasPlaytimeRemainingSpan));
        }

        public static TimeSpan GetRemainingCompletionistPlaytime(IWebDriver driver)
        {
            return GetPlaytime(driver, By.Id(SiteConstants.CompletionistPlaytimeRemainingSpan));
        }

        public static TimeSpan GetPlaytime(IWebDriver driver, By by)
        {
            return TestUtil.FreetextDurationToTimespan(driver.FindElement(by).Text);
        }
    }
}
