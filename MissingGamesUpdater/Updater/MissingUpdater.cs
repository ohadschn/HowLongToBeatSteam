﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Common.Entities;
using Common.Logging;
using Common.Storage;
using Common.Store;
using Common.Util;
using MissingGamesUpdater.Logging;
using SteamHltbScraper.Imputation;
using SteamHltbScraper.Scraper;

namespace MissingGamesUpdater.Updater
{
    internal static class MissingUpdater
    {
        private static readonly int SteamApiRetries = SiteUtil.GetOptionalValueFromConfig("MissingUpdaterSteamApiRetries", 100);
        private static readonly int StorageRetries = SiteUtil.GetOptionalValueFromConfig("MissingUpdaterStorageRetries", 10);
        private static readonly int UpdateLimit = SiteUtil.GetOptionalValueFromConfig("MissingUpdateLimit", int.MaxValue);
        [SuppressMessage("Sonar.CodeSmell", "S1075:URIsShouldNotBeHardcoded", Justification = "Steam API")]
        private static readonly Uri GetSteamAppListUrl = new Uri("http://api.steampowered.com/ISteamApps/GetAppList/v0001/");
        private static HttpRetryClient s_client;

        private static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("MissingUpdater");
                SiteUtil.SetDefaultConnectionLimit();
                using (s_client = new HttpRetryClient(SteamApiRetries))
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
            var tickCount = Environment.TickCount;
            MissingUpdaterEventSource.Log.UpdateMissingGamesStart();

            var allSteamAppsTask = GetAllSteamApps(s_client);
            var allKnownAppsTask = StorageHelper.GetAllApps(null, StorageRetries);
            
            await Task.WhenAll(allSteamAppsTask, allKnownAppsTask).ConfigureAwait(false);

            var allSteamApps = allSteamAppsTask.Result;
            var allKnownApps = allKnownAppsTask.Result;

            var knownSteamIdsHash = new HashSet<int>(allKnownApps.Select(ae => ae.SteamAppId));
            var missingApps = allSteamApps.Where(a => !knownSteamIdsHash.Contains(a.appid)).Take(UpdateLimit).ToArray();

            MissingUpdaterEventSource.Log.MissingAppsDetermined(missingApps);

            var updates = new ConcurrentBag<AppEntity>();
            InvalidOperationException ioe = null;
            try
            {
                await SteamStoreHelper
                    .GetStoreInformationUpdates(missingApps.Select(a => new BasicStoreInfo(a.appid, a.name, null)).ToArray(), s_client, updates)
                    .ConfigureAwait(false);
            }
            catch(Exception e)
            {
                ioe = new InvalidOperationException("Could not retrieve store information for all games", e);
            }

            var measuredUpdates = updates.Where(a => a.Measured).ToArray();
            if (measuredUpdates.Length > 0)
            {
                await HltbScraper.ScrapeHltb(measuredUpdates).ConfigureAwait(false);

                //re-impute for measured updates
                await Imputer.Impute(allKnownApps.Where(a => a.Measured).Concat(measuredUpdates).ToArray()).ConfigureAwait(false); 
            }

            //we're inserting new entries, no fear of collisions (even if two jobs overlap the next one will fix it)
            await StorageHelper.Insert(updates, "updating missing games", StorageHelper.SteamToHltbTableName, StorageRetries).ConfigureAwait(false);     

            if (ioe != null)
            {
                throw ioe; //fail job
            }

            await SiteUtil.SendSuccessMail("Missing updater", updates.Count + " app(s) added", tickCount).ConfigureAwait(false);
            MissingUpdaterEventSource.Log.UpdateMissingGamesStop();
        }

        internal static async Task<IList<App>> GetAllSteamApps(HttpRetryClient client)
        {
            MissingUpdaterEventSource.Log.RetrieveAllSteamAppsStart(GetSteamAppListUrl);

            using (var allGamesRoot = await client.GetAsync<AllGamesRoot>(GetSteamAppListUrl).ConfigureAwait(false))
            {
                MissingUpdaterEventSource.Log.RetrieveAllSteamAppsStop(GetSteamAppListUrl);

                if (allGamesRoot.Content?.applist?.apps?.app == null)
                {
                    MissingUpdaterEventSource.Log.ErrorRetrievingAllSteamApps(GetSteamAppListUrl);
                    throw new InvalidOperationException("Invalid response from " + GetSteamAppListUrl);
                }

                var apps = allGamesRoot.Content.applist.apps.app;
                MissingUpdaterEventSource.Log.RetrievedAllSteamApps(GetSteamAppListUrl, apps.Count);
                return apps;
            }
        }
    }
}