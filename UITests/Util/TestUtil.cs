using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;

namespace UITests.Util
{
    public static class TestUtil
    {
        public static void ExecuteOnAllBrowsers(Action<IWebDriver> test)
        {
            Console.WriteLine("Executing test on FireFox...");
            using (var driver = new FirefoxDriver()) { test(driver); }

            Console.WriteLine("Executing test on Chrome...");
            using (var driver = new ChromeDriver()) { test(driver); }

            Console.WriteLine("Executing test on Internet Explorer...");
            using (var driver = new InternetExplorerDriver())   { test(driver); }
        }

        public static TResult WaitUntil<TResult>(this IWebDriver driver, Func<IWebDriver, TResult> condition)
        {
            return WaitUntil(driver, condition, TimeSpan.FromSeconds(10));
        }
        public static TResult WaitUntil<TResult>(this IWebDriver driver, Func<IWebDriver, TResult> condition, TimeSpan timeout)
        {
            return new WebDriverWait(driver, timeout).Until(condition);
        }

        public static IWebElement WaitUntilElementIsAvailable(this IWebDriver driver, By by)
        {
            return WaitUntilElementCondition(driver, @by, e => true);
        }

        public static IWebElement WaitUntilElementIsAvailable(this IWebDriver driver, By by, TimeSpan timeout)
        {
            return WaitUntilElementCondition(driver, @by, e => true, timeout);
        }

        public static IWebElement WaitUntilElementIsVisible(this IWebDriver driver, By by)
        {
            return WaitUntilElementCondition(driver, @by, e => e.Displayed);
        }

        public static IWebElement WaitUntilElementIsVisible(this IWebDriver driver, By by, TimeSpan timeout)
        {
            return WaitUntilElementCondition(driver, @by, e => e.Displayed, timeout);
        }

        public static IWebElement WaitUntilElementCondition(this IWebDriver driver, By by, Predicate<IWebElement> condition)
        {
           return  WaitUntilElementCondition(driver, @by, condition, TimeSpan.FromSeconds(10));
        }

        public static IWebElement WaitUntilElementCondition(this IWebDriver driver, By by, Predicate<IWebElement> condition, TimeSpan timeout)
        {
            return driver.WaitUntil(d =>
            {
                var element = driver.FindElement(@by);
                return element == null || !condition(element) ? null : element;
            }, timeout);
        }

        public static IWebElement WaitUntilElementIsStationary(this IWebDriver driver, By by, int desiredStationarySamples)
        {
            return WaitUntilElementIsStationary(driver, @by, desiredStationarySamples, TimeSpan.FromSeconds(10));
        }

        public static IWebElement WaitUntilElementIsStationary(this IWebDriver driver, By by, int desiredStationarySamples, TimeSpan timeout)
        {
            var prevLocation = new Point(Int32.MinValue, Int32.MinValue);
            int stationaryCount = 0;
            return driver.WaitUntilElementCondition(@by, element =>
            {
                if (element.Location == prevLocation)
                {
                    stationaryCount++;
                    return stationaryCount == desiredStationarySamples;
                }

                prevLocation = element.Location;
                stationaryCount = 0;
                return false;
            });
        }

        public static void SetText(this IWebElement element, string text)
        {
            element.Clear();
            element.SendKeys(text);
        }

        public static void SelectValue(this IWebDriver driver, By by, string value)
        {
            new SelectElement(driver.FindElement(by)).SelectByValue(value);
        }

        public static string GetOptionsValue(this IWebDriver driver, By by)
        {
            return new SelectElement(driver.FindElement(by)).SelectedOption.Text;
        }

        private static int GetUnitCount(string duration, string unit)
        {
            var unitMatch = Regex.Match(duration, @"(\d+) " + unit);
            return unitMatch.Success ? Int32.Parse(unitMatch.Groups[1].Value) : 0;
        }

        public static TimeSpan FreetextDurationToTimespan(string duration)
        {
            var hours = GetUnitCount(duration, "hour");
            var days = GetUnitCount(duration, "day");
            var weeks = GetUnitCount(duration, "week");
            var months = GetUnitCount(duration, "month");
            var years = GetUnitCount(duration, "year");

            return new TimeSpan(years * 365 + months * 30 + weeks * 7 + days, hours, 0, 0);
        }

        public static string GetHiddenText(this IWebElement element)
        {
            return element.GetAttribute("textContent")?.Trim();
        }

        public static DateTime ParseBrowserDate(IWebDriver driver, string date)
        {
            return DateTime.ParseExact(date, driver is InternetExplorerDriver ? "d/MM/yyyy" : "M/d/yyyy", CultureInfo.InvariantCulture);
        }

        public static void AssertExternalLink(IWebDriver driver, string linkId, string expectedTitle)
        {
            var originalWindowHandle = driver.CurrentWindowHandle;
            var originalWindowHandles = driver.WindowHandles;

            foreach (string handle in driver.WindowHandles)
            {
                Console.WriteLine(handle);
            }

            Console.WriteLine($"Clicking '{linkId}'...");
            driver.FindElement(By.Id(linkId)).Click();

            Console.WriteLine("Waiting for the new window to pop...");
            driver.WaitUntil(d => d.WindowHandles.Count == (originalWindowHandles.Count + 1), TimeSpan.FromSeconds(10));

            Console.WriteLine("Switching to the new window...");
            driver.SwitchTo().Window(driver.WindowHandles.Except(originalWindowHandles).First());

            Console.WriteLine($"Waiting for new window to contain expected title {expectedTitle}...");
            driver.WaitUntil(ExpectedConditions.TitleContains(expectedTitle), TimeSpan.FromSeconds(20));

            Console.WriteLine("Closing the new window...");
            driver.Close();

            Console.WriteLine("Switching back to origin window...");
            driver.SwitchTo().Window(originalWindowHandle);
        }

        public static string StringJoin<T>(this IEnumerable<T> enumerable)
        {
            return String.Join(", ", enumerable);
        }

        public static void AssertEqualSequences<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            ICollection<T> expectedCollection = expected as ICollection<T> ?? expected.ToArray();
            ICollection<T> actualCollection = actual as ICollection<T> ?? actual.ToArray();

            Assert.IsTrue(expectedCollection.SequenceEqual(actualCollection),
                $"Expected sequence : {expectedCollection.StringJoin()}; Actual sequence: {actualCollection.StringJoin()}");
        }

        public static void AssertEqualSets<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            ISet<T> expectedSet = expected as ISet<T> ?? new HashSet<T>(expected);
            ISet<T> actualSet = actual as ISet<T> ?? new HashSet<T>(actual);

            Assert.IsTrue(expectedSet.SetEquals(actualSet), $"Expected set: {expectedSet.StringJoin()}; Actual set: {actualSet.StringJoin()}");
        }

        public static void AssertDistinctSets<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            ISet<T> firstSet = first as ISet<T> ?? new HashSet<T>(first);
            ISet<T> secondSet = second as ISet<T> ?? new HashSet<T>(second);

            var intersection = firstSet.Intersect(secondSet).ToArray();
            Assert.AreEqual(0, intersection.Length, $"Non-empty intersection for set: {firstSet.StringJoin()} and set: {secondSet.StringJoin()} - {intersection.StringJoin()}");
        }
    }
}
