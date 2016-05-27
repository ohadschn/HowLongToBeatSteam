using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class LoginTests
    {
        private static void AssertPersonaDetails(IWebDriver driver, string personaName, string avatarUUID)
        {
            Console.WriteLine("Locating and asserting persona name...");
            var personaNameSpan = driver.FindElement(By.Id(SiteConstants.PersonaNameSpanId));
            Assert.AreEqual(personaName, personaNameSpan.Text, "wrong persona name");

            Console.WriteLine("Locating and asserting persona avatar...");
            var personaAvatarImg = driver.FindElement(By.Id(SiteConstants.PersonaAvatarImgId));
            Assert.IsTrue(personaAvatarImg.GetAttribute("src")?.IndexOf(avatarUUID, StringComparison.OrdinalIgnoreCase) >= 0, "wrong persona avatar");
        }

        [TestMethod]
        public void TestDirectSignIn()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId, WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.SamplePersonaName, UserConstants.SamplePersonaAvatarUUID);

                Console.WriteLine("Asserting the imputed values notification is displayed...");
                driver.FindElement(By.Id(SiteConstants.ImputedValuesNotificationDivId));
            });
        }

        [TestMethod]
        public void TestSignInWithSteamId64()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteam64Id.ToString(), WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.SamplePersonaName, UserConstants.SamplePersonaAvatarUUID);

                Console.WriteLine("Asserting the imputed values notification is displayed...");
                driver.FindElement(By.Id(SiteConstants.ImputedValuesNotificationDivId));
            });
        }

        [TestMethod]
        public void TestSignInThroughSteam()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                Console.WriteLine("Retrieving Steam password from environment variable...");
                string steamPassword = Environment.GetEnvironmentVariable("STEAM_PASSWORD");
                Assert.IsNotNull(steamPassword, "The STEAM_PASSWORD environment variable must be set for Steam sign-in test (make sure to restart VS after you set it)");

                SignInHelper.SignInThroughSteam(driver, UserConstants.HltbsUser, steamPassword, WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.HltbsPersonaName, UserConstants.HltbsPersonaAvatarUUID);
            });
        }

        [TestMethod]
        public void TestSignInWithNoGames()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleNoGamesUserId.ToString(), WaitType.None);
                driver.WaitUntilElementIsVisible(By.Id(SiteConstants.EmptyLibraryDivId), "Could not locate empty library notification");
            });
        }

        [TestMethod]
        public void TestSignInWithInvalidUsername()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, Guid.NewGuid().ToString(), WaitType.None);
                driver.WaitUntilElementIsVisible(By.Id(SiteConstants.LoginErrorDivId), "Could not locate login error notification");
            });
        }

        [TestMethod]
        public void TestCachedGamesPage()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver, WaitType.PageLoad);
                AssertPersonaDetails(driver, String.Empty, UserConstants.SamplePersonaAvatarUUID);

                Console.WriteLine("Asserting the missing HLTB games alert is displayed...");
                driver.FindElement(By.Id(SiteConstants.MissingHltbGamesAlertDivID));
            });
        }
    }
}
