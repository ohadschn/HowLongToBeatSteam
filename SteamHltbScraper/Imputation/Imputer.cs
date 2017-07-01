using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
    public class TtbRatios
    {
        public double MainExtras { get; }
        public double ExtrasCompletionist { get; }
        public double ExtrasPlacement { get; }

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
        private const string GameTypeGames = "games";
        private const string GameTypeDlcsMods = "dlcs/mods";
        public static string AllGenresId => "All";

        private static readonly string AzureMlImputeServiceBaseUrl = SiteUtil.GetMandatoryValueFromConfig("AzureMlImputeServiceBaseUrl");
        private static readonly string ApiKey = SiteUtil.GetMandatoryCustomConnectionStringFromConfig("AzureMlImputeApiKey");
        private static readonly int AzureMlImputePollIntervalMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollIntervalMs", 1000);
        private static readonly int AzureMlImputePollTimeoutMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollTimeoutMs", 300 * 1000);

        private static readonly HashSet<string> KnownMissingGenres = 
            new HashSet<string>(SiteUtil.GetOptionalValueFromConfig("KnownMissingGenres", "Racing (dlcs/mods)").Split(',').Select(g => g.Trim()));

        private static readonly int NotCompletelyMissingThreshold = SiteUtil.GetOptionalValueFromConfig("NotCompletelyMissingThreshold", 100);
        private static readonly int ImputationThreshold = SiteUtil.GetOptionalValueFromConfig("ImputationThreshold", 70);
        private static readonly double InvalidTtbsThreshold = SiteUtil.GetOptionalValueFromConfig("InvalidTtbsThresholdPercent", 10)/100.0;
        private static readonly double ImputationMissThreshold = SiteUtil.GetOptionalValueFromConfig("ImputationMissThreshold", 15)/100.0;
        private static readonly double ImputationZerosThreshold = SiteUtil.GetOptionalValueFromConfig("ImputationZerosThreshold", 10)/100.0;
        private static readonly int GenreStatsStorageRetries = SiteUtil.GetOptionalValueFromConfig("GenreStatsStorageRetries", 100);
        private static readonly int ImputationServiceRetries = SiteUtil.GetOptionalValueFromConfig("ImputationServiceRetries", 100);
        private static HttpRetryClient s_client;

        private static readonly ConcurrentDictionary<string, GenreStatsEntity> s_genreStats = new ConcurrentDictionary<string, GenreStatsEntity>();

        public static async Task Impute(IReadOnlyList<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.ImputeStart();

            await ImputeByGenre(allApps).ConfigureAwait(false);

            HltbScraperEventSource.Log.UpdateGenreStatsStart(s_genreStats.Count);
            await StorageHelper.InsertOrReplace(s_genreStats.Values, "updating genre statistics", StorageHelper.GenreStatsTableName, GenreStatsStorageRetries).ConfigureAwait(false);
            HltbScraperEventSource.Log.UpdateGenreStatsStop(s_genreStats.Count);

            HltbScraperEventSource.Log.ImputeStop();
        }

        public static async Task ImputeByGenre(IReadOnlyList<AppEntity> allApps)
        {
            Sanitize(allApps);
            using (s_client = new HttpRetryClient(ImputationServiceRetries))
            {
                s_client.DefaultRequestAuthorization = new AuthenticationHeaderValue(HttpRetryClient.BearerAuthorizationScheme, ApiKey);

                var gamesImputationTask = ImputeTypeGenres(allApps.Where(a => a.IsGame).ToArray(), GameTypeGames);
                var dlcsImputationTask = ImputeTypeGenres(allApps.Where(a => !a.IsGame).ToArray(), GameTypeDlcsMods);

                await Task.WhenAll(gamesImputationTask, dlcsImputationTask).ConfigureAwait(false);
            }
        }

        // makes sure non-imputed values are ordered correctly (main <= extras <= completionist)
        internal static void Sanitize(IReadOnlyList<AppEntity> allApps)
        {
            int invalidTtbCount = 0;
            var ttbs = new List<int>(3);
            foreach (var app in allApps)
            {
                ttbs.Clear();

                if (!app.MainTtbImputed)
                {
                    ttbs.Add(app.MainTtb);
                }

                if (!app.ExtrasTtbImputed)
                {
                    ttbs.Add(app.ExtrasTtb);
                }

                if (!app.CompletionistTtbImputed)
                {
                    ttbs.Add(app.CompletionistTtb);
                }

                var originalTtbs = ttbs.ToArray();
                ttbs.Sort();
                if (!originalTtbs.SequenceEqual(ttbs))
                {
                    invalidTtbCount++;
                    int originalMain = app.MainTtb, originalExtras = app.ExtrasTtb, originlCompletionist = app.CompletionistTtb;

                    int i = 0;
                    if (!app.MainTtbImputed)
                    {
                        app.MainTtb = ttbs[i++];
                    }

                    if (!app.ExtrasTtbImputed)
                    {
                        app.ExtrasTtb = ttbs[i++];
                    }

                    if (!app.CompletionistTtbImputed)
                    {
                        app.CompletionistTtb = ttbs[i];
                    }

                    HltbScraperEventSource.Log.InvalidTtbsScraped(app.SteamName, app.HltbId, originalMain, originalExtras, originlCompletionist, 
                        app.MainTtbImputed, app.ExtrasTtbImputed, app.CompletionistTtbImputed, app.MainTtb, app.ExtrasTtb, app.CompletionistTtb);
                }
            }
            if (invalidTtbCount / (double)allApps.Count > InvalidTtbsThreshold)
            {
                HltbScraperEventSource.Log.TooManyInvalidTtbsScraped(invalidTtbCount, allApps.Count);
            }
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

            var ratios = GetTtbRatios(genre, apps, fallbackRatios);
            
            UpdateGenreStats(genre, ratios);
            await Impute(apps, genre, ratios, initial).ConfigureAwait(false);
            
            HltbScraperEventSource.Log.ImputeGenreStop(genre, apps.Count);
            return ratios;
        }

        private static TtbRatios GetTtbRatios(string genre, IReadOnlyCollection<AppEntity> apps, TtbRatios fallback)
        {
            var ratios = GetTtbRatiosCore(apps);

            const double tolerance = 0.0001;
            bool mainExtrasMissing = Math.Abs(ratios.MainExtras) < tolerance;
            bool extrasCompletionistMissing = Math.Abs(ratios.ExtrasCompletionist) < tolerance;
            bool extrasPlacementMissing = Math.Abs(ratios.ExtrasPlacement) < tolerance;

            if (fallback == null && (mainExtrasMissing || extrasCompletionistMissing || extrasPlacementMissing))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, 
                    "No record exists for game type '{0}' for which both main and extras (or extras and completionist) are present", genre));
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
            if (notCompletelyMissing.Length < ImputationThreshold)
            {
                if (initial)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                        "Insufficient amount of not completely missing games in game type '{0}': {1}", genre, notCompletelyMissing.Length));
                }

                if (notCompletelyMissing.Length == 0 && apps.Count > NotCompletelyMissingThreshold && !KnownMissingGenres.Contains(genre)) 
                {
                    //Detected probable scraping issue
                    HltbScraperEventSource.Log.GenreHasNoTtbs(genre);
                }

                //too few samples to say anything smart, we'll just take the average (there is always at least one app per genre)
                UpdateGenreStats(genre, (int)apps.Average(a => a.MainTtb), (int)apps.Average(a => a.ExtrasTtb), (int)apps.Average(a => a.CompletionistTtb));
                return;
            }
            
            try
            {
                await ImputeCore(genre, notCompletelyMissing, ratios).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HltbScraperEventSource.Log.ImputationError(genre, e.Message);
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

        private static async Task ImputeCore(string genre, IReadOnlyList<AppEntity> notCompletelyMissing, TtbRatios ratios)
        {
            HltbScraperEventSource.Log.CalculateImputationStart(genre, notCompletelyMissing.Count);

            string imputed = await InvokeImputationService(genre, notCompletelyMissing).ConfigureAwait(false);

            var imputedRows = imputed
                .Split(new [] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) //skip header row
                .Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            if (imputedRows.Length != notCompletelyMissing.Count)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "imputation count mismatch: expected {0}, actual {1}",
                    notCompletelyMissing.Count, imputedRows.Length));
            }

            int imputationZeroes = 0;
            int imputationMisses = 0;
            for (int i = 0; i < notCompletelyMissing.Count; i++)
            {
                bool imputationZero, imputationMiss;
                UpdateFromCsvRow(notCompletelyMissing[i], imputedRows[i], ratios, out imputationZero, out imputationMiss);
                if (imputationMiss)
                {
                    imputationMisses++;
                }
                if (imputationZero)
                {
                    imputationZeroes++;
                }
            }

            if ((double) imputationZeroes/notCompletelyMissing.Count > ImputationZerosThreshold)
            {
                HltbScraperEventSource.Log.ImputationProducedTooManyZeroTtbs(genre, imputationZeroes, notCompletelyMissing.Count);
            }
            if ((double) imputationMisses/notCompletelyMissing.Count > ImputationMissThreshold)
            {
                HltbScraperEventSource.Log.ImputationProducedTooManyMisses(genre, imputationMisses, notCompletelyMissing.Count);
            }

            HltbScraperEventSource.Log.CalculateImputationStop(genre, notCompletelyMissing.Count);
        }

        private static async Task<string> InvokeImputationService(string genre, IReadOnlyList<AppEntity> notCompletelyMissing)
        {
            var blobPath = await UploadTtbInputToBlob(genre, notCompletelyMissing).ConfigureAwait(false);
            string jobId = await SubmitImputationJob(blobPath).ConfigureAwait(false);
            return await PollForImputeJobCompletion(AzureMlImputeServiceBaseUrl + "/" + jobId).ConfigureAwait(false);
        }

        private static async Task<string> PollForImputeJobCompletion(string jobLocation)
        {
            HltbScraperEventSource.Log.PollImputationJobStatusStart();

            string imputed = null;
            var startTicks = Environment.TickCount;
            while (imputed == null && (Environment.TickCount - startTicks) < AzureMlImputePollTimeoutMs)
            {
                using (var statusResponse = await s_client.GetAsync<BatchScoreStatus>(jobLocation).ConfigureAwait(false))
                {
                    var status = statusResponse.Content;
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
            using (var response = await s_client.PostAsJsonAsync<BatchScoreRequest, string>(AzureMlImputeServiceBaseUrl, request).ConfigureAwait(false))
            {
                string jobId = response.Content;
                HltbScraperEventSource.Log.SubmitImputationJobStop(jobId);
                return jobId;
            }
        }

        private static async Task<string> UploadTtbInputToBlob(string genre, IReadOnlyList<AppEntity> notMissing)
        {
            var csvString = "Game,Main,Extras,Complete" + Environment.NewLine + string.Join(Environment.NewLine,
                notMissing.Select(a => string.Format(CultureInfo.InvariantCulture, "{0} ({1}),{2},{3},{4}",
                    a.SteamName.Replace(",", "-"), 
                    a.SteamAppId,
                    a.MainTtbImputed ? 0 : a.MainTtb,
                    a.ExtrasTtbImputed ? 0 : a.ExtrasTtb,
                    a.CompletionistTtbImputed ? 0 : a.CompletionistTtb)));

            var blobName = $"{genre}-{SiteUtil.CurrentTimestamp}-{Guid.NewGuid()}.csv";

            HltbScraperEventSource.Log.UploadTtbToBlobStart(blobName);

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference(StorageHelper.JobDataBlobContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(csvString).ConfigureAwait(false);

            HltbScraperEventSource.Log.UploadTtbToBlobStop(blobName);

            return blob.Uri.LocalPath;
        }

        internal static void UpdateFromCsvRow(AppEntity appEntity, string row, TtbRatios ratios, out bool imputationZero, out bool imputationMiss)
        {
            var ttbs = row.Split(',');
            if (ttbs.Length != 3)
            {
                throw new InvalidOperationException("Invalid CSV row, contains more than 3 values: " + row);
            }

            var imputedMain = GetRoundedValue(ttbs[0]);
            var imputedExtras = GetRoundedValue(ttbs[1]);
            var imputedCompletionist = GetRoundedValue(ttbs[2]);

            UpdateFromImputedValues(appEntity, imputedMain, imputedExtras, imputedCompletionist, ratios, out imputationZero, out imputationMiss);
        }

        private static void UpdateFromImputedValues(
            AppEntity appEntity, int imputedMain, int imputedExtras, int imputedCompletionist, TtbRatios ratios, out bool imputationZero, out bool imputationMiss)
        {
            HandleOverridenTtb(appEntity, "main", appEntity.MainTtb, appEntity.MainTtbImputed, ref imputedMain);
            HandleOverridenTtb(appEntity, "extras", appEntity.ExtrasTtb, appEntity.ExtrasTtbImputed, ref imputedExtras);
            HandleOverridenTtb(appEntity, "completionist", appEntity.CompletionistTtb, appEntity.CompletionistTtbImputed, ref imputedCompletionist);

            if (imputedMain == 0 || imputedExtras == 0 || imputedCompletionist == 0)
            {
                imputationZero = true;
                FixImputationZeroes(appEntity, ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
            }
            else
            {
                imputationZero = false;
            }

            if (imputedMain > imputedExtras || imputedExtras > imputedCompletionist)
            {
                imputationMiss = true;
                FixImputationMiss(appEntity, ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
            }
            else
            {
                imputationMiss = false;
            }

            appEntity.FixTtbs(imputedMain, imputedExtras, imputedCompletionist);
        }

        private static void FixImputationZeroes(AppEntity appEntity, TtbRatios ratios, ref int imputedMain, ref int imputedExtras, ref int imputedCompletionist)
        {
            HltbScraperEventSource.Log.ImputationProducedZeroTtb(
                appEntity.SteamName, appEntity.SteamAppId, imputedMain, imputedExtras, imputedCompletionist,
                appEntity.MainTtbImputed, appEntity.ExtrasTtbImputed, appEntity.CompletionistTtbImputed);

            if (imputedMain == 0 && imputedExtras == 0 && imputedCompletionist == 0)
            {
                throw new InvalidOperationException("all TTBs of a not completely missing app are zeroes: " + appEntity.SteamAppId);
            }

            FixTtbZeroes(ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
        }

        private static void FixTtbZeroes(TtbRatios ratios, ref int mainTtb, ref int extrasTtb, ref int completionistTtb)
        {
            //May result in invalid TTBs, so generally FixInvalidTtbs should be used as well
            if (mainTtb == 0)
            {
                if (extrasTtb == 0)
                {
                    extrasTtb = CalculateTtbFromRatio(completionistTtb, ratios.ExtrasCompletionist);
                }
                mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.MainExtras); //we know that extrasTtb is non-zero now
            }
            if (extrasTtb == 0)
            {
                extrasTtb = CalculateTtbFromRatio(mainTtb, 1/ratios.MainExtras); //we know mainTtb is non-zero now
            }
            if (completionistTtb == 0)
            {
                completionistTtb = CalculateTtbFromRatio(extrasTtb, 1/ratios.ExtrasCompletionist); //we know extrasTtb is non-zero now
            }
        }

        private static void FixImputationMiss(AppEntity appEntity, TtbRatios ratios, ref int imputedMain, ref int imputedExtras, ref int imputedCompletionist)
        {
            int originalImputedMain = imputedMain;
            int originalImputedExtras = imputedExtras;
            int originalImputedCompletionist = imputedCompletionist;

            FixInvalidTtbs(appEntity, ratios, ref imputedMain, ref imputedExtras, ref imputedCompletionist);
            
            HltbScraperEventSource.Log.ImputationMiss(
                appEntity.SteamName, appEntity.SteamAppId, originalImputedMain, originalImputedExtras, originalImputedCompletionist,
                imputedMain, imputedExtras, imputedCompletionist, appEntity.MainTtbImputed, appEntity.ExtrasTtbImputed,
                appEntity.CompletionistTtbImputed);
        }

        private static void FixInvalidTtbs(AppEntity appEntity, TtbRatios ratios, ref int mainTtb, ref int extrasTtb, ref int completionistTtb)
        {
            //recall that Sanitize made sure that non-imputed values were ordered correctly (main <= extras <= completionist)
            if (mainTtb > extrasTtb && extrasTtb > completionistTtb)
            {
                if (!appEntity.MainTtbImputed)
                {
                    //main is not imputed, which means both extras and completionist must be imputed and therefore fixed
                    extrasTtb = CalculateTtbFromRatio(mainTtb, 1 / ratios.MainExtras);
                    completionistTtb = CalculateTtbFromRatio(extrasTtb, 1 / ratios.ExtrasCompletionist);
                }
                else if (!appEntity.ExtrasTtbImputed)
                {
                    //extras is not imputed, which means both main and completionist must be imputed and therefore fixed
                    mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.MainExtras);
                    completionistTtb = CalculateTtbFromRatio(extrasTtb, 1 / ratios.ExtrasCompletionist);
                }
                else //!appEntity.CompletionistTtbImputed
                {
                    //completionist is not imputed, which means both main and extras must be imputed and therefore fixed
                    extrasTtb = CalculateTtbFromRatio(completionistTtb, ratios.ExtrasCompletionist);
                    mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.MainExtras);
                }
            }
            else if (mainTtb > extrasTtb && extrasTtb <= completionistTtb && mainTtb > completionistTtb)
            {
                if (appEntity.MainTtbImputed)
                {
                    //main is larger than both extras and completionist, therefore needs to be fixed
                    mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.ExtrasCompletionist);
                }
                else
                {
                    //main is not imputed and is bigger than both extras and completionist, therefore they must both be imputed and need to be fixed
                    extrasTtb = CalculateTtbFromRatio(mainTtb, 1 / ratios.MainExtras);
                    completionistTtb = CalculateTtbFromRatio(extrasTtb, 1 / ratios.ExtrasCompletionist);
                }
            }
            else if (mainTtb > extrasTtb && extrasTtb <= completionistTtb && mainTtb <= completionistTtb)
            {
                if (appEntity.MainTtbImputed)
                {
                    //main is imputed and extras <= completionist, so we'll just fix main
                    mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.MainExtras);
                }
                else
                {
                    //main is not imputed and is larger than extras, meaning extras must be imputed
                    extrasTtb = (int)(mainTtb + ratios.ExtrasPlacement * (completionistTtb - mainTtb));
                }
            }
            else if (mainTtb <= extrasTtb && extrasTtb > completionistTtb && mainTtb > completionistTtb)
            {
                if (appEntity.CompletionistTtbImputed)
                {
                    //completionist is imputed and main <= extras, so we'll just fix completionist
                    completionistTtb = CalculateTtbFromRatio(extrasTtb, 1 / ratios.ExtrasCompletionist);
                }
                else
                {
                    //completionist is not imputed but both main and extras are larger than it, which means both are imputed and need to be fixed
                    extrasTtb = CalculateTtbFromRatio(completionistTtb, ratios.ExtrasCompletionist);
                    mainTtb = CalculateTtbFromRatio(extrasTtb, ratios.MainExtras);
                }
            }
            else // (mainTtb <= extrasTtb && extrasTtb > completionistTtb && mainTtb <= completionistTtb)
            {
                if (appEntity.CompletionistTtbImputed)
                {
                    //completionist is imputed and main <= extras, so we'll just fix completionist
                    completionistTtb = CalculateTtbFromRatio(extrasTtb, 1 / ratios.ExtrasCompletionist);
                }
                else
                {
                    //completionist is not imputed and smaller than extras, therefore extras must be fixed
                    extrasTtb = (int)(mainTtb + ratios.ExtrasPlacement * (completionistTtb - mainTtb));
                }
            }
        }

        private static int CalculateTtbFromRatio(int referenceTtb, double ratio)
        {
            int calculatedTtb = (int)(referenceTtb * ratio);
            return calculatedTtb == 0 ? 1 : calculatedTtb;
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