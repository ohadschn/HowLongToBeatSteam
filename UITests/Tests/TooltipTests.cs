using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class TooltipTests
    {
        [TestMethod]
        public void TestNotifications()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                driver.Url = SiteConstants.LocalDeploymentUrl;
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.ProfileIdTooltip), "steamcommunity.com/id/[ID]");

                SignInHelper.SignInWithId(driver, UserConstants.SampleSteamId);
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.PlaytimeTooltip), "As recorded by Steam");
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.ExcludedGamesTooltip), "non-game apps are excluded");
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.SteamPlaytimeTitle), "Current Steam playtime");
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.MainPlaytimeTitle), "main objectives");
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.ExtrasPlaytimeTitle), "extra objectives");
                TooltipHelper.AssertTooltip(driver, By.Id(SiteConstants.CompletionistPlaytimeTitle), "all possible objectives");
            });
        }
    }
}
