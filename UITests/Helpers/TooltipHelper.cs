using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public static class TooltipHelper
    {
        public static void AssertTooltip(IWebDriver driver, By by, string expectedTooltip, bool mobile = false)
        {
            driver.Hover(by);
            AssertTooltip(driver, driver.FindElement(by), expectedTooltip, mobile);
        }

        public static void AssertTooltip(IWebDriver driver, IWebElement element, string expectedTooltip, bool mobile = false)
        {
            if (mobile)
            {
                new Actions(driver).ClickAndHold(element).Perform();
            }
            else
            {
                driver.Hover(element);
            }

            driver.WaitUntil(d => GetToolTipText(driver)?.Contains(expectedTooltip) ?? false, $"Could not verify tooltip '{expectedTooltip}'");

            if (mobile)
            {
                new Actions(driver).Release(element).Perform();
            }
            else
            {
                new Actions(driver).MoveByOffset(999, 999).Perform();
            }
        }

        public static string GetToolTipText(IWebDriver driver)
        {
            return driver.FindElement(By.ClassName(SiteConstants.TooltipClass))?.Text;
        }
    }
}
