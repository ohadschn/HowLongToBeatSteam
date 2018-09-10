using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;
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
        private const string GooglePlusShareTitle = "Google";

        [TestMethod]
        public void TestShareLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                var isInternetExplorer = driver is InternetExplorerDriver;
                
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                LinkHelper.AssertExternalLink(driver, SiteConstants.FacebookShareAnchorId, FacebookShareTitle, isInternetExplorer);
                LinkHelper.AssertExternalLink(driver, SiteConstants.TwitterShareAnchorId, TwitterShareTitle);
                LinkHelper.AssertExternalLink(driver, SiteConstants.RedditShareAnchorId, RedditShareTitle);
                LinkHelper.AssertExternalLink(driver, SiteConstants.GplusShareAnchorId, GooglePlusShareTitle);

                SurvivalHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);

                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalFacebookShareAnchorId, FacebookShareTitle, isInternetExplorer);
                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalTwitterShareAnchorId, TwitterShareTitle);
                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalRedditShareAnchorId, RedditShareTitle);
                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalGplusShareAnchorId, GooglePlusShareTitle);
            });
        }

        [TestMethod]
        public void TestNoShareLinksOnCachePage()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SocialSharingHeaderId)).Displayed, "Expected hidden social sharing section in cached games page");

                SurvivalHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);
                Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SurvivalSocialSharingHeaderId)).Displayed, "Expected hidden survival social sharing section in cached games page");
            });
        }
    }
}
