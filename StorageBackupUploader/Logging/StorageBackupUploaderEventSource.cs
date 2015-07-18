using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace StorageBackupUploader.Logging
{
    public class StorageBackupUploaderEventSource : EventSourceBase
    {
        public static readonly StorageBackupUploaderEventSource Log = new StorageBackupUploaderEventSource();
        private StorageBackupUploaderEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords StorageBackup = (EventKeywords)1;
            public const EventKeywords BlobStorage = (EventKeywords)16; //consistent with HltbScraperEventSource
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask BackupTableStorage = (EventTask) 1;
            public const EventTask SerializeApps = (EventTask) 2;
            public const EventTask CompressAppsData = (EventTask) 3;
            public const EventTask UploadAppBackupToBlobStorage = (EventTask) 4;
        }
        // ReSharper restore ConvertToStaticClass

        [Event(
            1,
            Message = "Start backing up table storage",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.BackupTableStorage,
            Opcode = EventOpcode.Start)]
        public void BackupTableStorageStart()
        {
            WriteEvent(1);
        }

        [Event(
            2,
            Message = "Finished backing up table storage - {0} app(s) backed up",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.BackupTableStorage,
            Opcode = EventOpcode.Stop)]
        public void BackupTableStorageStop(int appCount)
        {
            WriteEvent(2, appCount);
        }

        [Event(
            3,
            Message = "Start serializing {0} app(s) to file {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.SerializeApps,
            Opcode = EventOpcode.Start)]
        public void SerializeAppsStart(int appCount, string filename)
        {
            WriteEvent(3, appCount, filename);
        }

        [Event(
            4,
            Message = "Finished serializing {0} app(s) to file {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.SerializeApps,
            Opcode = EventOpcode.Stop)]
        public void SerializeAppsStop(int appCount, string filename)
        {
            WriteEvent(4, appCount, filename);
        }

        [Event(
            5,
            Message = "Start compressing app data {0} to {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.CompressAppsData,
            Opcode = EventOpcode.Start)]
        public void CompressAppsDataStart(string source, string destination)
        {
            WriteEvent(5, source, destination);
        }

        [Event(
            6,
            Message = "Finished compressing app data {0} to {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.CompressAppsData,
            Opcode = EventOpcode.Stop)]
        public void CompressAppsDataStop(string source, string destination)
        {
            WriteEvent(6, source, destination);
        }

        [Event(
            7,
            Message = "Start uploading app backup {0} to blob storage container {1}",
            Keywords = Keywords.StorageBackup | Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.UploadAppBackupToBlobStorage,
            Opcode = EventOpcode.Start)]
        public void UploadAppBackupToBlobStorageStart(string backup, string container)
        {
            WriteEvent(7, backup, container);
        }

        [Event(
            8,
            Message = "Finished uploading app backup {0} to blob storage container {1}",
            Keywords = Keywords.StorageBackup | Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.UploadAppBackupToBlobStorage,
            Opcode = EventOpcode.Stop)]
        public void UploadAppBackupToBlobStorageStop(string backup, string container)
        {
            WriteEvent(8, backup, container);
        }
    }
}
