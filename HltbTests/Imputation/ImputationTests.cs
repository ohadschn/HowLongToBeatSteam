using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Common.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteamHltbScraper.Imputation;

namespace HltbTests.Imputation
{
    [TestClass]
    public class ImputationTests
    {
        [TestMethod]
        public void TestImputation()
        {
            File.Delete(Path.Combine(Imputer.GetDataPath(), Imputer.ImputedCsvFileName));

            var apps = File.ReadLines("Imputation\\ttb.csv").Select(row =>
            {
                var app = new AppEntity();
                Imputer.UpdateFromCsvRow(app, row);
                return app;
            }).ToArray();

            Imputer.Impute(apps, apps);

            foreach (var app in apps)
            {
                Assert.IsTrue(app.MainTtb <= app.ExtrasTtb, 
                    String.Format(CultureInfo.InvariantCulture, "Main {0} > Extras {1}", app.MainTtb, app.ExtrasTtb));

                Assert.IsTrue(app.ExtrasTtb <= app.CompletionistTtb, 
                    String.Format(CultureInfo.InvariantCulture, "Extras {0} > Completionist {1}", app.MainTtb, app.ExtrasTtb));

                Console.WriteLine("{0} {1} {2}", app.MainTtb, app.ExtrasTtb, app.CompletionistTtb);
            }
        }
    }
}
