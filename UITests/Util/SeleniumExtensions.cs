using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Util;
using JetBrains.Annotations;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.UI;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;

namespace UITests.Util
{
    [Flags]
    public enum Browsers
    {
        Firefox = 1,
        Chrome = 2,
        InternetExplorer = 4,
        Edge = 8,
        Safari = 16,
        // ReSharper disable once InconsistentNaming
        IPhone = 32,
        AndroidPhone = 64,
        // ReSharper disable once InconsistentNaming
        IPad = 128,
        AndroidTablet = 256
    }

    public static class SeleniumExtensions
    {
        public const Browsers DesktopBrowsers = Browsers.Firefox | Browsers.Chrome | Browsers.InternetExplorer | Browsers.Edge | Browsers.Safari;
        public const Browsers MobileBrowsers = Browsers.IPhone | Browsers.AndroidPhone;
        public const Browsers TabletBrowsers = Browsers.IPad | Browsers.AndroidTablet;

        private const string BrowserstackTrue = "true";

        private static readonly string BrowserStackUser = SiteUtil.GetOptionalValueFromConfig("BrowserStackUser", null);
        private static readonly string BrowserStackKey = String.IsNullOrWhiteSpace(BrowserStackUser)
            ? null
            : SiteUtil.GetMandatoryCustomConnectionStringFromConfig("BrowserStackKey");

        private static IEnumerable<IWebDriver> GetDrivers(Browsers browsers)
        {
            return BrowserStackKey == null ? GetLocalDrivers(browsers) : GetBrowserStackDrivers(browsers);
        }

        private static void AddGlobalCapability(this DriverOptions options, string name, object capabilityValue)
        {
            switch (options)
            {
                case ChromeOptions chromeOptions:
                    chromeOptions.AddAdditionalCapability(name, capabilityValue, true);
                    break;
                case FirefoxOptions firefoxOptions:
                    firefoxOptions.AddAdditionalCapability(name, capabilityValue, true);
                    break;
                case InternetExplorerOptions internetExplorerOptions:
                    internetExplorerOptions.AddAdditionalCapability(name, capabilityValue, true);
                    break;
                default:
                    options.AddAdditionalCapability(name, capabilityValue);
                    break;
            }
        }

        [SuppressMessage("Sonar.CodeSmell", "S4144:MethodsShouldNotHaveIdenticalImplementations", Justification = "Different class")]
        private static void PopulateBrowserStackCapabilities(DriverOptions options)
        {
            options.AddGlobalCapability("browserstack.user", BrowserStackUser);
            options.AddGlobalCapability("browserstack.key", BrowserStackKey);
            options.AddGlobalCapability("browserstack.ie.enablePopups", BrowserstackTrue);
            options.AddGlobalCapability("browserstack.safari.enablePopups", BrowserstackTrue);
            options.AddGlobalCapability("browserstack.console", "verbose");
            options.AddGlobalCapability("browserstack.networkLogs", BrowserstackTrue);
        }

        private static IEnumerable<IWebDriver> GetLocalDrivers(Browsers browsers)
        {
            return GetDrivers(browsers, GetLocalDriver);
        }

        private static IWebDriver GetLocalDriver(Browsers browser)
        {
            switch (browser)
            {
                case Browsers.Firefox: return new FirefoxDriver();
                case Browsers.Chrome: return new ChromeDriver();
                case Browsers.InternetExplorer: return new InternetExplorerDriver();
                case Browsers.Edge: return new EdgeDriver();
                case Browsers.Safari: return null;
                case Browsers.IPhone: return GetMobileChromeDriver("iPhone 6/7/8");
                case Browsers.AndroidPhone: return GetMobileChromeDriver("LG Optimus L70");
                case Browsers.IPad: return GetMobileChromeDriver("iPad");
                case Browsers.AndroidTablet: return GetMobileChromeDriver("Nexus 7");
                default:
                    throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
            }
        }

        private static IWebDriver GetMobileChromeDriver(string deviceName)
        {
            var options = new ChromeOptions();
            options.EnableMobileEmulation(deviceName);
            return new ChromeDriver(options);
        }

        private static IEnumerable<IWebDriver> GetBrowserStackDrivers(Browsers browsers)
        {
            return GetDrivers(browsers, GetBrowserStackDriver);
        }

        private static IWebDriver GetBrowserStackDriver(Browsers browser)
        {
            const string windows = "Windows";
            const string osX = "OS X";
            const string fullHd = "1920x1080";

            DriverOptions options;
            switch (browser)
            {
                case Browsers.Firefox:
                {
                    options = new FirefoxOptions();
                    SetDesktopOptions(options, "Firefox", windows, fullHd);
                    break;
                }
                case Browsers.Chrome:
                {
                    options = new ChromeOptions();
                    SetDesktopOptions(options, "Chrome", windows, fullHd);
                    break;
                }
                case Browsers.InternetExplorer:
                {
                    options = new InternetExplorerOptions();
                    SetDesktopOptions(options, "IE", windows, fullHd);
                    break;
                }
                case Browsers.Edge:
                {
                    options = new EdgeOptions();
                    SetDesktopOptions(options, "Edge", windows, fullHd);
                    break;
                }
                case Browsers.Safari:
                {
                    options = new SafariOptions();
                    SetDesktopOptions(options, "Safari", osX, fullHd);
                    break;
                }
                case Browsers.IPhone:
                {
                    options = new SafariOptions();
                    SetDeviceOptions(options, "iPhone 8", "iPhone");
                    break;
                }
                case Browsers.AndroidPhone:
                {
                    options = new ChromeOptions();
                    SetDeviceOptions(options, "Google Pixel 2", "android");
                    break;
                }
                case Browsers.IPad:
                {
                    options = new SafariOptions();
                    SetDeviceOptions(options, "iPad 6th", "iPad");
                    break;
                }
                case Browsers.AndroidTablet:
                {
                    options = new ChromeOptions();
                    SetDeviceOptions(options, "Samsung Galaxy Note 4", "android");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
            }

            PopulateBrowserStackCapabilities(options);
            var capabilities = options.ToCapabilities();
            return new RemoteWebDriver(new Uri("http://hub-cloud.browserstack.com/wd/hub/"), capabilities);
        }

        private static void SetDeviceOptions(DriverOptions options, string device, string browser)
        {
            options.AddGlobalCapability("device", device);
            options.AddGlobalCapability("realMobile", BrowserstackTrue);
            options.AddGlobalCapability("browserName", browser);
        }

        private static void SetDesktopOptions(DriverOptions options, string browser, string os, string osVersion)
        {
            options.AddGlobalCapability("browser", browser);
            options.AddGlobalCapability("os", os);
            options.AddGlobalCapability("resolution", osVersion);
        }

        private static IEnumerable<IWebDriver> GetDrivers(Browsers browsers, Func<Browsers, IWebDriver> factory)
        {
            return ((Browsers[]) Enum.GetValues(typeof(Browsers))).Where(browser => browsers.HasFlag(browser)).Select(factory).Where(d => d != null);
        }

        public static void ExecuteOnMultipleBrowsers([NotNull] Action<IWebDriver> test, Browsers browsers = DesktopBrowsers)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));

            GetDrivers(browsers).ForEachAsync(BrowserStackKey == null ? 1 : 5, driver =>
            {
                return Task.Run(() =>
                {
                    using (driver)
                    {
                        ExecuteWithRetries(test, driver);
                    }
                });
            } , failEarly: false).GetAwaiter().GetResult();

        }

        private static void ExecuteWithRetries(Action<IWebDriver> test, IWebDriver driver, int retries = 2)
        {
            int maxAttempts = retries + 1;
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    driver.Manage().Window.Maximize();
                    test(driver);
                    return;
                }
                catch (WebDriverException e)
                {
                    Console.WriteLine("WARNING: Attempt {0}/{1} with WebDriver '{2}' failed: {3}", attempt, maxAttempts, driver, e);
                    driver.TakeScreenshot();

                    if (attempt == maxAttempts)
                    {
                        Console.WriteLine("ERROR: All retry attempts exhausted, failing test");
                        throw;
                    }
                }     
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Test code")]
        public static bool TakeScreenshot(this IWebDriver driver)
        {
            Console.WriteLine("Attempting to take screenshot using WebDriver '{0}'...", driver);

            var screenshotCapableDriver = driver as ITakesScreenshot;
            if (screenshotCapableDriver == null)
            {
                Console.WriteLine("WARNING: WebDriver '{0}' not capable of taking screenshots", driver);
                return false;
            }

            string screenshotPath;
            try
            {
                screenshotPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
                screenshotCapableDriver.GetScreenshot().SaveAsFile(screenshotPath);
            }
            catch (Exception exception)
            {
                Console.WriteLine("WARNING: Could not take/save screenshot - {0}", exception);
                return false;
            }

            Console.WriteLine("Saved screenshot: {0}", screenshotPath);
            return true;
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
            wait.IgnoreExceptionTypes(typeof(WebDriverException));
            return wait.Until(condition);
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
            }, message, timeout);
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

        public static void Hover([NotNull] this IWebDriver driver, IWebElement element)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            ScrollIntoView(driver, element); //https://github.com/SeleniumHQ/selenium/issues/4148
            new Actions(driver).MoveToElement(element).Perform();
        }

        public static void ScrollIntoView([NotNull] this IWebDriver driver, IWebElement element)
        {
            ((IJavaScriptExecutor) driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        public static void WaitForPageTitle([NotNull] IWebDriver driver, [NotNull] string expectedTitleSubstring)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (expectedTitleSubstring == null) throw new ArgumentNullException(nameof(expectedTitleSubstring));

            Console.WriteLine(FormattableString.Invariant($"Waiting for page title to contain '{expectedTitleSubstring}'..."));
            
            driver.WaitUntil(
                ExpectedConditions.TitleContains(expectedTitleSubstring), 
                FormattableString.Invariant($"Could not verify expected page title: {expectedTitleSubstring}"), 
                TimeSpan.FromSeconds(20));
        }

        public static void ClickById([NotNull] IWebDriver driver, [NotNull] string linkId)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (linkId == null) throw new ArgumentNullException(nameof(linkId));

            Console.WriteLine(FormattableString.Invariant($"Clicking '{linkId}'..."));
            driver.FindElement(By.Id(linkId)).Click();
        }

        public static bool IsMobile(this IWebDriver driver)
        {
            return driver is ChromeDriver chromeDriver && chromeDriver.Capabilities["mobileEmulationEnabled"] as bool? == true;
        }
    }
}