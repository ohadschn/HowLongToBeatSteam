using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Entities;
using Common.Storage;
using Common.Util;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using SteamHltbScraper.Logging;

namespace SteamHltbScraper.Imputation
{
    internal class TtbRatios
    {
        public double MainExtras { get; private set; }
        public double ExtrasCompletionist { get; private set; }
        public double ExtrasPlacement { get; private set; }

        public TtbRatios(double mainExtras, double extrasCompletionist, double extrasPlacement)
        {
            MainExtras = mainExtras;
            ExtrasCompletionist = extrasCompletionist;
            ExtrasPlacement = extrasPlacement;
        }
    }

    public static class Imputer
    {
        internal const string ImputedCsvFileName = "imputed.csv";
        public static string AllGenresId { get { return "All"; } }

        private static readonly string ApiKey = SiteUtil.GetMandatoryValueFromConfig("AzureMlImputeApiKey");
        private static readonly string AzureMlImputeServiceBaseUrl = SiteUtil.GetMandatoryValueFromConfig("AzureMlImputeServiceBaseUrl");
        private static readonly int AzureMlImputePollIntervalMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollIntervalMs", 1000);
        private static readonly int AzureMlImputePollTimeoutMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollTimeoutMs", 120 * 1000);
        private static readonly string BlobContainerName = SiteUtil.GetOptionalValueFromConfig("BlobContainerName", "jobdata");

        private static readonly int GenreStatsStorageRetries = SiteUtil.GetOptionalValueFromConfig("GenreStatsStorageRetries", 100);
        private static readonly int ImputationServiceRetries = SiteUtil.GetOptionalValueFromConfig("ImputationServiceRetries", 100);
        private static HttpRetryClient s_client;

        private static readonly ConcurrentDictionary<string, GenreStatsEntity> s_genreStats = new ConcurrentDictionary<string, GenreStatsEntity>();

        internal static async Task Impute(IReadOnlyList<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.ImputeStart();

            using (s_client = new HttpRetryClient(ImputationServiceRetries))
            {
                s_client.DefaultRequestAuthorization = new AuthenticationHeaderValue("Bearer", ApiKey);

                var gamesImputationTask = ImputeTypeGenres(allApps.Where(a => a.IsGame).ToArray(), "games");
                var dlcsImputationTask = ImputeTypeGenres(allApps.Where(a => !a.IsGame).ToArray(), "dlcs/mods");

                await Task.WhenAll(gamesImputationTask, dlcsImputationTask).ConfigureAwait(false);
            }

            HltbScraperEventSource.Log.UpdateGenreStatsStart(s_genreStats.Count);
            await StorageHelper.InsertOrReplace(s_genreStats.Values, GenreStatsStorageRetries, StorageHelper.GenreStatsTableName).ConfigureAwait(false);
            HltbScraperEventSource.Log.UpdateGenreStatsStop(s_genreStats.Count);

            HltbScraperEventSource.Log.ImputeStop();
        }

        private static async Task ImputeTypeGenres(IReadOnlyList<AppEntity> games, string gameType)
        {
            var gameTypeGenre = GenreStatsEntity.GetDecoratedGenre(AllGenresId, gameType);
            s_genreStats[gameTypeGenre] = new GenreStatsEntity(AllGenresId, gameType);
            var ratios = await ImputeGenreAndGetRatios(games, gameTypeGenre, null, true).ConfigureAwait(false);

            //we divide MaxConcurrentHttpRequests by 2 since both games and DLCs are imputed in parallel
            await games.GroupBy(a => a.Genres.First()).ForEachAsync(SiteUtil.MaxConcurrentHttpRequests / 2, async genreApps =>
            {
                var firstGenre = genreApps.Key;
                var decoratedGenre = GenreStatsEntity.GetDecoratedGenre(firstGenre, gameType);
                s_genreStats[decoratedGenre] = new GenreStatsEntity(firstGenre, gameType);
                await ImputeGenreAndGetRatios(genreApps.ToArray(), decoratedGenre, ratios, false).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static async Task<TtbRatios> ImputeGenreAndGetRatios(IReadOnlyList<AppEntity> apps, string genre, TtbRatios fallbackRatios, bool initial)
        {
            HltbScraperEventSource.Log.ImputeGenreStart(genre, apps.Count);

            var ratios = GetTtbRatios(apps, fallbackRatios);
            
            UpdateGenreStats(genre, ratios);
            await Impute(apps, genre, ratios, initial).ConfigureAwait(false);
            
            HltbScraperEventSource.Log.ImputeGenreStop(genre, apps.Count);
            return ratios;
        }

        private static TtbRatios GetTtbRatios(IReadOnlyCollection<AppEntity> apps, TtbRatios fallback)
        {
            var ratios = GetTtbRatiosCore(apps);

            const double tolerance = 0.0001;
            bool mainExtrasMissing = Math.Abs(ratios.MainExtras) < tolerance;
            bool extrasCompletionistMissing = Math.Abs(ratios.ExtrasCompletionist) < tolerance;
            bool extrasPlacementMissing = Math.Abs(ratios.ExtrasPlacement) < tolerance;

            if (fallback == null && (mainExtrasMissing || extrasCompletionistMissing || extrasPlacementMissing))
            {
                throw new InvalidOperationException("No record exists for which both main and extras (or extras and completionist) are present");
            }

            return new TtbRatios(
                mainExtrasMissing ? fallback.MainExtras : ratios.MainExtras,
                extrasCompletionistMissing ? fallback.ExtrasCompletionist : ratios.ExtrasCompletionist,
                extrasPlacementMissing ? fallback.ExtrasPlacement : ratios.ExtrasPlacement);
        }

        private static TtbRatios GetTtbRatiosCore(IReadOnlyCollection<AppEntity> apps)
        {
            var mainExtrasRatios = apps
                .Where(a => !a.MainTtbImputed && !a.ExtrasTtbImputed)
                .Select(a => (double)a.MainTtb / (double)a.ExtrasTtb)
                .ToArray();

            var extrasCompletionistRatios = apps
                .Where(a => !a.ExtrasTtbImputed && !a.CompletionistTtbImputed)
                .Select(a => (double)a.ExtrasTtb / (double)a.CompletionistTtb)
                .ToArray();

            var extrasPlacements = apps
                .Where(a => !a.MainTtbImputed && !a.ExtrasTtbImputed && !a.CompletionistTtbImputed && (a.MainTtb < a.CompletionistTtb))
                .Select(a => (double)(a.ExtrasTtb - a.MainTtb) / (double)(a.CompletionistTtb - a.MainTtb))
                .ToArray();

            return new TtbRatios(
                mainExtrasRatios.Length == 0 ? 0 : mainExtrasRatios.Average(),
                extrasCompletionistRatios.Length == 0 ? 0 : extrasCompletionistRatios.Average(),
                extrasPlacements.Length == 0 ? 0 : extrasPlacements.Average());
        }

        private static async Task Impute(IReadOnlyCollection<AppEntity> apps, string genre, TtbRatios ratios, bool initial)
        {
            var notCompletelyMissing = apps.Where(a => !a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed).ToArray();
            if (notCompletelyMissing.Length == 0)
            {
                if (initial)
                {
                    throw new InvalidOperationException("All TTBs are missing");
                }
                HltbScraperEventSource.Log.GenreHasNoTtbs(genre);

                //all apps are completely missing, so the current value in each of them is the game type average
                //we want the genre stats to use that average as well, so we'll just take the first (again, they are all the same)
                UpdateGenreStats(genre, apps.First().MainTtb, apps.First().ExtrasTtb, apps.First().CompletionistTtb);
                
                return;
            }

            try
            {
                await ImputeCore(notCompletelyMissing, ratios).ConfigureAwait(false);
            }
            catch (Exception)
            {
                HltbScraperEventSource.Log.ImputationError(genre);
                if (initial)
                {
                    throw;
                }
            }
            
            FillCompletelyMissing(genre, apps, notCompletelyMissing);
        }

        private static void FillCompletelyMissing(string genre, IEnumerable<AppEntity> allApps, IReadOnlyCollection<AppEntity> notCompletelyMissing)
        {
            Trace.Assert(notCompletelyMissing.Count > 0, "Empty notCompletelyMissing");

            //Calculate the average of imputed values
            int mainSum = 0;
            int extrasSum = 0;
            int completionistSum = 0;
            foreach (var app in notCompletelyMissing)
            {
                mainSum += app.MainTtb;
                extrasSum += app.ExtrasTtb;
                completionistSum += app.CompletionistTtb;
            }

            //Use the averages for the completely missing entries
            int mainAvg = mainSum/notCompletelyMissing.Count;
            int extrasAvg = extrasSum / notCompletelyMissing.Count;
            int completionistAvg = completionistSum / notCompletelyMissing.Count;

            UpdateGenreStats(genre, mainAvg, extrasAvg, completionistAvg);

            foreach (var app in allApps.Except(notCompletelyMissing)) //not not completely missing = completely missing
            {
                //HltbScraperEventSource.Log.SettingCompletelyMissingApp(app.SteamName, app.SteamAppId, mainAvg, extrasAvg, completionistAvg);
                app.SetMainTtb(mainAvg, true);
                app.SetExtrasTtb(extrasAvg, true);
                app.SetCompletionistTtb(completionistAvg, true);
            }
        }

        private static async Task ImputeCore(IReadOnlyList<AppEntity> notCompletelyMissing, TtbRatios ratios)
        {
            HltbScraperEventSource.Log.CalculateImputationStart(notCompletelyMissing.Count);

            string imputed = await InvokeImputationService(notCompletelyMissing).ConfigureAwait(false);

            var imputedRows = imputed
                .Split(new [] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) //skip header row
                .Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            Trace.Assert(notCompletelyMissing.Count == imputedRows.Length,
                String.Format(CultureInfo.InvariantCulture, "imputation count mismatch: expected {0}, actual {1}",
                    notCompletelyMissing.Count, imputedRows.Length));
            
            for (int i = 0; i < notCompletelyMissing.Count; i++)
            {
                UpdateFromCsvRow(notCompletelyMissing[i], imputedRows[i], ratios);
            }

            HltbScraperEventSource.Log.CalculateImputationStop(notCompletelyMissing.Count);
        }

        private static async Task<string> InvokeImputationService(IReadOnlyList<AppEntity> notCompletelyMissing)
        {
            var blobPath = await UploadTtbInputToBlob(notCompletelyMissing).ConfigureAwait(false);
            string jobId = await SubmitImputationJob(blobPath).ConfigureAwait(false);
            return await PollForImputeJobCompletion(AzureMlImputeServiceBaseUrl + "/" + jobId).ConfigureAwait(false);
        }

        private static async Task<string> PollForImputeJobCompletion(string jobLocation)
        {
            HltbScraperEventSource.Log.PollImputationJobStatusStart();

            string imputed = null;
            var startTicks = Environment.TickCount;
            while (imputed == null && (Environment.TickCount - startTicks) <AzureMlImputePollTimeoutMs)
            {
                var status = await SiteUtil.GetAsync<BatchScoreStatus>(s_client, jobLocation).ConfigureAwait(false);
                switch (status.StatusCode)
                {
                    case BatchScoreStatusCode.NotStarted:
                        HltbScraperEventSource.Log.ExpectedPollingStatusRetrieved(status.StatusCode.ToString());
                        break;
                    case BatchScoreStatusCode.Running:
                        HltbScraperEventSource.Log.ExpectedPollingStatusRetrieved(status.StatusCode.ToString());
                        break;
                    case BatchScoreStatusCode.Failed:
                        HltbScraperEventSource.Log.UnexpectedPollingStatusRetrieved(status.StatusCode.ToString(), status.Details);
                        throw new InvalidOperationException("Error executing imputation job:" + Environment.NewLine + status.Details);
                    case BatchScoreStatusCode.Cancelled:
                        HltbScraperEventSource.Log.UnexpectedPollingStatusRetrieved(status.StatusCode.ToString(), "job canceled");
                        throw new InvalidOperationException("Imputation job was unexpectedly canceled");
                    case BatchScoreStatusCode.Finished:
                        HltbScraperEventSource.Log.ExpectedPollingStatusRetrieved(status.StatusCode.ToString());
                        var credentials = new StorageCredentials(status.Result.SasBlobToken);
                        var cloudBlob = new CloudBlockBlob(new Uri(new Uri(status.Result.BaseLocation), status.Result.RelativeLocation), credentials);
                        imputed = await cloudBlob.DownloadTextAsync().ConfigureAwait(false);
                        break;
                }
                await Task.Delay(AzureMlImputePollIntervalMs).ConfigureAwait(false);
            }

            if (imputed == null)
            {
                throw new InvalidOperationException("Imputation job timed out");
            }
            HltbScraperEventSource.Log.PollImputationJobStatusStop();
            return imputed;
        }

        private static async Task<string> SubmitImputationJob(string inputBlobPath)
        {
            var request = new BatchScoreRequest
            {
                Input = new AzureBlobDataReference
                {
                    ConnectionString = StorageHelper.AzureStorageBlobConnectionString,
                    RelativeLocation = inputBlobPath,
                },
                GlobalParameters = new Dictionary<string, string>(),
            };

            HltbScraperEventSource.Log.SubmitImputationJobStart();
            string jobId;
            using (var response = await s_client.PostAsJsonAsync(AzureMlImputeServiceBaseUrl, request).ConfigureAwait(false))
            {
                jobId = await response.Content.ReadAsAsync<string>().ConfigureAwait(false);
            }
            HltbScraperEventSource.Log.SubmitImputationJobStop(jobId);

            return jobId;
        }

        private static async Task<string> UploadTtbInputToBlob(IReadOnlyList<AppEntity> notMissing)
        {
            var csvString = "Game,Main,Extras,Complete" + Environment.NewLine + string.Join(Environment.NewLine,
                notMissing.Select(a => string.Format(CultureInfo.InvariantCulture, "{0} ({1}),{2},{3},{4}",
                    a.SteamName.Replace(",", "-"), 
                    a.SteamAppId,
                    a.MainTtbImputed ? 0 : a.MainTtb,
                    a.ExtrasTtbImputed ? 0 : a.ExtrasTtb,
                    a.CompletionistTtbImputed ? 0 : a.CompletionistTtb)));

            var blobName = String.Format(CultureInfo.InvariantCulture, "ttb-{0}-{1}.csv", SiteUtil.CurrentTimestamp, Guid.NewGuid());

            HltbScraperEventSource.Log.UploadTtbToBlobStart(blobName);

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference(BlobContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(csvString).ConfigureAwait(false);

            HltbScraperEventSource.Log.UploadTtbToBlobStop(blobName);

            return blob.Uri.LocalPath;
        }

        internal static void UpdateFromCsvRow(AppEntity appEntity, string row, TtbRatios ratios)
        {
            var ttbs = row.Split(',');
            Trace.Assert(ttbs.Length == 3, "Invalid CSV row, contains more than 3 values: " + row);

            var imputedMain = GetRoundedValue(ttbs[0]);
            var imputedExtras = GetRoundedValue(ttbs[1]);
            var imputedCompletionist = GetRoundedValue(ttbs[2]);

            UpdateFromImputedValues(appEntity, imputedMain, imputedExtras, imputedCompletionist, ratios);
        }

        private static void UpdateFromImputedValues(
            AppEntity appEntity, int imputedMain, int imputedExtras, int imputedCompletionist, TtbRatios ratios)
        {
            HandleOverridenTtb(appEntity, "main", appEntity.MainTtb, appEntity.MainTtbImputed, ref imputedMain);
            HandleOverridenTtb(appEntity, "extras", appEntity.ExtrasTtb, appEntity.ExtrasTtbImputed, ref imputedExtras);
            HandleOverridenTtb(appEntity, "completionist", appEntity.CompletionistTtb, appEntity.CompletionistTtbImputed, ref imputedCompletionist);

            if (imputedMain == 0 || imputedExtras == 0 || imputedCompletionist == 0)
            {
                FixImputationZero(appEntity, ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
            }

            if (imputedMain > imputedExtras || imputedExtras > imputedCompletionist)
            {
                FixImputationMiss(appEntity, ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
            }

            appEntity.SetMainTtb(imputedMain, appEntity.MainTtbImputed);
            appEntity.SetExtrasTtb(imputedExtras, appEntity.ExtrasTtbImputed);
            appEntity.SetCompletionistTtb(imputedCompletionist, appEntity.CompletionistTtbImputed);
        }

        private static void FixImputationZero(AppEntity appEntity, TtbRatios ratios, ref int imputedMain, ref int imputedExtras, ref int imputedCompletionist)
        {
            HltbScraperEventSource.Log.ImputationProducedZeroTtb(
                appEntity.SteamName, appEntity.SteamAppId, imputedMain, imputedExtras, imputedCompletionist,
                appEntity.MainTtbImputed, appEntity.ExtrasTtbImputed, appEntity.CompletionistTtbImputed);

            Trace.Assert(imputedMain > 0 || imputedExtras > 0 || imputedCompletionist > 0, "all TTBs of a not completely missing app are zeroes");

            if (imputedMain == 0)
            {
                if (imputedExtras == 0)
                {
                    imputedExtras = (int) (imputedCompletionist*ratios.ExtrasCompletionist);
                }
                imputedMain = (int) (imputedExtras*ratios.MainExtras); //we know that imputedExtras is non-zero now
            }
            if (imputedExtras == 0)
            {
                imputedExtras = (int) (imputedMain/ratios.MainExtras); //we know imputedMain is non-zero now
            }
            if (imputedCompletionist == 0)
            {
                imputedCompletionist = (int) (imputedExtras/ratios.ExtrasCompletionist); //we know imputedExtras is non-zero now
            }
        }

        private static void FixImputationMiss(AppEntity appEntity, TtbRatios ratios, ref int imputedMain, ref int imputedExtras, ref int imputedCompletionist)
        {
            int originalImputedMain = imputedMain;
            int originalImputedExtras = imputedExtras;
            int originalImputedCompletionist = imputedCompletionist;

            bool imputationMiss = false;
            if (imputedMain > imputedExtras)
            {
                imputationMiss = true;
                if (appEntity.MainTtbImputed) //main imputed (possibly extras as well)
                {
                    imputedMain = (int) (imputedExtras*ratios.MainExtras);
                }
                else //extras imputed (main not imputed)
                {
                    imputedExtras = (int) (imputedMain/ratios.MainExtras);
                }
            }
            if (imputedExtras > imputedCompletionist)
            {
                imputationMiss = true;
                if (appEntity.CompletionistTtbImputed) //completionist imputed (possibly extras as well)
                {
                    imputedCompletionist = (int) (imputedExtras/ratios.ExtrasCompletionist);
                }
                else //extras imputed (completionist not imputed) - we'll use the extras placement to avoid reducing extras below main
                {
                    imputedExtras = (int) (imputedMain + ratios.ExtrasPlacement*(imputedCompletionist - imputedMain));
                }
            }

            if (imputationMiss)
            {
                LogImputationMiss(appEntity, originalImputedMain, originalImputedExtras, originalImputedCompletionist, 
                    imputedMain, imputedExtras, imputedCompletionist);
            }
        }

        private static void LogImputationMiss(
            AppEntity appEntity, int originalImputedMain, int originalImputedExtras, int originalImputedCompletionist, 
            int imputedMain, int imputedExtras, int imputedCompletionist)
        {
            HltbScraperEventSource.Log.ImputationMiss(
                appEntity.SteamName, appEntity.SteamAppId, originalImputedMain, originalImputedExtras, originalImputedCompletionist, 
                imputedMain, imputedExtras, imputedCompletionist, appEntity.MainTtbImputed, appEntity.ExtrasTtbImputed, appEntity.CompletionistTtbImputed);
        }

        private static void HandleOverridenTtb(AppEntity appEntity, string ttbType, int currentTtb, bool ttbImputed, ref int imputed)
        {           
            if (!ttbImputed && currentTtb != imputed)
            {
                HltbScraperEventSource.Log.ImputationOverrodeOriginalValue(appEntity.SteamAppId, appEntity.SteamName, ttbType, currentTtb, imputed);
                imputed = currentTtb;
            }
        }

        internal static int GetRoundedValue(string value)
        {
            return (int)Math.Round(Double.Parse(value, CultureInfo.InvariantCulture));
        }

        private static void UpdateGenreStats(string genre, int mainAvg, int extrasAvg, int completionistAvg)
        {
            var stats = s_genreStats[genre];
            stats.MainAverage = mainAvg;
            stats.ExtrasAverage = extrasAvg;
            stats.CompletionistAverage = completionistAvg;
        }

        private static void UpdateGenreStats(string genre, TtbRatios ratios)
        {
            var stats = s_genreStats[genre];
            stats.MainExtrasRatio = ratios.MainExtras;
            stats.ExtrasCompletionistRatio = ratios.ExtrasCompletionist;
            stats.ExtrasPlacementRatio = ratios.ExtrasPlacement;
        }
    }
}