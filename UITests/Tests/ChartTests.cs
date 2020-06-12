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

        private static bool IsActive(IWebElement element)
        {
            return element.GetAttribute("class").Contains("active");
        }

        private static void AssertActive(IWebDriver driver, IWebElement element, string message)
        {
            driver.WaitUntil(d => IsActive(element), Invariant($"Expected element {element} to be active [{message}]"));
        }

        private static IWebElement TestSlicer(IWebDriver driver, string slicerId)
        {
            Console.WriteLine("Locating '{0}' playtime slicer...", slicerId);
            var slicer = driver.FindElement(By.Id(slicerId));
            driver.WaitUntil(d =>
            {
                Console.WriteLine("Clicking slicer {0}...", slicer);
                slicer.Click();
                return IsActive(slicer);
            }, "expected clicked slicer to be active");

            return slicer;
        }

        private static void TestSlicers(IWebDriver driver)
        {
            var currentPlaytimeSlicer = TestSlicer(driver, SiteConstants.CurrentPlaytimeSlicerId);
            Assert.IsTrue(driver.FindElements(By.ClassName(SiteConstants.NoDataIndicatorId)).All(e => e.Displayed), "Expected no data indication for current playtime");

            TestSlicer(driver, SiteConstants.RemainingPlaytimeSlicerId);
            Assert.IsFalse(currentPlaytimeSlicer.Enabled, "Expected current playtime slicer to be disabled when sliced for remaining playtime");
            AssertActive(driver, driver.FindElement(By.Id(SiteConstants.MainPlaytimeSlicerId)), "The selected Current slicer has been disabled, so Main should get auto-selected");

            TestSlicer(driver, SiteConstants.ExtrasPlaytimeSlicerId);
            TestSlicer(driver, SiteConstants.CompletionistPlaytimeSlicerId);
            
            TestSlicer(driver, SiteConstants.TotalPlaytimeSlicerId);
            Assert.IsTrue(currentPlaytimeSlicer.Enabled, "Expected current playtime slicer to be enabled when sliced for total playtime");
        }

        [TestMethod]
        public void TestRenderedCharts()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                TestRenderedCharts(driver);
                TestSlicers(driver);
            });
        }

        [TestMethod]
        public void TestMobileRenderedCharts()
        {
            SeleniumExtensions.ExecuteOnMultipleBrowsers(driver =>
            {
                SignInHelper.SignInWithId(driver);

                TestRenderedCharts(driver);
                TestSlicers(driver);
            }, SeleniumExtensions.MobileBrowsers);
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