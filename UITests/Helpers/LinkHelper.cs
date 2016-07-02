using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UITests.Util;

namespace UITests.Helpers
{
    public static class LinkHelper
    {
        public static void AssertExternalLink(IWebDriver driver, string linkId, string expectedTitle)
        {
            var originalWindowHandle = driver.CurrentWindowHandle;
            var originalWindowHandles = driver.WindowHandles.ToArray();

            Console.WriteLine($"Clicking '{linkId}'...");
            driver.FindElement(By.Id(linkId)).Click();

            Console.WriteLine("Waiting for the new window to pop...");
            driver.WaitUntil(d => d.WindowHandles.Count == (originalWindowHandles.Length + 1), "Could not verify new window/tab opening", TimeSpan.FromSeconds(10));

            Console.WriteLine("Switching to the new window...");
            driver.SwitchTo().Window(driver.WindowHandles.Except(originalWindowHandles).First());

            Console.WriteLine($"Waiting for new window to contain expected title {expectedTitle}...");
            driver.WaitUntil(ExpectedConditions.TitleContains(expectedTitle), $"Could not verify expected title {expectedTitle}", TimeSpan.FromSeconds(20));

            Console.WriteLine("Closing the new window...");
            driver.Close();

            Console.WriteLine("Switching back to origin window...");
            driver.SwitchTo().Window(originalWindowHandle);
        }
    }
}
