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
            var apps = File.ReadLines("Imputation\\ttb.csv").Select(row =>
            {
                var app = new AppEntity(++i, i.ToString(CultureInfo.InvariantCulture), GetRandomAppType())
                {
                    Genres = new[] { GetRandomGenre() }
                };

                var ttbs = row.Split(',');
                Trace.Assert(ttbs.Length == 3, "Invalid CSV row, contains more than 3 values: " + row);

                app.MainTtb = Imputer.GetRoundedValue(ttbs[0]);
                app.MainTtbImputed = app.MainTtb == 0;

                app.ExtrasTtb = Imputer.GetRoundedValue(ttbs[1]);
                app.ExtrasTtbImputed = app.ExtrasTtb == 0;

                app.CompletionistTtb = Imputer.GetRoundedValue(ttbs[2]);
                app.CompletionistTtbImputed = app.CompletionistTtb == 0;

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
