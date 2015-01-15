using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Util;
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
            int i = 1;
            var apps = File.ReadLines("Imputation\\games.csv").Select(row =>
            {
                var gameValues = row.Split(',');
                Trace.Assert(gameValues.Length == 5, "Invalid CSV row (must contain exactly 5 values) " + row);

                var app = new AppEntity(i, "Game" + i.ToString(CultureInfo.InvariantCulture),
                    Boolean.Parse(gameValues[0]) ? AppEntity.GameTypeName : AppEntity.DlcTypeName)
                {
                    Genres = new[] { gameValues[1]}
                };

                var mainTtb = Imputer.GetRoundedValue(gameValues[2]);
                app.SetMainTtb(mainTtb, mainTtb == 0);

                var extrasTtb = Imputer.GetRoundedValue(gameValues[3]);
                app.SetExtrasTtb(extrasTtb, extrasTtb == 0);

                var completionistTtb = Imputer.GetRoundedValue(gameValues[4]);
                app.SetCompletionistTtb(completionistTtb, completionistTtb == 0);

                i++;

                return app;
            }).ToArray();

            await Imputer.Impute(apps).ConfigureAwait(false);

            foreach (var app in apps)
            {
                Assert.IsTrue(app.MainTtb <= app.ExtrasTtb, 
                    String.Format(CultureInfo.InvariantCulture, "Main {0} > Extras {1}", app.MainTtb, app.ExtrasTtb));

                Assert.IsTrue(app.ExtrasTtb <= app.CompletionistTtb, 
                    String.Format(CultureInfo.InvariantCulture, "Extras {0} > Completionist {1}", app.MainTtb, app.ExtrasTtb));

                Console.WriteLine("{0} {1} {2}", app.MainTtb, app.ExtrasTtb, app.CompletionistTtb);
            }
        }

        private static string GetRandomGenre()
        {
            return GetRandomValue(new[] { "Action", "Strategy", "RPG", "Adventure", "Casual", "Indie", "Sports", "Simulation", "Racing" });
        }

        private static string GetRandomAppType()
        {
            return GetRandomValue(new[] {AppEntity.ModTypeName, AppEntity.DlcTypeName, AppEntity.GameTypeName});
        }

        private static T GetRandomValue<T>(IReadOnlyList<T> values)
        {
            return values[RandomGenerator.Next(0, values.Count)];
        }
    }
}
