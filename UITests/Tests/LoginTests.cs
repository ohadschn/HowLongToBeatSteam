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
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId, WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.SamplePersonaName, UserConstants.SamplePersonaAvatarUUID);
            });
        }

        [TestMethod]
        public void TestSignInWithSteamId64()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteam64Id.ToString(), WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.SamplePersonaName, UserConstants.SamplePersonaAvatarUUID);
            });
        }

        [TestMethod]
        public void TestSignInThroughSteam()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                Console.WriteLine("Retrieving Steam password from environment variable...");
                string steamPassword = Environment.GetEnvironmentVariable("STEAM_PASSWORD");
                Assert.IsNotNull(steamPassword, "The STEAM_PASSWORD environment variable must be set for Steam sign-in test (make sure to restart VS after you set it)");

                SiteHelper.SignInThroughSteam(driver, UserConstants.HltbsUser, steamPassword, WaitType.PageLoad);
                AssertPersonaDetails(driver, UserConstants.HltbsPersonaName, UserConstants.HltbsPersonaAvatarUUID);
            });
        }

        [TestMethod]
        public void TestSignInWithNoGames()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleNoGamesUserId.ToString(), WaitType.None);
                driver.WaitUntilElementIsVisible(By.Id(SiteConstants.EmptyLibraryDivId));
            });
        }

        [TestMethod]
        public void TestSignInWithInvalidUsername()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, Guid.NewGuid().ToString(), WaitType.None);
                driver.WaitUntilElementIsVisible(By.Id(SiteConstants.LoginErrorDivId));
            });
        }

        [TestMethod]
        public void TestCachedGamesPage()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.GoToCachedGamesPage(driver, WaitType.PageLoad);
                AssertPersonaDetails(driver, String.Empty, UserConstants.SamplePersonaAvatarUUID);
            });
        }
    }
}
