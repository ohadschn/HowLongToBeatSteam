using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage.Table;

namespace UnknownUpdater
{
    class UnknownUpdater
    {
        private static readonly HttpRetryClient Client = new HttpRetryClient(5);
        static void Main()
        {
            SiteUtil.SetDefaultConnectionLimit();
            using (Client)
            {
                UpdateUnknownApps().Wait();
            }
        }

        private async static Task UpdateUnknownApps()
        {
            SiteUtil.TraceInformation("Getting all unknown apps...");
            var apps = await TableHelper.GetAllApps(ae => ae, AppEntity.UnknownFilter, 5).ConfigureAwait(false);

            SiteUtil.TraceInformation("Re-querying unknown apps...");
            var updates = await SteamStoreHelper.GetStoreInformationUpdates(
                        apps.Select(ae => new BasicStoreInfo(ae.SteamAppId, ae.SteamName, ae.AppType)), Client).ConfigureAwait(false);

            SiteUtil.TraceInformation("Updating newly categorized apps: {0}", String.Join(", ", updates.Select(
                ae => String.Format(CultureInfo.InvariantCulture, "{0} / {1}", ae.SteamAppId, ae.SteamName))));

            var appsDict = apps.ToDictionary(ae => ae.SteamAppId);
            await TableHelper.ExecuteOperations(updates,
                    ae => new[] {TableOperation.Delete(appsDict[ae.SteamAppId]), TableOperation.Insert(ae)}, 10).ConfigureAwait(false);
            
            SiteUtil.TraceInformation("Finished updating apps");
        }
    }
}
