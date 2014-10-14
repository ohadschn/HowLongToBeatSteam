using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;

namespace MissingGamesUpdater
{
    class MissingUpdater
    {
        private static readonly Uri GetSteamAppListUrl = new Uri("http://api.steampowered.com/ISteamApps/GetAppList/v0001/");
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
            MissingUpdaterEventSource.Log.UpdateMissingGamesStart();

            var steamTask = GetAllSteamApps();
            var tableTask = TableHelper.GetAllApps(ae => ae.SteamAppId);
            
            await Task.WhenAll(steamTask, tableTask).ConfigureAwait(false);

            var apps = steamTask.Result;
            var knownSteamIds = tableTask.Result;

            var knownSteamIdsHash = new HashSet<int>(knownSteamIds);
            var missingApps = apps.Where(a => !knownSteamIdsHash.Contains(a.appid));

            var updates = await SteamStoreHelper.GetStoreInformationUpdates(missingApps.Select(a => new BasicStoreInfo(a.appid, a.name, null)), s_client)
                .ConfigureAwait(false);

            await TableHelper.Insert(updates, 5).ConfigureAwait(false);     //we're inserting new entries, no fear of collisions 
            MissingUpdaterEventSource.Log.UpdateMissingGamesStop();            //(even if two jobs overlap the next one will fix it)
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            MissingUpdaterEventSource.Log.RetrieveAllSteamAppsStart(GetSteamAppListUrl);
            AllGamesRoot allGamesRoot;
            using (var response = await s_client.GetAsync(GetSteamAppListUrl).ConfigureAwait(false))
            {
                allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>().ConfigureAwait(false);
            }
            MissingUpdaterEventSource.Log.RetrieveAllSteamAppsStop(GetSteamAppListUrl);

            if (allGamesRoot == null || allGamesRoot.applist == null || allGamesRoot.applist.apps == null || allGamesRoot.applist.apps.app == null)
            {
                MissingUpdaterEventSource.Log.ErrorRetrievingAllSteamApps(GetSteamAppListUrl);
                throw new InvalidOperationException("Invalid response from " + GetSteamAppListUrl);
            }

            var apps = allGamesRoot.applist.apps.app;
            MissingUpdaterEventSource.Log.RetrievedAllSteamApps(GetSteamAppListUrl, apps.Count);
            return apps;
        }
    }
}