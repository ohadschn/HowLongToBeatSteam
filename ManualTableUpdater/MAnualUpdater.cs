using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json.Linq;

namespace ManualTableUpdater
{
    class ManualUpdater
    {
        private const string SteamStoreApiUrlTemplate = "http://store.steampowered.com/api/appdetails/?appids={0}";
        private const int MaxSteamStoreIdsPerRequest = 50;

        static void Main()
        {
            //var games = GetAppsFromCsv();
            GetAppsWithStoreData().Wait();

            Util.TraceInformation("All done!");
            Console.ReadLine();
        }

        private static async Task GetAppsWithStoreData()
        {
            var all = await TableHelper.GetAllApps(ae => ae);
            var allArray = all.ToArray();

            var updates = new List<AppEntity>();
            int counter = 0;

            await allArray.Partition(MaxSteamStoreIdsPerRequest).ForEachAsync(20, async partition =>
            {
                Interlocked.Add(ref counter, MaxSteamStoreIdsPerRequest);
                Util.TraceInformation("Getting store info for apps {0}-{1}", counter - MaxSteamStoreIdsPerRequest + 1, counter);
                await GetStoreInfo(partition, updates);
            });

            Util.TraceInformation("Deleting old entries...");
            await TableHelper.Delete(allArray);

            Util.TraceInformation("Inserting new entries...");
            await TableHelper.InsertOrReplace(updates);
        }

        private static async Task GetStoreInfo(IList<AppEntity> apps, ICollection<AppEntity> updates)
        {
            using (var client = new HttpClient())
            {
                var requestUri = String.Format(SteamStoreApiUrlTemplate, string.Join(",", apps.Select(ae => ae.SteamAppId)));

                Util.TraceInformation("Getting app store information from: {0}", requestUri);
                var response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var jObject = await response.Content.ReadAsAsync<JObject>();
                foreach (var app in apps)
                {
                    var appInfo = jObject[app.SteamAppId.ToString(CultureInfo.InvariantCulture)].ToObject<StoreAppInfo>();

                    string type;
                    if (!appInfo.success || appInfo.data == null)
                    {
                        Util.TraceWarning("Could not retrieve store information for {0} / {1}", app.SteamAppId, app.SteamName);
                        type = "Unknown";
                    }
                    else
                    {
                        type = appInfo.data.type;
                    }

                    Util.TraceInformation("Categorizing {0} / {1} as {2}", app.SteamAppId, app.SteamName, type);

                    updates.Add(new AppEntity(
                        app.SteamAppId, app.SteamName, type, 
                        app.HltbId, app.HltbName, app.MainTtb, app.ExtrasTtb, app.CompletionistTtb, app.CombinedTtb));
                }
            }
        }

        //public static void GetAppsFromCsv()
        //{
        //    var games = new List<AppEntity>();
        //    foreach (var line in File.ReadLines(@"steamHltb.csv"))
        //    {
        //        var parts = line.Split(',');
        //        if (parts.Length != 3)
        //        {
        //            continue; //too few to worry about these, we'll get them properly with the web job
        //        }

        //        string name = parts[0];
        //        int appId = int.Parse(parts[1]);
        //        int hltbId = int.Parse(parts[2]);

        //        games.Add(new AppEntity(appId, name, hltbId));
        //    }

        //    Util.TraceInformation("Updating...");
        //    TableHelper.InsertOrReplace(games);
        //}
    }

    // ReSharper disable InconsistentNaming
    public class Data
    {
        public string type { get; set; }
    }

    public class StoreAppInfo
    {
        public bool success { get; set; }
        public Data data { get; set; }
    }
    // ReSharper restore InconsistentNaming
}
