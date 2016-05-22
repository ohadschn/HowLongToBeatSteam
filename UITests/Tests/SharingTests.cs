using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class SharingTests
    {
        private const string FacebookShareTitle = "Facebook";
        private const string TwitterShareTitle = "Twitter";
        private const string RedditShareTitle = "reddit";
        private const string GooglePlusShareTitle = "Google+";

        [TestMethod]
        public void TestShareLinks()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                TestUtil.AssertExternalLink(driver, SiteConstants.FacebookShareAnchorId, FacebookShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.TwitterShareAnchorId, TwitterShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.RedditShareAnchorId, RedditShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.GplusShareAnchorId, GooglePlusShareTitle);

                SiteHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);

                TestUtil.AssertExternalLink(driver, SiteConstants.SurvivalFacebookShareAnchorId, FacebookShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.SurvivalTwitterShareAnchorId, TwitterShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.SurvivalRedditShareAnchorId, RedditShareTitle);
                TestUtil.AssertExternalLink(driver, SiteConstants.SurvivalGplusShareAnchorId, GooglePlusShareTitle);
            });
        }

        [TestMethod]
        public void TestNoShareLinksOnCachePage()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.GoToCachedGamesPage(driver);
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SocialSharingHeaderId)).Displayed, "Expected hidden social sharing section in cached games page");

                SiteHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SurvivalSocialSharingHeaderId)).Displayed, "Expected hidden survival social sharing in cached games page");
            });
        }
    }
}
