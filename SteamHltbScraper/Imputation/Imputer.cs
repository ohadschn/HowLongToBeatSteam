using System;
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
    public static class Imputer
    {
        internal const string ImputedCsvFileName = "imputed.csv";
        private static readonly string ApiKey = SiteUtil.GetMandatoryValueFromConfig("AzureMlImputeApiKey");
        private const string AzureMlImputeServiceBaseUrl = @"https://ussouthcentral.services.azureml.net/workspaces/379440377d144ed7b3e82e952cfc2819/services/59552aca10d942899334d43f70a5131a/jobs";
        private static readonly int AzureMlImputePollIntervalMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollIntervalMs", 1000);
        private static readonly int AzureMlImputePollTimeoutMs = SiteUtil.GetOptionalValueFromConfig("AzureMlImputePollTimeoutMs", 120 * 1000);

        internal static async Task Impute(IReadOnlyList<AppEntity> allApps, IReadOnlyList<AppEntity> updates)
        {
            HltbScraperEventSource.Log.ImputeStart();

            MarkImputed(updates);
            ZeroPreviouslyImputed(allApps.Except(updates));

            var notMissing = allApps.Where(a => !a.MainTtbImputed || !a.ExtrasTtbImputed || !a.CompletionistTtbImputed).ToArray();
            await ImputeCore(notMissing).ConfigureAwait(false);
            FillMissing(allApps, notMissing);

            HltbScraperEventSource.Log.ImputeStop();
        }

        private static void ZeroPreviouslyImputed(IEnumerable<AppEntity> previous)
        {
            foreach (var app in previous)
            {
                app.MainTtb = app.MainTtbImputed ? 0 : app.MainTtb;
                app.ExtrasTtb = app.ExtrasTtbImputed ? 0 : app.MainTtb;
                app.CompletionistTtb = app.CompletionistTtbImputed ? 0 : app.MainTtb;
            }
        }

        private static void FillMissing(IReadOnlyList<AppEntity> allApps, IReadOnlyList<AppEntity> notMissing)
        {
            int mainSum = 0;
            int extrasSum = 0;
            int completionistSum = 0;
            foreach (var app in notMissing)
            {
                mainSum += app.MainTtb;
                extrasSum += app.ExtrasTtb;
                completionistSum += app.CompletionistTtb;
            }

            int mainAvg = mainSum/notMissing.Count;
            int extrasAvg = extrasSum / notMissing.Count;
            int completionistAvg = completionistSum / notMissing.Count;
            foreach (var app in allApps.Except(notMissing)) //not not missing = missing
            {
                app.MainTtb = mainAvg;
                app.MainTtbImputed = true;
                app.ExtrasTtb = extrasAvg;
                app.ExtrasTtbImputed = true;
                app.CompletionistTtb = completionistAvg;
                app.CompletionistTtbImputed = true;
            }
        }

        private static async Task ImputeCore(IReadOnlyList<AppEntity> allApps)
        {
            HltbScraperEventSource.Log.CalculateImputationStart();

            string imputed = await InvokeImputationService(allApps).ConfigureAwait(false);

            //skip header row and discard blank lines
            var imputedRows = imputed
                .Split(new [] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) 
                .Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            Trace.Assert(allApps.Count == imputedRows.Length,
                String.Format(CultureInfo.InvariantCulture, "imputation count mismatch: expected {0}, actual {1}", allApps.Count, imputedRows.Length));
            
            for (int i = 0; i < allApps.Count; i++)
            {
                UpdateFromCsvRow(allApps[i], imputedRows[i]);
                FixImputationMiss(allApps[i]);
            }

            HltbScraperEventSource.Log.CalculateImputationStop();
        }

        private static async Task<string> InvokeImputationService(IReadOnlyList<AppEntity> allApps)
        {
            var blobPath = await UploadTtbInputToBlob(allApps).ConfigureAwait(false);

            using (var client = new HttpRetryClient(20))
            {
                client.DefaultRequestAuthorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                string jobId = await SubmitImputationJob(client, blobPath).ConfigureAwait(false);
                return await PollForImputeJobCompletion(client, AzureMlImputeServiceBaseUrl + "/" + jobId).ConfigureAwait(false);
            }
        }

        private static async Task<string> PollForImputeJobCompletion(HttpRetryClient client, string jobLocation)
        {
            HltbScraperEventSource.Log.PollImputationJobStatusStart();

            string imputed = null;
            var startTicks = Environment.TickCount;
            while (imputed == null && (Environment.TickCount - startTicks) < AzureMlImputePollTimeoutMs)
            {
                var status = await SiteUtil.GetAsync<BatchScoreStatus>(client, jobLocation).ConfigureAwait(false);
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

        private static async Task<string> SubmitImputationJob(HttpRetryClient client, string inputBlobPath)
        {
            var request = new BatchScoreRequest
            {
                Input = new AzureBlobDataReference
                {
                    ConnectionString = StorageHelper.AzureStorageConnectionString,
                    RelativeLocation = inputBlobPath,
                },
                GlobalParameters = new Dictionary<string, string>(),
            };

            HltbScraperEventSource.Log.SubmitImputationJobStart();
            string jobId;
            using (var response = await client.PostAsJsonAsync(AzureMlImputeServiceBaseUrl, request).ConfigureAwait(false))
            {
                jobId = await response.Content.ReadAsAsync<string>().ConfigureAwait(false);
            }
            HltbScraperEventSource.Log.SubmitImputationJobStop(jobId);

            return jobId;
        }

        private static async Task<string> UploadTtbInputToBlob(IReadOnlyList<AppEntity> allApps)
        {
            var csvString = "Main,Extras,Complete" + Environment.NewLine + string.Join(Environment.NewLine,
                allApps.Select(a => string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", a.MainTtb, a.ExtrasTtb, a.CompletionistTtb)));

            var blobName = String.Format(CultureInfo.InvariantCulture, "ttb-{0}.csv", SiteUtil.CurrentTimestamp);

            HltbScraperEventSource.Log.UploadTtbToBlobStart(blobName);

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference("jobdata");
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(csvString).ConfigureAwait(false);

            HltbScraperEventSource.Log.UploadTtbToBlobStop(blobName);

            return blob.Uri.LocalPath;
        }

        internal static string GetDataPath()
        {
            var dataPath = Environment.GetEnvironmentVariable("WEBJOBS_DATA_PATH");
            Trace.Assert(dataPath != null, "WEBJOBS_DATA_PATH environment variable undefined!");
            return dataPath;
        }

        private static void MarkImputed(IEnumerable<AppEntity> updates)
        {
            foreach (var app in updates)
            {
                app.MainTtbImputed = (app.MainTtb == 0);
                app.ExtrasTtbImputed = (app.ExtrasTtb == 0);
                app.CompletionistTtbImputed = (app.CompletionistTtb == 0);
            }
        }

        internal static void UpdateFromCsvRow(AppEntity appEntity, string row)
        {
            var ttbs = row.Split(',');
            Trace.Assert(ttbs.Length == 3);

            appEntity.MainTtb = GetRoundedValue(ttbs[0]);
            appEntity.ExtrasTtb = GetRoundedValue(ttbs[1]);
            appEntity.CompletionistTtb = GetRoundedValue(ttbs[2]);
        }

        private static void FixImputationMiss(AppEntity appEntity)
        {
            if (appEntity.MainTtb > appEntity.ExtrasTtb)
            {
                HltbScraperEventSource.Log.ImputationMiss("Main>Extras", appEntity.MainTtb, appEntity.ExtrasTtb);
                if (appEntity.MainTtbImputed)
                {
                    appEntity.MainTtb = appEntity.ExtrasTtb;
                }
                else //only extras imputed
                {
                    appEntity.ExtrasTtb = appEntity.MainTtb;
                }
            }

            if (appEntity.ExtrasTtb > appEntity.CompletionistTtb)
            {
                HltbScraperEventSource.Log.ImputationMiss("Extras>Completionist", appEntity.ExtrasTtb, appEntity.CompletionistTtb);
                if (appEntity.CompletionistTtbImputed)
                {
                    appEntity.CompletionistTtb = appEntity.ExtrasTtb;
                }
                else //only extras imputed
                {
                    appEntity.ExtrasTtb = appEntity.CompletionistTtb;
                }
            }
        }

        private static int GetRoundedValue(string value)
        {
            return (int)Math.Round(Double.Parse(value, CultureInfo.InvariantCulture));
        }
    }
}
