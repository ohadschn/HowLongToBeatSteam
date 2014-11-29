using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Util;
using Newtonsoft.Json.Linq;

namespace Common.Store
{
    public static class SteamStoreHelper
    {
        public const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        public const int MaxSteamStoreIdsPerRequest = 50;

        public static async Task<ConcurrentBag<AppEntity>> GetStoreInformationUpdates(IEnumerable<BasicStoreInfo> missingApps, HttpRetryClient client)
        {
            int counter = 0;
            var updates = new ConcurrentBag<AppEntity>();
            
            CommonEventSource.Log.RetrieveMissingStoreInformationStart();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async partition =>
            {
                await GetStoreInfo(Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest), partition, updates, client).ConfigureAwait(false);
            }).ConfigureAwait(false);
            CommonEventSource.Log.RetrieveMissingStoreInformationStop();

            return updates;
        }

        private static async Task GetStoreInfo(int counter, IList<BasicStoreInfo> apps, ConcurrentBag<AppEntity> updates, HttpRetryClient client)
        {
            var start = counter - MaxSteamStoreIdsPerRequest + 1;
            var requestUrl = new Uri(String.Format(SteamStoreApiUrlTemplate, String.Join(",", apps.Select(si => si.AppId))));
            CommonEventSource.Log.RetrieveStoreInformationStart(start, counter, requestUrl);

            var jObject = await SiteUtil.GetAsync<JObject>(client, requestUrl).ConfigureAwait(false);

            foreach (var app in apps)
            {
                var appInfo = jObject[app.AppId.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                string type = !appInfo.success || appInfo.data == null
                    ? AppEntity.UnknownType
                    : appInfo.data.type;

                if (app.AppType == type)
                {
                    CommonEventSource.Log.SkippedCategorizedApp(app.AppId, app.Name, type);
                    continue;
                }

                CommonEventSource.Log.CategorizingApp(app.AppId, app.Name, type);
                updates.Add(new AppEntity(app.AppId, app.Name, type));
            }

            CommonEventSource.Log.RetrieveStoreInformationStop(start, counter, requestUrl);
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
