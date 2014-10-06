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

        private static readonly HttpRetryClient Client = new HttpRetryClient(5);

        static void Main()
        {
            SiteUtil.SetDefaultConnectionLimit();
            using (Client)
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
            int counter = 0;
            var updates = new ConcurrentBag<AppEntity>();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async partition =>
            {
                Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest);
                SiteUtil.TraceInformation("Retrieving store info for apps {0}-{1}", counter - MaxSteamStoreIdsPerRequest + 1, counter);
                await GetStoreInfo(partition, updates).ConfigureAwait(false);
            }).ConfigureAwait(false);

            SiteUtil.TraceInformation("Updating missing apps: {0}", 
                String.Join(",", updates.Select(a => String.Format("{0} / {1} ({2})", a.SteamAppId, a.SteamName, a.AppType))));

            await TableHelper.Insert(updates, 5).ConfigureAwait(false); //we're inserting new entries, no fear of collisions (even it two jobs overlap the next one will fix it)
            SiteUtil.TraceInformation("Finished updating missing apps");
        }

        private static async Task<IList<App>> GetAllSteamApps()
        {
            SiteUtil.TraceInformation("Getting list of all Steam apps from {0}...", GetSteamAppListUrl);
            
            AllGamesRoot allGamesRoot;
            using (var response = await Client.GetAsync(GetSteamAppListUrl).ConfigureAwait(false))
            {
                allGamesRoot = await response.Content.ReadAsAsync<AllGamesRoot>().ConfigureAwait(false);
            }

            SiteUtil.TraceInformation("Finished getting all steam apps");
            return allGamesRoot.applist.apps.app;
        }

        private static async Task GetStoreInfo(IList<App> apps, ConcurrentBag<AppEntity> updates)
        {
            var requestUrl = String.Format(SteamStoreApiUrlTemplate, string.Join(",", apps.Select(ae => ae.appid)));
            SiteUtil.TraceInformation("Getting app store information from: {0}", requestUrl);
            
            JObject jObject;
            using (var response = await Client.GetAsync(requestUrl).ConfigureAwait(false))
            {
                jObject = await response.Content.ReadAsAsync<JObject>().ConfigureAwait(false);
            }

            foreach (var app in apps)
            {
                var appInfo = jObject[app.appid.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                string type;
                if (!appInfo.success || appInfo.data == null)
                {
                    type = AppEntity.UnknownType;
                }
                else
                {
                    type = appInfo.data.type;
                }

                SiteUtil.TraceInformation("Categorizing {0} / {1} as {2}", app.appid, app.name, type);
                updates.Add(new AppEntity(app.appid, app.name, type));
            }
        }
    }
}