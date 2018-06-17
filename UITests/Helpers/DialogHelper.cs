using System;
using OpenQA.Selenium;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Helpers
{
    public static class DialogHelper
    {
        public static void TestDialog(IWebDriver driver, IWebElement dialogButton, string modalId, Action test, bool waitForDismissal = true)
        {
            Console.WriteLine("Opening dialog...");
            dialogButton.Click();

            Console.WriteLine(Invariant($"Waiting for dialog to load (by waiting for {modalId} to be stationary)..."));
            driver.WaitUntilElementIsStationary(By.Id(modalId), 3, Invariant($"Could not verify dialog {modalId} is stationary"));

            Console.WriteLine("Executing dialog test...");
            test();

            if (waitForDismissal)
            {
                Console.WriteLine(Invariant($"Waiting until dialog is dismissed (by waiting for {modalId} to be invisible)..."));
                driver.WaitUntil(d => !d.FindElement(By.Id(modalId)).Displayed, Invariant($"Could not verify dialog {modalId} is dismissed"));
            }
        }

        public static void TestDialog(IWebDriver driver, string dialogButtonId, string modalId, Action test, bool waitForDismissal = true)
        {
            TestDialog(driver, driver.FindElement(By.Id(dialogButtonId)), modalId, test, waitForDismissal);
        }
    }
}
