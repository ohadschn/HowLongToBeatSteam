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
            
            SiteEventSource.Log.RetrieveMissingStoreInformationStart();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async partition =>
            {
                await GetStoreInfo(Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest), partition, updates, client).ConfigureAwait(false);
            }).ConfigureAwait(false);
            SiteEventSource.Log.RetrieveMissingStoreInformationStop();

            return updates;
        }

        private static async Task GetStoreInfo(int counter, IList<BasicStoreInfo> apps, ConcurrentBag<AppEntity> updates, HttpRetryClient client)
        {
            var start = counter - MaxSteamStoreIdsPerRequest + 1;
            var requestUrl = new Uri(String.Format(SteamStoreApiUrlTemplate, String.Join(",", apps.Select(si => si.AppId))));
            SiteEventSource.Log.RetrieveStoreInformationStart(start, counter, requestUrl);
            
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
                    SiteEventSource.Log.SkippedCategorizedApp(app.AppId, app.Name, type);
                    continue;
                }

                SiteEventSource.Log.CategorizingApp(app.AppId, app.Name, type);
                updates.Add(new AppEntity(app.AppId, app.Name, type));
            }

            SiteEventSource.Log.RetrieveStoreInformationStop(start, counter, requestUrl);
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
