using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class SteamStoreHelper
    {
        public const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public const int MaxSteamStoreIdsPerRequest = 50;

        public static async Task<ConcurrentBag<AppEntity>> GetStoreInformationUpdates(IEnumerable<BasicStoreInfo> missingApps, HttpRetryClient client)
        {
            int counter = 0;
            var updates = new ConcurrentBag<AppEntity>();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async partition =>
            {
                Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest);
                SiteUtil.TraceInformation("Retrieving store info for apps {0}-{1}", counter - MaxSteamStoreIdsPerRequest + 1, counter);
                await GetStoreInfo(partition, updates, client).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return updates;
        }

        public static async Task GetStoreInfo(IList<BasicStoreInfo> apps, ConcurrentBag<AppEntity> updates, HttpRetryClient client)
        {
            var requestUrl = String.Format(SteamStoreApiUrlTemplate, String.Join(",", apps.Select(si => si.AppId)));
            SiteUtil.TraceInformation("Getting app store information from: {0}", requestUrl);
            
            JObject jObject;
            using (var response = await client.GetAsync(requestUrl).ConfigureAwait(false))
            {
                jObject = await response.Content.ReadAsAsync<JObject>().ConfigureAwait(false);
            }

            foreach (var app in apps)
            {
                var appInfo = jObject[app.AppId.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                string type = !appInfo.success || appInfo.data == null
                    ? AppEntity.UnknownType
                    : appInfo.data.type;

                if (app.AppType == type)
                {
                    SiteUtil.TraceInformation("Skipping already categorized app {0} / {1} ({2})", app.AppId, app.Name, type); 
                    continue;
                }

                SiteUtil.TraceInformation("Categorizing {0} / {1} as {2}", app.AppId, app.Name, type);
                updates.Add(new AppEntity(app.AppId, app.Name, type));
            }
        }
    }

    public class BasicStoreInfo
    {
        public int AppId { get; private set; }
        public string Name { get; private set; }
        public string AppType { get; private set; }

        public BasicStoreInfo(int appId, string name, string appType)
        {
            AppType = appType;
            Name = name;
            AppId = appId;
        }
    }
}
