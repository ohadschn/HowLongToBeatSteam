using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class LinkTests
    {
        private static void AssertPageLink(IWebDriver driver, string pageAnchorId, string expectedTitle)
        {
            DialogHelper.TestDialog(driver, pageAnchorId, SiteConstants.ExternalModalId, () =>
            {
                driver.WaitUntil(ExpectedConditions.TextToBePresentInElementLocated(By.Id(SiteConstants.ExternalPageTitleHeaderId), expectedTitle));

                Console.WriteLine("Dismissing external page modal dialog...");
                //we can't click the close button directly because it's covered in a bootstrap overlay
                //clicking the overlay isn't straightforward either (it fails due to the iframe) 
                //The easiest way to dismiss the dialog is to emulate an ESC key press
                driver.FindElement(By.Id(SiteConstants.ExternalModalId)).SendKeys(Keys.Escape);
            });
        }

        private static void AssertUnconditionalLinks(IWebDriver driver)
        {
            Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.ContactAnchorId)).Displayed, "Expected contact link to be visible");

            AssertPageLink(driver, SiteConstants.PrivacyAnchorId, SiteConstants.PrivacyPolicyTitle);
            AssertPageLink(driver, SiteConstants.FaqAnchorId, SiteConstants.FaqTitle);

            LinkHelper.AssertExternalLink(driver, SiteConstants.FacebookLinkId, "HowLongToBeatSteam");
            LinkHelper.AssertExternalLink(driver, SiteConstants.TwitterLinkId, "hltbsteam");
            LinkHelper.AssertExternalLink(driver, SiteConstants.GooglePlusLinkId, "HowLongToBeatSteam");
            LinkHelper.AssertExternalLink(driver, SiteConstants.SteamGroupLinkId, "HLTBS");

            LinkHelper.AssertExternalLink(driver, SiteConstants.SteamAnchorId, "Steam");
            LinkHelper.AssertExternalLink(driver, SiteConstants.HltbAnchorId, "HowLongToBeat");
            LinkHelper.AssertExternalLink(driver, SiteConstants.OhadSoftAnchorId, "OhadSoft");
        }

        [TestMethod]
        public void TestLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                LinkHelper.AssertExternalLink(driver, SiteConstants.CachedGamesPanelId, "HowLongToBeatSteam");
                AssertUnconditionalLinks(driver);
            });
        }

        [TestMethod]
        public void TestCachedGamesLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);

                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.CachedGamesPanelId)).Displayed, "Expected cached games link to be hidden in cached games page");
                AssertUnconditionalLinks(driver);
            });
        }
    }
}
