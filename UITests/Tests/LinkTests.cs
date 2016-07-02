﻿using System;
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
                driver.WaitUntil(ExpectedConditions.TextToBePresentInElementLocated(By.Id(SiteConstants.ExternalPageTitleHeaderId), expectedTitle),
                    $"Could not verify external page title: {expectedTitle}");

                Console.WriteLine("Dismissing external page modal dialog...");
                //we can't click the close button directly because it's covered in a bootstrap overlay
                //clicking the overlay isn't straightforward either (it fails due to the iframe) 
                //The easiest way to dismiss the dialog is to emulate an ESC key press
                driver.FindElement(By.Id(SiteConstants.ExternalModalId)).SendKeys(Keys.Escape);
            });
        }

        private static void AssertUnconditionalLinks(IWebDriver driver, bool mobile)
        {
            Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.ContactAnchorId)).Displayed, "Expected contact link to be visible");

            AssertPageLink(driver, SiteConstants.PrivacyAnchorId, SiteConstants.PrivacyPolicyTitle);
            AssertPageLink(driver, SiteConstants.FaqAnchorId, SiteConstants.FaqTitle);

            LinkHelper.AssertExternalLink(driver, mobile ? SiteConstants.MobileFooterFacebookLinkId : SiteConstants.FooterFacebookLinkId, "HowLongToBeatSteam");
            LinkHelper.AssertExternalLink(driver, mobile ? SiteConstants.MobileFooterTwitterLinkId :  SiteConstants.FooterTwitterLinkId, "hltbsteam");
            LinkHelper.AssertExternalLink(driver, mobile ? SiteConstants.MobileFooterGooglePlusLinkId : SiteConstants.FooterGooglePlusLinkId, "HowLongToBeatSteam");
            LinkHelper.AssertExternalLink(driver, mobile ? SiteConstants.MobileFooterSteamGroupLinkId : SiteConstants.FooterSteamGroupLinkId, "HLTBS");

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
                AssertUnconditionalLinks(driver, false);
            });
        }

        [TestMethod]
        public void TestMobileLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                LinkHelper.AssertExternalLink(driver, SiteConstants.CachedGamesPanelId, "HowLongToBeatSteam");
                AssertUnconditionalLinks(driver, true);
            }, Browsers.OptimusL70Chrome);
        }

        [TestMethod]
        public void TestCachedGamesLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);

                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.CachedGamesPanelId)).Displayed, "Expected cached games link to be hidden in cached games page");
                AssertUnconditionalLinks(driver, false);
            });
        }
    }
}
