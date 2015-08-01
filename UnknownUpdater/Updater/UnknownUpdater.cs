using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Store;
using Common.Util;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;
using SteamHltbScraper.Imputation;
using SteamHltbScraper.Scraper;
using UnknownUpdater.Logging;

namespace UnknownUpdater.Updater
{
    class UnknownUpdater
    {
        private static readonly int StoreApiRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStoreApiRetries", 1000);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStorageRetries", 100);
        private static readonly int UpdateLimit = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterUpdateLimit", int.MaxValue);

        private static readonly HttpRetryClient Client = new HttpRetryClient(StoreApiRetries);
        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("UnknownUpdater");
                SiteUtil.SetDefaultConnectionLimit();
                using (Client)
                {
                    UpdateUnknownApps().Wait();
                }
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        class AppEntitySteamIdComparer : IEqualityComparer<AppEntity>
        {
            public bool Equals([NotNull] AppEntity x, [NotNull] AppEntity y)
            {
                if (x == null) throw new ArgumentNullException("x");
                if (y == null) throw new ArgumentNullException("y");

                return x.SteamAppId == y.SteamAppId;
            }

            public int GetHashCode([NotNull] AppEntity appEntity)
            {
                if (appEntity == null) throw new ArgumentNullException("appEntity");

                return appEntity.SteamAppId;
            }
        }


        private async static Task UpdateUnknownApps()
        {
            var ticks = Environment.TickCount;
            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStart();

            var allUnknownApps = (await StorageHelper.GetAllApps(AppEntity.UnknownFilter, StorageRetries).ConfigureAwait(false)).Take(UpdateLimit).ToArray();
            
            var updates = new ConcurrentBag<AppEntity>();
            InvalidOperationException ioe = null;
            try
            {
                await SteamStoreHelper.GetStoreInformationUpdates(
                    allUnknownApps.Select(ae => new BasicStoreInfo(ae.SteamAppId, ae.SteamName, ae.AppType)).ToArray(), Client, updates).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ioe = new InvalidOperationException("Could not retrieve store information for all apps", e);
            }

            UnknownUpdaterEventSource.Log.UpdateNewlyCategorizedApps(updates);

            var measuredUpdates = updates.Where(a => a.Measured).ToArray();
            if (measuredUpdates.Length > 0)
            {
                await HltbScraper.ScrapeHltb(measuredUpdates).ConfigureAwait(false);

                //re-impute with new scraped values for updated games (Enumberable.Union() guarantees measuredUpdates will be enumerated before allApps!)
                var allMeasuredApps = await StorageHelper.GetAllApps(AppEntity.MeasuredFilter).ConfigureAwait(false);
                await Imputer.Impute(measuredUpdates.Union(allMeasuredApps, new AppEntitySteamIdComparer()).ToArray()).ConfigureAwait(false);    
            }

            var unknownAppsMap = allUnknownApps.ToDictionary(ae => ae.SteamAppId);
            await StorageHelper.ExecuteOperations(updates,
                ae => new[] {TableOperation.Delete(unknownAppsMap[ae.SteamAppId]), TableOperation.Insert(ae)},
                StorageHelper.SteamToHltbTableName, "updating previously unknown games", StorageRetries).ConfigureAwait(false);

            if (ioe != null)
            {
                throw ioe; //fail job
            }

            await SiteUtil.SendSuccessMail("Unknown updater", 
                updates.Count + " previously unknown game(s) updated", ticks).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStop();
        }
    }
}
