using System;
using System.Globalization;
using System.Threading.Tasks;
using Common.Entities;
using Common.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Imputation;

namespace HltbTests.Imputation
{
    [TestClass]
    public class ImputationTests
    {
        [TestMethod]
        public async Task TestImputation()
        {
            var games = (await StorageHelper.GetAllApps(AppEntity.MeasuredFilter)).ToArray();

            await Imputer.ImputeByGenre(games).ConfigureAwait(false);
            AssertValidTtbs(games);
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
