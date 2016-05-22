using System;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Util;

namespace UITests.Helpers
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum PlayStyle
    {
        Main,
        Extras,
        Completionist
    }

    public class SurvivalHelper
    {
        public static void CalculateSurvival(IWebDriver driver, Gender gender, int birthYear, int weeklyPlaytime, PlayStyle playStyle)
        {
            DialogHelper.TestDialog(driver, SiteConstants.SurvivalCalculatorAnchorId, SiteConstants.SurvivalModalId, () =>
            {
                Console.WriteLine("Populating calculator settings...");
                driver.SelectValue(By.Id(SiteConstants.SurvivalGenderSelectId), gender.ToString());
                driver.SelectValue(By.Id(SiteConstants.SurvivalBirthYearSelectId), birthYear.ToString());
                driver.SelectValue(By.Id(SiteConstants.SurvivalWeeklyPlaytimeSelectId), weeklyPlaytime.ToString());
                driver.SelectValue(By.Id(SiteConstants.SurvivalPlayStyleSelectId), playStyle.ToString());

                Console.WriteLine("Starting calculation...");
                driver.FindElement(By.Id(SiteConstants.SurvivalCalculatorButtonId)).Click();

                Console.WriteLine("Waiting for calculation to complete...");
                driver.WaitUntilElementCondition(By.Id(SiteConstants.SurvivalBacklogCompletionLabelId), e => e.Text != SiteConstants.SurvivalNotCalculatedText);
            }, false);
        }
    }
}
