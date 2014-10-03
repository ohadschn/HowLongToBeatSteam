using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json.Linq;

namespace MissingGamesUpdater
{
    class MissingUpdater
    {
        private const string GetSteamAppListUrl = "http://api.steampowered.com/ISteamApps/GetAppList/v0001/";
        private const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        private const int MaxSteamStoreIdsPerRequest = 50;
        private const int MaxConcurrentRequests = 20;

        private static readonly HttpClient Client = new HttpClient();

        static void Main()
        {
            using (Client)
            {
                UpdateMissingGames().Wait();                
            }
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
            var missingApps = apps.Where(a => !knownSteamIdsHash.Contains(a.appid));

            Util.TraceInformation("Retrieving store information for missing apps...");
            int counter = 0;
            var updates = new ConcurrentBag<AppEntity>();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(MaxConcurrentRequests, async partition =>
            {
                Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest);
                Util.TraceInformation("Retrieving store info for apps {0}-{1}", counter - MaxSteamStoreIdsPerRequest + 1, counter);
                await GetStoreInfo(partition, updates);
            });

            Util.TraceInformation("Updating missing apps: {0}", 
                String.Join(",", updates.Select(a => String.Format("{0} / {1} ({2})", a.SteamAppId, a.SteamName, a.Type))));

            await TableHelper.InsertOrReplace(updates);
            Util.TraceInformation("Finished updating missing apps");
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            Util.TraceInformation("Getting list of all Steam apps from {0}...", GetSteamAppListUrl);
            var response = await Client.GetAsync(GetSteamAppListUrl);
            response.EnsureSuccessStatusCode();

            var allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>();

            Util.TraceInformation("Finished getting all steam apps");
            return allGamesRoot.applist.apps.app;
        }

        private static async Task GetStoreInfo(IList<App> apps, ConcurrentBag<AppEntity> updates)
        {
            var requestUri = String.Format(SteamStoreApiUrlTemplate, string.Join(",", apps.Select(ae => ae.appid)));

            Util.TraceInformation("Getting app store information from: {0}", requestUri);
            var response = await Client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var jObject = await response.Content.ReadAsAsync<JObject>();
            foreach (var app in apps)
            {
                var appInfo = jObject[app.appid.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                string type;
                if (!appInfo.success || appInfo.data == null)
                {
                    type = "Unknown";
                }
                else
                {
                    type = appInfo.data.type;
                }

                Util.TraceInformation("Categorizing {0} / {1} as {2}", app.appid, app.name, type);
                updates.Add(new AppEntity(app.appid, app.name, type));
            }
        }
    }
}
