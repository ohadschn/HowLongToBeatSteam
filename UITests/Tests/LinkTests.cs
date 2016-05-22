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
            Console.WriteLine("Clicking page link...");
            driver.WaitUntilElementCondition(By.Id(SiteConstants.ExternalModalBodyDivId), e => !e.Displayed);
            driver.FindElement(By.Id(pageAnchorId)).Click();

            Console.WriteLine("Waiting for expected page title...");
            driver.WaitUntil(ExpectedConditions.TextToBePresentInElementLocated(By.Id(SiteConstants.ExternalPageTitleHeaderId), expectedTitle));

            Console.WriteLine("Dismissing external page modal dialog...");
            //we can't click the close button directly because it's covered in a bootstrap overlay
            //clicking the overlay isn't straightforward either, it fails due to the iframe
            driver.WaitUntilElementIsStationary(By.Id(SiteConstants.ExternalModalId), 3).SendKeys(Keys.Escape);

            Console.WriteLine("Waiting until modal dialog is dismissed...");
            driver.WaitUntil(d => !d.FindElement(By.Id(SiteConstants.ExternalModalId)).Displayed);
        }

        private static void AssertUnconditionalLinks(IWebDriver driver)
        {
            Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.ContactAnchorId)).Displayed, "Expected contact link to be visible");

            AssertPageLink(driver, SiteConstants.PrivacyAnchorId, "Privacy Policy");
            AssertPageLink(driver, SiteConstants.FaqAnchorId, "Frequently Asked Questions");

            TestUtil.AssertExternalLink(driver, SiteConstants.FacebookLinkId, "HowLongToBeatSteam");
            TestUtil.AssertExternalLink(driver, SiteConstants.TwitterLinkId, "hltbsteam");
            TestUtil.AssertExternalLink(driver, SiteConstants.GooglePlusLinkId, "HowLongToBeatSteam");
            TestUtil.AssertExternalLink(driver, SiteConstants.SteamGroupLinkId, "HLTBS");

            TestUtil.AssertExternalLink(driver, SiteConstants.SteamAnchorId, "Steam");
            TestUtil.AssertExternalLink(driver, SiteConstants.HltbAnchorId, "HowLongToBeat");
            TestUtil.AssertExternalLink(driver, SiteConstants.OhadSoftAnchorId, "OhadSoft");
        }

        [TestMethod]
        public void TestLinks()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                TestUtil.AssertExternalLink(driver, SiteConstants.CachedGamesPanelId, "HowLongToBeatSteam");
                AssertUnconditionalLinks(driver);
            });
        }

        [TestMethod]
        public void TestCachedGamesLinks()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.GoToCachedGamesPage(driver);

                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.CachedGamesPanelId)).Displayed, "Expected cached games link to be hidden in cached games page");
                AssertUnconditionalLinks(driver);
            });
        }
    }
}
