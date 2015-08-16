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
        private const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        private static readonly int MaxSteamStoreIdsPerRequest = SiteUtil.GetOptionalValueFromConfig("MaxSteamStoreIdsPerRequest", 50);

        public static async Task GetStoreInformationUpdates(
            ICollection<BasicStoreInfo> missingApps, 
            HttpRetryClient client, 
            ConcurrentBag<AppEntity> updates)
        {
            int counter = 0;

            CommonEventSource.Log.RetrieveMissingStoreInformationStart();
            await missingApps.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests, async partition =>
            {
                await GetStoreInfo(Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest), missingApps.Count, partition, updates, client)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
            CommonEventSource.Log.RetrieveMissingStoreInformationStop();
        }

        private static async Task GetStoreInfo(int counter, int total, IList<BasicStoreInfo> apps, ConcurrentBag<AppEntity> updates, HttpRetryClient client)
        {
            var start = counter - MaxSteamStoreIdsPerRequest + 1;
            var requestUrl = new Uri(String.Format(SteamStoreApiUrlTemplate, String.Join(",", apps.Select(si => si.AppId))));
            CommonEventSource.Log.RetrieveStoreInformationStart(start, counter, total, requestUrl);

            var jObject = await SiteUtil.GetAsync<JObject>(client, requestUrl).ConfigureAwait(false);

            foreach (var app in apps)
            {
                var appInfo = jObject[app.AppId.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                string type = !appInfo.success || String.IsNullOrWhiteSpace(appInfo.data?.type)
                    ? AppEntity.UnknownType
                    : appInfo.data.type;

                if (app.AppType == type)
                {
                    CommonEventSource.Log.SkippedPopulatedApp(app.AppId, app.Name, type);
                    continue;
                }

                if (type == AppEntity.UnknownType)
                {
                    CommonEventSource.Log.PopulatingUnknownApp(app.AppId, app.Name);
                    updates.Add(new AppEntity(app.AppId, app.Name, AppEntity.UnknownType));
                    return;
                }

                PopulateApp(updates, appInfo, app, type);
            }

            CommonEventSource.Log.RetrieveStoreInformationStop(start, counter, total, requestUrl);
        }

        private static void PopulateApp(ConcurrentBag<AppEntity> updates, StoreAppInfo appInfo, BasicStoreInfo app, string type)
        {
            var platforms = GetPlatforms(appInfo);
            var categories = GetCategories(appInfo.data.categories);
            var genres = GetGenres(appInfo.data.genres);
            var publishers = GetDistinctListOrUnknown(appInfo.data.publishers);
            var developers = GetDistinctListOrUnknown(appInfo.data.developers);
            var releaseDate = GetReleaseDate(appInfo.data.release_date);
            var metaCriticScore = GetMetaCriticScore(appInfo.data.metacritic);

            if (ContainsAppGenre(genres))
            {
                type = AppEntity.AppTypeName;
            }
            else if (!categories.Contains("Single-player", StringComparer.OrdinalIgnoreCase))
            {
                type = AppEntity.MultiplayerOnlyTypeName;
            }

            CommonEventSource.Log.PopulateApp(
                app.AppId, app.Name, type, platforms, categories.ToFlatString(), genres.ToFlatString(), 
                publishers.ToFlatString(), developers.ToFlatString(), releaseDate.ToString(CultureInfo.InvariantCulture), metaCriticScore);

            updates.Add(new AppEntity(app.AppId, app.Name, type, platforms, categories, genres, publishers, developers, releaseDate, metaCriticScore));
        }

        private static bool ContainsAppGenre(IReadOnlyList<string> genres)
        {
            var currentAppGenres = new HashSet<string>(genres, StringComparer.OrdinalIgnoreCase);
            currentAppGenres.IntersectWith(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Audio Production",
                "Video Production",
                "Web Publishing",
                "Utilities",
                "Design & Illustration",
                "Animation & Modeling",
                "Accounting",
                "Education",
            });
            return currentAppGenres.Count > 0;
        }

        private static IReadOnlyList<string> GetGenres(IEnumerable<Genre> genres)
        {
            var nonGenres = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"Free to Play", "Early Access"};
            return genres == null
                ? AppEntity.UnknownList
                : GetDistinctListOrUnknown(genres.Select(g => g.description).Where(g => !nonGenres.Contains(g)));
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
            return metacritic?.score ?? AppEntity.UnknownScore;
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
        public int AppId { get; }
        public string Name { get; }
        public string AppType { get; }

        public BasicStoreInfo(int appId, string name, string appType)
        {
            AppType = appType;
            Name = name;
            AppId = appId;
        }
    }
}
