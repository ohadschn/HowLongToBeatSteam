using System;
using System.Linq;
using JetBrains.Annotations;
using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using UITests.Util;

namespace UITests.Helpers
{
    public static class LinkHelper
    {
        public static void AssertInternalLink([NotNull] IWebDriver driver, [NotNull] string linkId, [NotNull] string expectedTitleSubstring)
        {
            SeleniumExtensions.ClickById(driver, linkId);
            SeleniumExtensions.WaitForPageTitle(driver, expectedTitleSubstring);
            SignInHelper.WaitForLoad(driver, WaitType.PageLoad);
        }

        public static void AssertExternalLink([NotNull] IWebDriver driver, [NotNull] string linkId, [NotNull] string expectedTitleSubstring, bool newWindow = false, bool dismissAlertOnClose = false)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (linkId == null) throw new ArgumentNullException(nameof(linkId));
            if (expectedTitleSubstring == null) throw new ArgumentNullException(nameof(expectedTitleSubstring));

            var originalWindowHandle = driver.CurrentWindowHandle;
            var originalWindowHandles = driver.WindowHandles.ToArray();

            SeleniumExtensions.ClickById(driver, linkId);

            if (newWindow)
            {
                
                Console.WriteLine("Waiting for the new window to pop...");
                driver.WaitUntil(d => d.WindowHandles.Count == originalWindowHandles.Length + 1, "Could not verify new window/tab opening", TimeSpan.FromSeconds(10));

                Console.WriteLine("Switching to the new window...");
                driver.SwitchTo().Window(driver.WindowHandles.Except(originalWindowHandles).First());
            }

            SeleniumExtensions.WaitForPageTitle(driver, expectedTitleSubstring);

            if (newWindow)
            {
                Console.WriteLine("Closing the new window...");
                driver.Close();

                if (dismissAlertOnClose)
                {
                    driver.WaitUntil(ExpectedConditions.AlertIsPresent(), "alert expected");
                    driver.SwitchTo().Alert().Accept();
                }

                Console.WriteLine("Switching back to origin window...");
                driver.SwitchTo().Window(originalWindowHandle);
            }
            else
            {
                Console.WriteLine("Navigating back to HLTBS...");
                driver.Navigate().Back();

                SignInHelper.WaitForLoad(driver, WaitType.PageLoad);
            }
        }
    }
}