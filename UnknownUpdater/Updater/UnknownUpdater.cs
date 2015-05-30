using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Store;
using Common.Util;
using Microsoft.WindowsAzure.Storage.Table;
using UnknownUpdater.Logging;

namespace UnknownUpdater.Updater
{
    class UnknownUpdater
    {
        private static readonly int StoreApiRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStoreApiRetries", 1000);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStorageRetries", 100);

        private static readonly HttpRetryClient Client = new HttpRetryClient(StoreApiRetries);
        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
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

        private async static Task UpdateUnknownApps()
        {
            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStart();

            var apps = await StorageHelper.GetAllApps(AppEntity.UnknownFilter, StorageRetries).ConfigureAwait(false);
            
            var updates = new ConcurrentBag<AppEntity>();
            InvalidOperationException ioe = null;
            try
            {
                await SteamStoreHelper.GetStoreInformationUpdates(
                    apps.Select(ae => new BasicStoreInfo(ae.SteamAppId, ae.SteamName, ae.AppType)), Client, updates).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ioe = new InvalidOperationException("Could not retrieve store information for all apps", e);
            }

            UnknownUpdaterEventSource.Log.UpdateNewlyCategorizedApps(updates);

            var appsDict = apps.ToDictionary(ae => ae.SteamAppId);
            await StorageHelper.ExecuteOperations(updates,
                ae => new[] {TableOperation.Delete(appsDict[ae.SteamAppId]), TableOperation.Insert(ae)},
                StorageHelper.SteamToHltbTableName, StorageRetries).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStop();

            if (ioe != null)
            {
                throw ioe; //fail job
            }
        }
    }
}
