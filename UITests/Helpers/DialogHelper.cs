using System;
using JetBrains.Annotations;
using OpenQA.Selenium;
using UITests.Util;
using static System.FormattableString;

namespace UITests.Helpers
{
    public static class DialogHelper
    {
        public static void TestDialog(
            [NotNull] IWebDriver driver, 
            [NotNull] IWebElement dialogButton, 
            [NotNull] string modalId, 
            [NotNull] Action test, 
            bool waitForDismissal = true)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (dialogButton == null) throw new ArgumentNullException(nameof(dialogButton));
            if (modalId == null) throw new ArgumentNullException(nameof(modalId));
            if (test == null) throw new ArgumentNullException(nameof(test));

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

        public static void TestDialog(
            [NotNull] IWebDriver driver, 
            [NotNull] string dialogButtonId, 
            [NotNull] string modalId, 
            [NotNull] Action test, 
            bool waitForDismissal = true)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (dialogButtonId == null) throw new ArgumentNullException(nameof(dialogButtonId));
            if (modalId == null) throw new ArgumentNullException(nameof(modalId));
            if (test == null) throw new ArgumentNullException(nameof(test));

            Console.WriteLine("Testing dialog by ID: {0}", dialogButtonId);
            TestDialog(driver, driver.FindElement(By.Id(dialogButtonId)), modalId, test, waitForDismissal);
        }
    }
}
