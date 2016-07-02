using OpenQA.Selenium;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public static class TooltipHelper
    {
        public static void AssertTooltip(IWebDriver driver, By by, string expectedTooltip)
        {
            driver.Hover(by);
            driver.WaitUntil(d => GetToolTipText(driver)?.Contains(expectedTooltip) ?? false, $"Could not verify tooltip '{expectedTooltip}' for element '{by}'");
        }

        public static string GetToolTipText(IWebDriver driver)
        {
            try
            {
                return driver.FindElement(By.ClassName(SiteConstants.TooltipClass))?.Text;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        }
    }
}
