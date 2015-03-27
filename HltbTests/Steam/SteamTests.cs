using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Store;
using MissingGamesUpdater.Updater;
using Common.Util;
using Common.Entities;

namespace HltbTests.Steam
{
    [TestClass]
    public class SteamTests
    {
        [TestMethod]
        public void TestStoreApi()
        {
            ConcurrentBag<AppEntity> updates;

            using (var client = new HttpRetryClient(200))
            {
                var steamApps = MissingUpdater.GetAllSteamApps(client).Result;
                var portalApp = steamApps.FirstOrDefault(app => app.appid == 400 && String.Equals(app.name, "Portal", StringComparison.Ordinal));
                Assert.IsNotNull(portalApp, "Could not find Portal in the steam library: {0}",
                    String.Join(", ", steamApps.Select(a => String.Format(CultureInfo.InvariantCulture, "{0}/{1}", a.appid, a.name))));

                updates = SteamStoreHelper.GetStoreInformationUpdates(new[] { new BasicStoreInfo(portalApp.appid, portalApp.name, null) }, client).Result;
            }

            Assert.AreEqual(1, updates.Count, "Expected exactly one update for requested app (Portal)");
            var portal = updates.First();

            Assert.IsTrue(portal.IsGame, "Portal is not classified as a game");
            Assert.IsFalse(portal.IsDlc, "Portal is classified as a DLC");
            Assert.IsFalse(portal.IsMod, "Portal is classified as a mod");

            Assert.IsTrue(portal.Categories.Contains("Single-player", StringComparer.OrdinalIgnoreCase),
                "Portal is not classified as single-player: {0}", portal.CategoriesFlat);

            Assert.AreEqual("Valve", portal.Developers.SingleOrDefault(),
                "Valve are not listed as the sole developers of Portal: {0}", portal.DevelopersFlat);

            Assert.AreEqual("Valve", portal.Publishers.SingleOrDefault(),
                "Valve are not listed as the sole publishers of Portal: {0}", portal.PublishersFlat);

            Assert.IsTrue(portal.Genres.Contains("Action", StringComparer.OrdinalIgnoreCase),
                "Portal is not classified as an action game: {0}", portal.GenresFlat);

            Assert.IsTrue(portal.MetacriticScore > 85, "Portal is scored too low on Metacritic: {0}", portal.MetacriticScore);

            Assert.IsTrue(portal.Platforms.HasFlag(Common.Entities.Platforms.Windows) &&
                            portal.Platforms.HasFlag(Common.Entities.Platforms.Linux) &&
                            portal.Platforms.HasFlag(Common.Entities.Platforms.Mac),
                            "Portal is not listed as supported on Windows, Mac, and Linux: {0}", portal.Platforms);

            Assert.AreEqual(new DateTime(2007, 10, 10), portal.ReleaseDate, "Portal release date is incorrect");
        }
    }
}
