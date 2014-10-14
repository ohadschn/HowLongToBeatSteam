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
            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStart();
            
            var apps = await TableHelper.GetAllApps(ae => ae, AppEntity.UnknownFilter, 5).ConfigureAwait(false);
            var updates = await SteamStoreHelper.GetStoreInformationUpdates(
                        apps.Select(ae => new BasicStoreInfo(ae.SteamAppId, ae.SteamName, ae.AppType)), Client).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateNewlyCategorizedApps(updates);

            var appsDict = apps.ToDictionary(ae => ae.SteamAppId);
            await TableHelper.ExecuteOperations(updates,
                    ae => new[] {TableOperation.Delete(appsDict[ae.SteamAppId]), TableOperation.Insert(ae)}, 10).ConfigureAwait(false);

            UnknownUpdaterEventSource.Log.UpdateUnknownAppsStop();
        }
    }
}
