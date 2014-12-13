using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Common.Storage;
using Common.Store;
using Common.Util;
using MissingGamesUpdater.Logging;

namespace MissingGamesUpdater.Updater
{
    class MissingUpdater
    {
        private static readonly Uri GetSteamAppListUrl = new Uri("http://api.steampowered.com/ISteamApps/GetAppList/v0001/");
        private static HttpRetryClient s_client;

        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.SetDefaultConnectionLimit();
                using (s_client = new HttpRetryClient(100))
                {
                    UpdateMissingGames().Wait();                
                }
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task UpdateMissingGames()
        {
            MissingUpdaterEventSource.Log.UpdateMissingGamesStart();

            var steamTask = GetAllSteamApps();
            var tableTask = StorageHelper.GetAllApps(ae => ae.SteamAppId);
            
            await Task.WhenAll(steamTask, tableTask).ConfigureAwait(false);

            var apps = steamTask.Result;
            var knownSteamIds = tableTask.Result;

            var knownSteamIdsHash = new HashSet<int>(knownSteamIds);
            var missingApps = apps.Where(a => !knownSteamIdsHash.Contains(a.appid));

            var updates = await SteamStoreHelper.GetStoreInformationUpdates(missingApps.Select(a => new BasicStoreInfo(a.appid, a.name, null)), s_client)
                .ConfigureAwait(false);

            await StorageHelper.InsertApps(updates, 5).ConfigureAwait(false);     //we're inserting new entries, no fear of collisions 
            MissingUpdaterEventSource.Log.UpdateMissingGamesStop();            //(even if two jobs overlap the next one will fix it)
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            MissingUpdaterEventSource.Log.RetrieveAllSteamAppsStart(GetSteamAppListUrl);

            var allGamesRoot = await SiteUtil.GetAsync<AllGamesRoot>(s_client, GetSteamAppListUrl).ConfigureAwait(false);

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