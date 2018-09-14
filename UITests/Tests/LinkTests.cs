using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Tests
{
    [TestClass]
    public class LinkTests
    {
        private static void AssertPageLink(IWebDriver driver, string pageAnchorId, string expectedTitle, string textContainerId, string expectedText)
        {
            DialogHelper.TestDialog(driver, pageAnchorId, SiteConstants.ExternalModalId, () =>
            {
                driver.WaitUntil(ExpectedConditions.TextToBePresentInElementLocated(By.Id(SiteConstants.ExternalPageTitleHeaderId), expectedTitle),
                    Invariant($"Could not verify external page title: {expectedTitle}"));

                driver.SwitchTo().Frame(driver.FindElement(By.Id(SiteConstants.ExternalPageFrameId)));
                driver.WaitUntil(ExpectedConditions.TextToBePresentInElementLocated(By.Id(textContainerId), expectedText),
                    Invariant($"Could not verify external page text '{expectedText}'"));
                driver.SwitchTo().DefaultContent();

                Console.WriteLine("Dismissing external page modal dialog...");
                //we can't click the close button directly because it's covered in a bootstrap overlay
                //clicking the overlay isn't straightforward either (it fails due to the iframe) 
                //The easiest way to dismiss the dialog is to emulate an ESC key press
                driver.FindElement(By.Id(SiteConstants.ExternalModalId)).SendKeys(Keys.Escape);
            });
        }

        private static void AssertPageLinks(IWebDriver driver)
        {
            Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.ContactAnchorId)).Displayed, "Expected contact link to be visible");

            AssertPageLink(driver, SiteConstants.PrivacyAnchorId, SiteConstants.PrivacyTitle, SiteConstants.PrivacyContainerId, "For more information see on how Google");
            AssertPageLink(driver, SiteConstants.FaqAnchorId, SiteConstants.FaqTitle, SiteConstants.FaqContainerId, "It's fun to start sentences with");
        }

        private static string GetFooterLinkIdPrefix(bool mobile)
        {
            return mobile ? SiteConstants.MobileFooterPrefix : String.Empty;
        }

        private static void AssertExternalLinks(IWebDriver driver, bool mobile = false)
        {
            LinkHelper.AssertExternalLink(driver, GetFooterLinkIdPrefix(mobile) + SiteConstants.FooterFacebookLinkId, "HowLongToBeatSteam - Home | Facebook");
            LinkHelper.AssertExternalLink(driver, GetFooterLinkIdPrefix(mobile) + SiteConstants.FooterTwitterLinkId, "hltbsteam");
            LinkHelper.AssertExternalLink(driver, GetFooterLinkIdPrefix(mobile) + SiteConstants.FooterGithubLinkId, "ohadschn/HowLongToBeatSteam");
            LinkHelper.AssertExternalLink(driver, GetFooterLinkIdPrefix(mobile) + SiteConstants.FooterSteamGroupLinkId, "Group :: HLTBS");

            LinkHelper.AssertExternalLink(driver, SiteConstants.SteamAnchorId, "Welcome to Steam");
            LinkHelper.AssertExternalLink(driver, SiteConstants.HltbAnchorId, "HowLongToBeat.com");
            LinkHelper.AssertExternalLink(driver, SiteConstants.OhadSoftAnchorId, "OhadSoft");
        }

        private static void AssertInternalLinks(IWebDriver driver, bool mobile = false)
        {
            LinkHelper.AssertInternalLink(driver, SiteConstants.CachedGamesPanelId, "All cached");

            if (!mobile)
            {
                LinkHelper.AssertInternalLink(driver, SiteConstants.MissingGamesLinkId, "All missing");
            }
        }

        [TestMethod]
        public void TestPageLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                AssertPageLinks(driver);
            });
        }

        [TestMethod]
        public void TestProperLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                AssertInternalLinks(driver);
                AssertExternalLinks(driver);
            }, Browsers.Chrome | Browsers.Firefox); //IE behaves strangely and it doesn't really matter as these links are simple hrefs
        }

        [Ignore] // for some reason clicking links sometimes doesn't work, try reactivating on the next Chrome driver release
        [TestMethod]
        public void TestMobileLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                AssertPageLinks(driver);
                AssertInternalLinks(driver, mobile: true);
                AssertExternalLinks(driver, true);
            }, Browsers.OptimusL70Chrome);
        }

        [TestMethod]
        public void TestCachedGamesLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.CachedGamesPanelId)).Displayed, "Expected cached games pane to be hidden in cached games page");
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.MissingGamesLinkId)).Displayed, "Expected missing games link to be visible in cached games page");
            });
        }

        [TestMethod]
        public void TestMissingGamesLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToMissingGamesPage(driver);
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.CachedGamesPanelId)).Displayed, "Expected cached games pane to be visible in missing games page");
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.MissingGamesLinkId)).Displayed, "Expected missing games link to be hidden in missing games page");
            });
        }
    }
}