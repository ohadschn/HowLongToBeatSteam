using System;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using UITests.Constants;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Helpers
{
    public enum WaitType
    {
        None,
        PageLoad
    }

    public static class SignInHelper
    {
        private static void WaitForLoad(IWebDriver driver, WaitType waitType)
        {
            if (waitType == WaitType.None)
            {
                return;
            }

            Console.WriteLine("Waiting for user page to load...");
            driver.WaitUntilElementIsVisible(By.Id(SiteConstants.PersonaAvatarImgId), "Could not locate persona avatar image", TimeSpan.FromSeconds(20));
            driver.WaitUntilElementIsStationary(By.Id(SiteConstants.PersonaAvatarImgId), 3, "Could not detect scrolling animation completion", TimeSpan.FromSeconds(10));
            Console.WriteLine("User page loaded");
        }

        public static void SignInWithId(IWebDriver driver, string steamId, WaitType waitType = WaitType.PageLoad)
        {
            Console.WriteLine(Invariant($"Navigating to deployment URL: {SiteConstants.LocalDeploymentUrl}..."));
            driver.Url = SiteConstants.LocalDeploymentUrl;

            Console.WriteLine("Locating Steam ID textbox...");
            var steamIdText = driver.FindElement(By.Id(SiteConstants.SteamIdTextId));

            Console.WriteLine("Inputting Steam ID...");
            steamIdText.SendKeys(steamId);

            Console.WriteLine("Submitting form...");
            steamIdText.Submit();

            WaitForLoad(driver, waitType);
        }

        public static void SignInThroughSteam(IWebDriver driver, string steamUser, string steamPassword, WaitType waitType = WaitType.PageLoad)
        {
            Console.WriteLine(Invariant($"Navigating to deployment URL: {SiteConstants.LocalDeploymentUrl}..."));
            driver.Url = SiteConstants.LocalDeploymentUrl;

            Console.WriteLine("Clicking Steam sign in button...");
            driver.FindElement(By.Id(SiteConstants.SteamSignInButtonId)).Click();

            try
            {
                driver.WaitUntil(ExpectedConditions.UrlContains("Authentication"), "Could not verify authentication URL", TimeSpan.FromSeconds(3));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Navigation to authentication page did not take place (probably because the page wasn't fully loaded) - retrying...");
                driver.FindElement(By.Id(SiteConstants.SteamSignInButtonId)).Click();
            }

            Console.WriteLine("Waiting for Valve's Steam login button...");
            driver.WaitUntil(ExpectedConditions.ElementExists(By.Id(SiteConstants.ValveSteamLoginButtonId)), "Could not find Valve's Steam login button", TimeSpan.FromSeconds(30));

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

        public static void GoToCachedGamesPage(IWebDriver driver, WaitType waitType = WaitType.PageLoad)
        {
            Console.WriteLine(Invariant($"Navigating to cached games page URL: {SiteConstants.CachedGamePage}..."));
            driver.Url = SiteConstants.CachedGamePage;

            WaitForLoad(driver, waitType);
        }
    }
}
