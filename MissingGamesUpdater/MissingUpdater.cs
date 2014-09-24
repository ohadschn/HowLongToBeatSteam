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
        private const string GetSteamAppListUrl = "http://api.steampowered.com/ISteamApps/GetAppList/v0001/";

        static void Main()
        {
            UpdateMissingGames().Wait();
        }

        private static async Task UpdateMissingGames()
        {
            Util.TraceInformation("Querying for all Steam apps and all stored apps...");

            var steamTask = GetAllSteamApps();
            var tableTask = TableHelper.GetAllApps(ae => ae.SteamAppId);
            
            await Task.WhenAll(steamTask, tableTask);

            var apps = steamTask.Result;
            var knownSteamIds = tableTask.Result;

            Util.TraceInformation("Preparing known IDs hash...");
            var knownSteamIdsHash = new HashSet<int>(knownSteamIds);

            Util.TraceInformation("Identifying missing apps...");
            var missingApps = apps.Where(a => !knownSteamIdsHash.Contains(a.appid)).Select(a => new AppEntity(a.appid, a.name)).ToArray();

            Util.TraceInformation("Updating missing apps: {0}",
                String.Join(",", missingApps.Select(a => String.Format("{0} / {1}", a.SteamAppId, a.SteamName))));

            TableHelper.InsertOrReplace(missingApps);
            Util.TraceInformation("Finished updating missing apps");
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            Util.TraceInformation("Getting list of all Steam apps from {0}...", GetSteamAppListUrl);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(GetSteamAppListUrl);
                response.EnsureSuccessStatusCode();

                var allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>();
                return allGamesRoot.applist.apps.app;
            }
        }
    }
}
