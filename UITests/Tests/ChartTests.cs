using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class ChartTests
    {
        private static void AssertAmChart(IWebDriver driver, string chartDivId)
        {
            Console.WriteLine($"Looking for amCharts div in {chartDivId}...");
            Assert.IsTrue(driver.FindElement(By.CssSelector($"#{chartDivId} .amcharts-main-div")).Displayed, "Expected amCharts div to be displayed");
        }

        private static void TestRenderedCharts(IWebDriver driver)
        {
            foreach (var amchartDiv in SiteConstants.AmchartDivs)
            {
                AssertAmChart(driver, amchartDiv);
            }
        }

        private void AssertActive(IWebElement element)
        {
            Assert.IsTrue(element.GetAttribute("class").Contains("active"), $"Expected element {element} to be active");
        }

        private void TestSlicers(IWebDriver driver)
        {
            Console.WriteLine("Clicking 'Current' playtime slicer...");
            var currentPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.CurrentPlaytimeSlicerId));
            currentPlaytimeSlicer.Click();
            AssertActive(currentPlaytimeSlicer);
            Assert.IsTrue(driver.FindElements(By.ClassName(SiteConstants.NoDataIndicatorId)).All(e => e.Displayed), "Expected no data indication for current playtime");

            Console.WriteLine("Clicking 'Remaining' playtime slicer...");
            var remainingPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.RemainingPlaytimeSlicerId));
            remainingPlaytimeSlicer.Click();
            Assert.IsFalse(currentPlaytimeSlicer.Enabled, "Expected current playtime slicer to be disabled when sliced for remaining playtime");
            AssertActive(remainingPlaytimeSlicer);
            AssertActive(driver.FindElement(By.Id(SiteConstants.MainPlaytimeSlicerId)));

            Console.WriteLine("Clicking 'Extras' playtime slicer...");
            var extrasPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.ExtrasPlaytimeSlicerId));
            extrasPlaytimeSlicer.Click();
            AssertActive(extrasPlaytimeSlicer);

            Console.WriteLine("Clicking 'Completionist' playtime slicer...");
            var completionistPlaytimeSlicer = driver.FindElement(By.Id(SiteConstants.CompletionistPlaytimeSlicerId));
            completionistPlaytimeSlicer.Click();
            AssertActive(completionistPlaytimeSlicer);
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
            }, Browsers.iPhone4Chrome);
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
