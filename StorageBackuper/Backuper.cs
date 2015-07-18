using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using Common.Storage;
using Common.Util;
using Microsoft.WindowsAzure.Storage;

namespace StorageBackuper
{
    class Backuper
    {
        private const string BlobContainerName = "storagebackup";

        static void Main()
        {
            EventSource.SetCurrentThreadActivityId(Guid.NewGuid());
            try
            {
                SiteUtil.KeepWebJobAlive();
                SiteUtil.MockWebJobEnvironmentIfMissing("StorageBackuper");
                BackupTableStorage().Wait();
            }
            finally
            {
                EventSourceRegistrar.DisposeEventListeners();
            }
        }

        private static async Task BackupTableStorage()
        {
            var apps = await StorageHelper.GetAllApps();
            
            var context = new OperationContext();            
            var appsSerialized = apps.Select(a => a.WriteEntity(context).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PropertyAsObject));

            var baseFilename = String.Format(CultureInfo.InvariantCulture, "apps-{0}-{1}", SiteUtil.CurrentTimestamp, Guid.NewGuid());
            
            var xmlFilename = Path.ChangeExtension(baseFilename, "xml");
            SiteUtil.DataContractSerializeToFile(appsSerialized, xmlFilename);

            var sevenzipFilename = Path.ChangeExtension(baseFilename, "7z");
            var exitCode = await SiteUtil.RunProcessAsync("7za.exe", 
                String.Format(CultureInfo.InvariantCulture, "a {0} {1}", sevenzipFilename, xmlFilename)).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException("Could not compress backup XML, 7-Zip exited with code: " + exitCode);
            }

            var container = StorageHelper.GetCloudBlobClient(20).GetContainerReference(BlobContainerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = container.GetBlockBlobReference(baseFilename);
            await blob.UploadFromFileAsync(sevenzipFilename, FileMode.Open).ConfigureAwait(false);
        }
    }
}
