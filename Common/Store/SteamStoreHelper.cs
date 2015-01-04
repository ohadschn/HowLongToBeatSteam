using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        private static readonly int MaxSteamStoreIdsPerRequest = SiteUtil.GetOptionalValueFromConfig("MaxSteamStoreIdsPerRequest", 50);

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

                if (type == AppEntity.UnknownType)
                {
                    CommonEventSource.Log.CategorizingUnknownApp(app.AppId, app.Name);
                    updates.Add(new AppEntity(app.AppId, app.Name, AppEntity.UnknownType));
                    return;
                }

                Trace.Assert(appInfo.data != null, "appInfo.data != null");
                var platforms = GetPlatforms(appInfo);
                var categories = GetCategories(appInfo.data.categories);
                var genres = GetGenres(appInfo.data.genres);
                var publishers = GetDistinctListOrUnknown(appInfo.data.publishers);
                var developers = GetDistinctListOrUnknown(appInfo.data.developers);
                var releaseDate = GetReleaseDate(appInfo.data.release_date);
                var metaCriticScore = GetMetaCriticScore(appInfo.data.metacritic);
                CommonEventSource.Log.CategorizingApp(app.AppId, app.Name, type, platforms, categories.ToFlatString(), genres.ToFlatString(), 
                    publishers.ToFlatString(), developers.ToFlatString(), releaseDate.ToString(CultureInfo.InvariantCulture), metaCriticScore);
                updates.Add(new AppEntity(app.AppId, app.Name, type, platforms, categories, genres, publishers, developers, releaseDate, metaCriticScore));
            }

            CommonEventSource.Log.RetrieveStoreInformationStop(start, counter, requestUrl);
        }

        private static IReadOnlyList<string> GetGenres(IEnumerable<Genre> genres)
        {
            return genres == null
                ? AppEntity.UnknownList
                : GetDistinctListOrUnknown(genres.Select(g => g.description));
        }

        private static IReadOnlyList<string> GetCategories(IEnumerable<Category> categories)
        {
            return categories == null
                ? AppEntity.UnknownList
                : GetDistinctListOrUnknown(categories.Select(c => c.description));
        }

        private static IReadOnlyList<string> GetDistinctListOrUnknown(IEnumerable<string> values)
        {
            if (values == null)
            {
                return AppEntity.UnknownList;
            }
            var distinct = values.Where(s => !String.IsNullOrWhiteSpace(s)).Distinct().ToArray();
            return (distinct.Length > 0) ? distinct : AppEntity.UnknownList;
        }

        private static DateTime GetReleaseDate(ReleaseDate releaseDate)
        {
            if (releaseDate == null)
            {
                return AppEntity.UnknownDate;
            }

            DateTime ret;
            return DateTime.TryParse(releaseDate.date, out ret)
                ? ret
                : AppEntity.UnknownDate;
        }

        private static int GetMetaCriticScore(Metacritic metacritic)
        {
            return metacritic == null
                ? AppEntity.UnknownScore
                : metacritic.score;
        }

        private static Entities.Platforms GetPlatforms(StoreAppInfo appInfo)
        {
            if (appInfo.data.platforms == null)
            {
                return Entities.Platforms.None;
            }

            return (appInfo.data.platforms.windows ? Entities.Platforms.Windows : Entities.Platforms.None)
                   | (appInfo.data.platforms.mac ? Entities.Platforms.Mac : Entities.Platforms.None)
                   | (appInfo.data.platforms.linux ? Entities.Platforms.Linux : Entities.Platforms.None);
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
