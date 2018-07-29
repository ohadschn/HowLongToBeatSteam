using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Tests
{
    [TestClass]
    public class ChartTests
    {
        private static void AssertAmChart(ISearchContext driver, string chartDivId)
        {
            Console.WriteLine(Invariant($"Looking for amCharts div in {chartDivId}..."));
            Assert.IsTrue(driver.FindElement(By.CssSelector(Invariant($"#{chartDivId} .amcharts-main-div"))).Displayed, "Expected amCharts div to be displayed");
        }

        private static void TestRenderedCharts(ISearchContext driver)
        {
            foreach (var amchartDiv in SiteConstants.AmchartDivs)
            {
                AssertAmChart(driver, amchartDiv);
            }
        }

        private static void AssertActive(IWebDriver driver, IWebElement element)
        {
            driver.WaitUntil(d => element.GetAttribute("class").Contains("active"), Invariant($"Expected element {element} to be active"));
        }

        private static void TestSlicers(IWebDriver driver)
        {
            Console.WriteLine("Clicking 'Current' playtime slicer...");
            var currentPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.CurrentPlaytimeSlicerId));
            currentPlaytimeSlicer.Click();
            currentPlaytimeSlicer.Click(); //sometimes the first click doesn't register
            AssertActive(driver, currentPlaytimeSlicer);
            Assert.IsTrue(driver.FindElements(By.ClassName(SiteConstants.NoDataIndicatorId)).All(e => e.Displayed), "Expected no data indication for current playtime");

            Console.WriteLine("Clicking 'Remaining' playtime slicer...");
            var remainingPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.RemainingPlaytimeSlicerId));
            remainingPlaytimeSlicer.Click();
            remainingPlaytimeSlicer.Click(); //sometimes the first click doesn't register
            Assert.IsFalse(currentPlaytimeSlicer.Enabled, "Expected current playtime slicer to be disabled when sliced for remaining playtime");
            AssertActive(driver, remainingPlaytimeSlicer);
            AssertActive(driver, driver.FindElement(By.Id(SiteConstants.MainPlaytimeSlicerId)));

            Console.WriteLine("Clicking 'Extras' playtime slicer...");
            var extrasPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.ExtrasPlaytimeSlicerId));
            extrasPlaytimeSlicer.Click();
            extrasPlaytimeSlicer.Click(); //sometimes the first click doesn't register
            AssertActive(driver, extrasPlaytimeSlicer);

            Console.WriteLine("Clicking 'Completionist' playtime slicer...");
            var completionistPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.CompletionistPlaytimeSlicerId));
            completionistPlaytimeSlicer.Click();
            completionistPlaytimeSlicer.Click(); //sometimes the first click doesn't register
            AssertActive(driver, completionistPlaytimeSlicer);
        }

        [TestMethod]
        public void TestRenderedCharts()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                TestRenderedCharts(driver);
                TestSlicers(driver);
            });
        }

        [TestMethod]
        public void TestMobileRenderedCharts()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver, UserConstants.HltbsUser);

                TestRenderedCharts(driver);
                TestSlicers(driver);
            }, Browsers.iPhoneXChrome);
        }

        [TestMethod]
        public void TestCachedRenderedCharts()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.GoToCachedGamesPage(driver);

                TestRenderedCharts(driver);
                TestSlicers(driver);
            });
        }
    }
}
