using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class NotificationTests
    {
        [TestMethod]
        public void TestMissingHltbNotification()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.GoToCachedGamesPage(driver, WaitType.PageLoad);

                Console.WriteLine("Asserting the missing HLTB games alert is displayed...");
                driver.FindElement(By.Id(SiteConstants.MissingHltbGamesAlertDivID));
            });
        }

        [TestMethod]
        public void TestImputedValuesNotification()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId, WaitType.PageLoad);

                Console.WriteLine("Asserting the imputed values notification is displayed...");
                driver.FindElement(By.Id(SiteConstants.ImputedValuesNotificationDivId));
            });
        }
    }
}
