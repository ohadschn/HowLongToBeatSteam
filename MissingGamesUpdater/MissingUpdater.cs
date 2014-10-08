using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;

namespace MissingGamesUpdater
{
    class MissingUpdater
    {
        private const string GetSteamAppListUrl = "http://api.steampowered.com/ISteamApps/GetAppList/v0001/";

        private static HttpRetryClient s_client;

        static void Main()
        {
            SiteUtil.SetDefaultConnectionLimit();
            using (s_client = new HttpRetryClient(5))
            {
                UpdateMissingGames().Wait();                
            }
        }

        private static async Task UpdateMissingGames()
        {
            SiteUtil.TraceInformation("Querying for all Steam apps and all stored apps...");

            var steamTask = GetAllSteamApps();
            var tableTask = TableHelper.GetAllApps(ae => ae.SteamAppId);
            
            await Task.WhenAll(steamTask, tableTask).ConfigureAwait(false);

            var apps = steamTask.Result;
            var knownSteamIds = tableTask.Result;

            SiteUtil.TraceInformation("Preparing known IDs hash...");
            var knownSteamIdsHash = new HashSet<int>(knownSteamIds);

            SiteUtil.TraceInformation("Identifying missing apps...");
            var missingApps = apps.Where(a => !knownSteamIdsHash.Contains(a.appid));

            SiteUtil.TraceInformation("Retrieving store information for missing apps...");
            var updates = await SteamStoreHelper.GetStoreInformationUpdates(missingApps.Select(a => new BasicStoreInfo(a.appid, a.name, null)), s_client)
                .ConfigureAwait(false);

            await TableHelper.Insert(updates, 5).ConfigureAwait(false);     //we're inserting new entries, no fear of collisions 
            SiteUtil.TraceInformation("Finished updating apps");            //(even it two jobs overlap the next one will fix it)
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            SiteUtil.TraceInformation("Getting list of all Steam apps from {0}...", GetSteamAppListUrl);
            
            AllGamesRoot allGamesRoot;
            using (var response = await s_client.GetAsync(GetSteamAppListUrl).ConfigureAwait(false))
            {
                allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>().ConfigureAwait(false);
            }

            SiteUtil.TraceInformation("Finished getting all steam apps");
            return allGamesRoot.applist.apps.app;
        }
    }
}