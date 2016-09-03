using System;
using System.Globalization;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Imputation;

namespace HltbTests.Imputation
{
    [TestClass]
    public class ImputationTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            EventSourceRegistrar.DisposeEventListeners();
        }

        [TestMethod]
        public async Task TestImputation()
        {
            var games = (await StorageHelper.GetAllApps(AppEntity.MeasuredFilter).ConfigureAwait(true)).ToArray();

            await Imputer.ImputeByGenre(games).ConfigureAwait(false);
            AssertValidTtbs(games);
        }

        [TestMethod]
        public void TestSanitization()
        {
            AssertSanitization(1, 2, 3, 1, 2, 3);
            AssertSanitization(2, 1, 3, 1, 2, 3);
            AssertSanitization(1, 3, 2, 1, 2, 3);
            AssertSanitization(2, 3, 1, 1, 2, 3);
            AssertSanitization(3, 1, 2, 1, 2, 3);
            AssertSanitization(3, 2, 1, 1, 2, 3);
            AssertSanitization(0, 100, 200, 0, 100, 200);
            AssertSanitization(0, 200, 100, 0, 100, 200);
            AssertSanitization(15, 0, 150, 15, 0, 150);
            AssertSanitization(150, 0, 15, 15, 0, 150);
            AssertSanitization(17, 18, 0, 17, 18, 0);
            AssertSanitization(18, 17, 0, 17, 18, 0);
            AssertSanitization(111, 0, 0, 111, 0, 0);
            AssertSanitization(0, 222, 0, 0, 222, 0);
            AssertSanitization(0, 0, 123, 0, 0, 123);
        }

        private static void AssertSanitization(int main, int extras, int completionist, int mainExpected, int extrasExpected, int completionistExpected)
        {
            var game = new AppEntity(1, "test", AppEntity.GameTypeName);
            game.SetMainTtb(main, main == 0);
            game.SetExtrasTtb(extras, extras == 0);
            game.SetCompletionistTtb(completionist, completionist == 0);
            
            Imputer.Sanitize(new [] {game});

            var ttbs = String.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", main, extras, completionist);
            Assert.AreEqual(mainExpected, game.MainTtb, "Invalid Main sanitization for TTBs: " + ttbs);
            Assert.AreEqual(extrasExpected, game.ExtrasTtb, "Invalid Extras sanitization for TTBs: " + ttbs);
            Assert.AreEqual(completionistExpected, game.CompletionistTtb, "Invalid Completionist sanitization for TTBs: " + ttbs);
        }

        private static void AssertValidTtbs(AppEntity[] games)
        {
            foreach (var game in games)
            {
                Assert.AreNotEqual(0, game.MainTtb, "Main TTB = 0");
                Assert.AreNotEqual(0, game.ExtrasTtb, "Extras TTB = 0");
                Assert.AreNotEqual(0, game.CompletionistTtb, "Completionist TTB = 0");

                Assert.IsTrue(game.MainTtb <= game.ExtrasTtb,
                    String.Format(CultureInfo.InvariantCulture, "Main {0} > Extras {1}", game.MainTtb, game.ExtrasTtb));

                Assert.IsTrue(game.ExtrasTtb <= game.CompletionistTtb,
                    String.Format(CultureInfo.InvariantCulture, "Extras {0} > Completionist {1}", game.MainTtb, game.ExtrasTtb));

                Console.WriteLine("{0} {1} {2}", game.MainTtb, game.ExtrasTtb, game.CompletionistTtb);
            }
        }
    }
}
