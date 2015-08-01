using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Common.Storage;
using Common.Util;
using Microsoft.WindowsAzure.Storage;
using StorageBackupUploader.Logging;

namespace StorageBackupUploader.BackupUploader
{
    class BackupUploader
    {
        private static readonly int LogRetentionDays = SiteUtil.GetOptionalValueFromConfig("LogRetentionDays", 14);
        private static readonly string BackupBlobContainerName = SiteUtil.GetOptionalValueFromConfig("BackupBlobContainerName", "storagebackup");
        private static readonly int BackupUploaderStorageRetries = SiteUtil.GetOptionalValueFromConfig("BackupUploaderStorageRetries", 10);

        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("StorageBackupUploader");
                BackupTableStorage().Wait();
                DeleteOldLogEntries().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task DeleteOldLogEntries()
        {
            var ticks = Environment.TickCount;

            int deleteCount = await StorageHelper.DeleteOldEntities(StorageHelper.SlabLogsTableName, DateTime.UtcNow.AddDays(-LogRetentionDays), "log entries", BackupUploaderStorageRetries)
                .ConfigureAwait(false);

            await SiteUtil.SendSuccessMail("Old log deleter", deleteCount + " old logs deleted", ticks).ConfigureAwait(false);
        }

        private static async Task BackupTableStorage()
        {
            var ticks = Environment.TickCount;
            StorageBackupUploaderEventSource.Log.BackupTableStorageStart();

            var apps = await StorageHelper.GetAllApps().ConfigureAwait(false);
            
            var context = new OperationContext();            
            var appsSerialized = apps.Select(a => a.WriteEntity(context).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PropertyAsObject));

            var baseFilename = String.Format(CultureInfo.InvariantCulture, "apps-{0}-{1}", SiteUtil.CurrentTimestamp, Guid.NewGuid());
            var xmlFilename = Path.ChangeExtension(baseFilename, "xml");
            var sevenzipFilename = Path.ChangeExtension(baseFilename, "7z");

            SerializeAppsToFile(appsSerialized, xmlFilename, apps.Count);

            int exitCode = await CompressAppsData(xmlFilename, sevenzipFilename).ConfigureAwait(false);
            if (exitCode != 0)
            {
                throw new InvalidOperationException("Could not compress backup XML, 7-Zip exited with code: " + exitCode);
            }

            await UploadAppBackupToBlobStorage(sevenzipFilename, baseFilename).ConfigureAwait(false);

            await SiteUtil.SendSuccessMail("Storage Backup Uploader", apps.Count + " app(s) backed up", ticks)
                .ConfigureAwait(false);

            StorageBackupUploaderEventSource.Log.BackupTableStorageStop(apps.Count);
        }

        private static void SerializeAppsToFile(IEnumerable<Dictionary<string, object>> appsSerialized, string xmlFilename, int appCount)
        {
            StorageBackupUploaderEventSource.Log.SerializeAppsStart(appCount, xmlFilename);

            SiteUtil.DataContractSerializeToFile(appsSerialized, xmlFilename);

            StorageBackupUploaderEventSource.Log.SerializeAppsStop(appCount, xmlFilename);
        }

        private static async Task<int> CompressAppsData(string xmlFilename, string sevenzipFilename)
        {
            StorageBackupUploaderEventSource.Log.CompressAppsDataStart(xmlFilename, sevenzipFilename);

            var exitCode = await SiteUtil.RunProcessAsync("7za.exe",
                String.Format(CultureInfo.InvariantCulture, "a {0} {1}", sevenzipFilename, xmlFilename)).ConfigureAwait(false);

            StorageBackupUploaderEventSource.Log.CompressAppsDataStop(xmlFilename, sevenzipFilename);

            return exitCode;
        }

        private static async Task UploadAppBackupToBlobStorage(string sevenzipFilename, string baseFilename)
        {
            StorageBackupUploaderEventSource.Log.UploadAppBackupToBlobStorageStart(sevenzipFilename, BackupBlobContainerName);

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference(BackupBlobContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(baseFilename);
            await blob.UploadFromFileAsync(sevenzipFilename, FileMode.Open).ConfigureAwait(false);

            StorageBackupUploaderEventSource.Log.UploadAppBackupToBlobStorageStop(sevenzipFilename, BackupBlobContainerName);
        }
    }
}
