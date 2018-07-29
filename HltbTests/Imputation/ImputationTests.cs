using System;
using System.Collections.Generic;
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

        [TestMethod]
        public void TestInvalidTtbFixes() //checking all branches of Imputer.FixInvalidTtbs
        {
            // M > E >= C
            AssertTtbFixes(10, 8, 7, false, true, true);
            AssertTtbFixes(10, 7, 7, false, true, true);
            AssertTtbFixes(11, 7, 6, true, false, true);
            AssertTtbFixes(12, 6, 6, true, false, false);
            AssertTtbFixes(9, 8, 7, true, true, false);
            AssertTtbFixes(9, 8, 8, true, true, false);

            // C >= M > E
            AssertTtbFixes(7, 5, 10, true, false, false);
            AssertTtbFixes(7, 5, 7, true, false, false);
            AssertTtbFixes(10, 9, 20, false, true, true);
            AssertTtbFixes(20, 9, 20, false, true, false);

            // M > C > E
            AssertTtbFixes(20, 10, 15, true, false, false);
            AssertTtbFixes(10, 8, 9, false, true, true);

            // E > C >= M
            AssertTtbFixes(1, 7, 3, false, false, true);
            AssertTtbFixes(1, 7, 1, false, false, true);
            AssertTtbFixes(50, 90, 80, true, true, false);
            AssertTtbFixes(80, 90, 80, true, true, false);

            // E >= M > C
            AssertTtbFixes(10, 11, 9, false, false, true);
            AssertTtbFixes(11, 11, 9, false, false, true);
            AssertTtbFixes(30, 40, 29, true, true, false);
            AssertTtbFixes(30, 30, 29, true, true, false);
        }

        private static void AssertTtbFixes(int main, int extras, int completionist, bool mainImputed, bool extrasImputed, bool completionistImputed)
        {
            int mainBefore = main;
            int extrasBefore = extras;
            int completionistBefore = completionist;
            Imputer.FixInvalidTtbs(ref main, mainImputed, ref extras, extrasImputed, ref completionist, completionistImputed, new TtbRatios(0.7, 0.4, 0.3));
            Assert.IsTrue(completionist >= extras && extras >= main, 
                $"Invalid TTBs not fixed: M{mainBefore}/E{extrasBefore}/C{completionistBefore} -> M{main}/E{extras}/C{completionist}");
        }

        private static void AssertValidTtbs(IEnumerable<AppEntity> games)
        {
            foreach (var game in games)
            {
                Assert.AreNotEqual(0, game.MainTtb, "Main TTB is 0 for: " + game);
                Assert.AreNotEqual(0, game.ExtrasTtb, "Extras TTB is 0 for: " + game );
                Assert.AreNotEqual(0, game.CompletionistTtb, "Completionist TTB is 0 for: " + game);

                Assert.IsTrue(game.MainTtb <= game.ExtrasTtb, "Main is longer than Extras for: " + game);
                Assert.IsTrue(game.ExtrasTtb <= game.CompletionistTtb, "Extras is longer than Completionist for: " + game);

                Console.WriteLine("{0} {1} {2}", game.MainTtb, game.ExtrasTtb, game.CompletionistTtb);
            }
        }
    }
}