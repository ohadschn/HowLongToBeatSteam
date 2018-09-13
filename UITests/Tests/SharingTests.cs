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

        [TestMethod]
        public void TestShareLinks()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                var isInternetExplorer = driver is InternetExplorerDriver;
                
                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);

                LinkHelper.AssertExternalLink(driver, SiteConstants.FacebookShareAnchorId, FacebookShareTitle, newWindow:true, dismissAlertOnClose: isInternetExplorer);
                LinkHelper.AssertExternalLink(driver, SiteConstants.TwitterShareAnchorId, TwitterShareTitle, newWindow:true);
                LinkHelper.AssertExternalLink(driver, SiteConstants.RedditShareAnchorId, RedditShareTitle, newWindow:true);

                SurvivalHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);

                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalFacebookShareAnchorId, FacebookShareTitle, newWindow:true, dismissAlertOnClose: isInternetExplorer);
                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalTwitterShareAnchorId, TwitterShareTitle, newWindow:true);
                LinkHelper.AssertExternalLink(driver, SiteConstants.SurvivalRedditShareAnchorId, RedditShareTitle, newWindow:true);
            });
        }

        private static void AssertNoShareLinks(IWebDriver driver)
        {
            Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SocialSharingHeaderId)).Displayed, 
                "Expected hidden social sharing section in non-user games page");

            SurvivalHelper.CalculateSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 10, PlayStyle.Extras);

            Assert.IsFalse(driver.FindElement(By.Id(SiteConstants.SurvivalSocialSharingHeaderId)).Displayed,
                "Expected hidden survival social sharing section in non-user games page");
        }

        [TestMethod]
        public void TestNoShareLinksOnCachePage()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);
                AssertNoShareLinks(driver);
            });
        }

        [TestMethod]
        public void TestNoShareLinksOnMissingGamesPage()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToMissingGamesPage(driver);
                AssertNoShareLinks(driver);
            });
        }
    }
}