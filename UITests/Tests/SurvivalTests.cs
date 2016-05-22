using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using UITests.Constants;
using UITests.Helpers;
using UITests.Util;

namespace UITests.Tests
{
    [TestClass]
    public class SurvivalTests
    {
        private static void AssertSurvival(IWebDriver driver, Gender gender, int birthYear, int weeklyPlaytime, PlayStyle playStyle, bool expectSurvival)
        {
            SiteHelper.SignInWithId(driver, UserConstants.SampleSteamId);
            SiteHelper.CalculateSurvival(driver, gender, birthYear, weeklyPlaytime, playStyle);

            Console.WriteLine("Parsing backlog completion date...");
            var backlogCompletion = TestUtil.ParseBrowserDate(driver, driver.FindElement(By.Id(SiteConstants.SurvivalBacklogCompletionLabelId)).Text);

            Console.WriteLine("Parsing time of death date...");
            var timeOfDeath = TestUtil.ParseBrowserDate(driver, driver.FindElement(By.Id(SiteConstants.SurvivalTimeOfDeathLabelId)).Text);

            Console.WriteLine("Asserting expected results...");
            bool survival = timeOfDeath >= backlogCompletion;
            if (expectSurvival)
            {
                Assert.IsTrue(survival, "Expected time of death to come after backlog completion");
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.SurvivalSuccessImgId)).Displayed, "Expected backlog completion success");
            }
            else
            {
                Assert.IsFalse(survival, "Expected time of death to come before backlog completion");
                Assert.IsTrue(driver.FindElement(By.Id(SiteConstants.SurvivalFailureImgId)).Displayed, "Expected backlog completion failure");
            }
        }

        [TestMethod]
        public void TestSurvival()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                Console.WriteLine("Asserting the survival of a 20 year old female who plays 100 hours a week in Main style...");
                AssertSurvival(driver, Gender.Female, DateTime.Now.Year - 20, 100, PlayStyle.Main, true);
            });
        }

        [TestMethod]
        public void TestNonSurvival()
        {
            TestUtil.ExecuteOnAllBrowsers(driver =>
            {
                Console.WriteLine("Asserting the non-survival of a 70 year old make who plays 1 hour a wekk in Completionist style...");
                AssertSurvival(driver, Gender.Male, DateTime.Now.Year - 70, 1, PlayStyle.Completionist, false);
            });
        }
    }
}
