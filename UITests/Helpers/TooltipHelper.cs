using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using UITests.Constants;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Helpers
{
    public static class TooltipHelper
    {
        public static void AssertTooltip(IWebDriver driver, By by, string expectedTooltip, bool mobile = false)
        {
            Console.WriteLine(Invariant($"Hovering over '{by}' and asserting a tooltip containing '{expectedTooltip}'..."));
            AssertTooltip(driver, driver.FindElement(by), expectedTooltip, mobile);
        }

        public static void AssertTooltip(IWebDriver driver, IWebElement element, string expectedTooltip, bool mobile = false)
        {
            var verificationFailureMessage = Invariant($"Could not verify tooltip '{expectedTooltip}'");
            driver.WaitUntil(d =>
            {
                if (mobile)
                {
                    new Actions(driver).ClickAndHold(element).Perform();
                }
                else
                {
                    driver.Hover(element);
                }

                bool tooltipFound = true;
                try
                {
                    driver.WaitUntil(dr => GetToolTipText(driver)?.Contains(expectedTooltip) ?? false, verificationFailureMessage, TimeSpan.FromSeconds(3));
                }
                catch (WebDriverTimeoutException)
                {
                    tooltipFound = false;
                }
                finally
                {
                    if (mobile)
                    {
                        new Actions(driver).Release(element).Perform();
                    }
                }

                if (tooltipFound)
                {
                    return true;
                }

                if (!mobile)
                {
                    new Actions(driver).MoveByOffset(999, 999).Perform();
                }

                return false;
            }, verificationFailureMessage);
        }

        public static string GetToolTipText(IWebDriver driver)
        {
            return driver.FindElement(By.ClassName(SiteConstants.TooltipClass))?.Text;
        }
    }
}
