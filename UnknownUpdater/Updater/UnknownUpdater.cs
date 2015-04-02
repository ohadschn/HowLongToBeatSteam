﻿using System;
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
        private static readonly int StoreApiRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStoreApiRetries", 100);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("UnknownUpdaterStorageRetries", 10);

        private static readonly HttpRetryClient Client = new HttpRetryClient(StoreApiRetries);
        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
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

            var apps = await StorageHelper.GetAllApps(ae => ae, AppEntity.UnknownFilter, StorageRetries).ConfigureAwait(false);
            var updates = await SteamStoreHelper.GetStoreInformationUpdates(
                        apps.Select(ae => new BasicStoreInfo(ae.SteamAppId, ae.SteamName, ae.AppType)), Client).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateNewlyCategorizedApps(updates);

            var appsDict = apps.ToDictionary(ae => ae.SteamAppId);
            await StorageHelper.ExecuteAppOperations(updates,
                    ae => new[] { TableOperation.Delete(appsDict[ae.SteamAppId]), TableOperation.Insert(ae) }, StorageRetries).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStop();
        }
    }
}
