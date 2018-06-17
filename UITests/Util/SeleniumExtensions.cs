﻿using System;
using System.Drawing;
using JetBrains.Annotations;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace UITests.Util
{
    [Flags]
    public enum Browsers
    {
        Firefox = 1,
        Chrome = 2,
        InternetExplorer = 4,
        // ReSharper disable once InconsistentNaming
        iPhone4Chrome = 8,
        OptimusL70Chrome = 16, //384 X 640
        Nexus7Chrome = 32, // 600 X 960
        // ReSharper disable once InconsistentNaming
        iPadMiniChrome = 64 //768 X 1024
    }

    public static class SeleniumExtensions
    {
        public const Browsers DesktopBrowsers = Browsers.Firefox | Browsers.Chrome | Browsers.InternetExplorer;

        private static IWebDriver GetMobileChromeDriver(string deviceName)
        {
            var options = new ChromeOptions();
            options.EnableMobileEmulation(deviceName);
            return new ChromeDriver(options);
        }

        public static void ExecuteOnMultipleBrowsers([NotNull] Action<IWebDriver> test, Browsers browsers = DesktopBrowsers)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));

            if (browsers.HasFlag(Browsers.Firefox))
            {
                Console.WriteLine("Executing test on FireFox...");
                using (var driver = new FirefoxDriver()) { test(driver); }
            }

            if (browsers.HasFlag(Browsers.Chrome))
            {
                Console.WriteLine("Executing test on Chrome...");
                using (var driver = new ChromeDriver()) { test(driver); }
            }

            if (browsers.HasFlag(Browsers.InternetExplorer))
            {
                Console.WriteLine("Executing test on Internet Explorer...");
                using (var driver = new InternetExplorerDriver()) { test(driver); }
            }

            if (browsers.HasFlag(Browsers.iPhone4Chrome))
            {
                Console.WriteLine("Executing test on Apple iPhone 8 Chrome...");
                using (var driver = GetMobileChromeDriver("iPhone X")) { test(driver); }
            }

            if (browsers.HasFlag(Browsers.OptimusL70Chrome))
            {
                Console.WriteLine("Executing test on LG Optimus L70 Chrome...");
                using (var driver = GetMobileChromeDriver("LG Optimus L70")) { test(driver); }    
            }

            if (browsers.HasFlag(Browsers.Nexus7Chrome))
            {
                Console.WriteLine("Executing test on Google Nexus 7 Chrome...");
                using (var driver = GetMobileChromeDriver("Nexus 7")) { test(driver); }
            }

            if (browsers.HasFlag(Browsers.iPadMiniChrome))
            {
                Console.WriteLine("Executing test on Apple iPad Mini Chrome...");
                using (var driver = GetMobileChromeDriver("Apple iPad Mini")) { test(driver); }
            }
        }

        public static TResult WaitUntil<TResult>([NotNull] this IWebDriver driver, [NotNull] Func<IWebDriver, TResult> condition, [NotNull] string message)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            return WaitUntil(driver, condition, message, TimeSpan.FromSeconds(10));
        }

        public static TResult WaitUntil<TResult>([NotNull] this IWebDriver driver, [NotNull] Func<IWebDriver, TResult> condition, [NotNull] string message, TimeSpan timeout)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            var wait = new WebDriverWait(driver, timeout) {Message = message};
            wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
            return wait.Until(condition);
        }

        public static IWebElement WaitUntilElementIsAvailable([NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] string message)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return WaitUntilElementCondition(driver, by, e => true, message);
        }

        public static IWebElement WaitUntilElementIsAvailable([NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] string message, TimeSpan timeout)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return WaitUntilElementCondition(driver, by, e => true, message, timeout);
        }

        public static IWebElement WaitUntilElementIsVisible([NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] string message)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return WaitUntilElementCondition(driver, by, e => e.Displayed, message);
        }

        public static IWebElement WaitUntilElementIsVisible([NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] string message, TimeSpan timeout)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return WaitUntilElementCondition(driver, by, e => e.Displayed, message, timeout);
        }

        public static IWebElement WaitUntilElementCondition([NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] Predicate<IWebElement> condition, [NotNull] string message)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            return  WaitUntilElementCondition(driver, by, condition, message, TimeSpan.FromSeconds(10));
        }

        public static IWebElement WaitUntilElementCondition(
            [NotNull] this IWebDriver driver, [NotNull] By by, [NotNull] Predicate<IWebElement> condition, [NotNull] string message, TimeSpan timeout)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            return driver.WaitUntil(d =>
            {
                var element = driver.FindElement(by);
                return element == null || !condition(element) ? null : element;
            }, message, timeout);
        }

        public static IWebElement WaitUntilElementIsStationary([NotNull] this IWebDriver driver, [NotNull] By by, int desiredStationarySamples, [NotNull] string message)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return WaitUntilElementIsStationary(driver, by, desiredStationarySamples, message, TimeSpan.FromSeconds(10));
        }

        public static IWebElement WaitUntilElementIsStationary(
            [NotNull] this IWebDriver driver, [NotNull] By by, int desiredStationarySamples, [NotNull] string message, TimeSpan timeout)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            var prevLocation = new Point(Int32.MinValue, Int32.MinValue);
            int stationaryCount = 0;
            return driver.WaitUntilElementCondition(by, element =>
            {
                if (element.Location == prevLocation)
                {
                    stationaryCount++;
                    return stationaryCount == desiredStationarySamples;
                }

                prevLocation = element.Location;
                stationaryCount = 0;
                return false;
            }, message);
        }

        public static void SetText([NotNull] this IWebElement element, [NotNull] string text)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (text == null) throw new ArgumentNullException(nameof(text));

            element.Clear();
            element.SendKeys(text);
        }

        public static void SelectValue([NotNull] this IWebDriver driver, [NotNull] By by, string value)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            new SelectElement(driver.FindElement(by)).SelectByValue(value);
        }

        public static string GetOptionsValue([NotNull] this IWebDriver driver, [NotNull] By by)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (by == null) throw new ArgumentNullException(nameof(by));

            return new SelectElement(driver.FindElement(by)).SelectedOption.Text;
        }

        public static string GetHiddenText([NotNull] this IWebElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            return element.GetAttribute("textContent")?.Trim();
        }

        public static void Hover([NotNull] this IWebDriver driver, By by)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            Hover(driver, driver.FindElement(by));
        }

        public static void Hover([NotNull] this IWebDriver driver, IWebElement element)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            //https://github.com/SeleniumHQ/selenium/issues/4148
            ((IJavaScriptExecutor) driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            new Actions(driver).MoveToElement(element).Perform();
        }
    }
}