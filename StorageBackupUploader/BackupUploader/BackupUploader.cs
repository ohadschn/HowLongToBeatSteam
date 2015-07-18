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
        private const string BlobContainerName = "storagebackup";

        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("StorageBackupUploader");
                BackupTableStorage().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task BackupTableStorage()
        {
            var ticks = Environment.TickCount;
            StorageBackupUploaderEventSource.Log.BackupTableStorageStart();

            var apps = await StorageHelper.GetAllApps();
            
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

            await SiteUtil.SendSuccessMail("Storage Backup Uploader", SiteUtil.GetTimeElapsedFromTickCount(ticks), apps.Count + " app(s) backed up")
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
            StorageBackupUploaderEventSource.Log.UploadAppBackupToBlobStorageStart(sevenzipFilename, BlobContainerName);

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference(BlobContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(baseFilename);
            await blob.UploadFromFileAsync(sevenzipFilename, FileMode.Open).ConfigureAwait(false);

            StorageBackupUploaderEventSource.Log.UploadAppBackupToBlobStorageStop(sevenzipFilename, BlobContainerName);
        }
    }
}
