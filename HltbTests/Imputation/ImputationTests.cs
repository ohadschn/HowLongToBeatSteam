using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
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
            var games = GetSampleGames();

            await Imputer.Impute(games).ConfigureAwait(false);

            AssertValidTtbs(games);
        }

        [TestMethod]
        public async Task TestImputationFromGenreStats()
        {
            var games = GetSampleGames();

            await Imputer.ImputeFromStats(games).ConfigureAwait(false);

            AssertValidTtbs(games);
        }

        private static AppEntity[] GetSampleGames()
        {
            int i = 1;
            return File.ReadLines("games.csv").Select(row =>
            {
                var gameValues = row.Split(',');
                Trace.Assert(gameValues.Length == 5, "Invalid CSV row (must contain exactly 5 values) " + row);

                var game = new AppEntity(i, "Game" + i.ToString(CultureInfo.InvariantCulture),
                    Boolean.Parse(gameValues[0]) ? AppEntity.GameTypeName : AppEntity.DlcTypeName)
                {
                    Genres = new[] {gameValues[1]}
                };

                var mainTtb = Imputer.GetRoundedValue(gameValues[2]);
                game.SetMainTtb(mainTtb, mainTtb == 0);

                var extrasTtb = Imputer.GetRoundedValue(gameValues[3]);
                game.SetExtrasTtb(extrasTtb, extrasTtb == 0);

                var completionistTtb = Imputer.GetRoundedValue(gameValues[4]);
                game.SetCompletionistTtb(completionistTtb, completionistTtb == 0);

                i++;

                return game;
            }).ToArray();
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
