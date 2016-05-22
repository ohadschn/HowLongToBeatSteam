using System;
using System.Drawing;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;

namespace UITests.Util
{
    [Flags]
    public enum Browsers
    {
        Firefox,
        Chrome,
        InternetExplorer
        //TODO mobile
    }

    public static class SeleniumExtensions
    {
        public static void ExecuteOnMultipleBrowsers(Action<IWebDriver> test, Browsers browsers = Browsers.Firefox | Browsers.Chrome | Browsers.InternetExplorer)
        {
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

            //TODO mobile
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
            new SelectElement(driver.FindElement(@by)).SelectByValue(value);
        }

        public static string GetOptionsValue(this IWebDriver driver, By by)
        {
            return new SelectElement(driver.FindElement(@by)).SelectedOption.Text;
        }

        public static string GetHiddenText(this IWebElement element)
        {
            return element.GetAttribute("textContent")?.Trim();
        }
    }
}
