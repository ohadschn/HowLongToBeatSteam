using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Logging;

namespace StorageBackupUploader.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-BackupUploader")]
    public sealed class BackupUploaderEventSource : EventSourceBase
    {
        public static readonly BackupUploaderEventSource Log = new BackupUploaderEventSource();
        private BackupUploaderEventSource()
        {
        }

        // ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() { }
            public const EventKeywords StorageBackup = (EventKeywords) 1;
            public const EventKeywords StorageCleanup = (EventKeywords) 2;
            public const EventKeywords TableStorage = (EventKeywords) 4; //consistent with SiteEventSource
            public const EventKeywords BlobStorage = (EventKeywords) 16; //consistent with HltbScraperEventSource
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask BackupTableStorage = (EventTask) 1;
            public const EventTask SerializeApps = (EventTask) 2;
            public const EventTask CompressAppsData = (EventTask) 3;
            public const EventTask UploadAppBackupToBlobStorage = (EventTask) 4;
            public const EventTask DeleteOldLogEntries = (EventTask) 5;
            public const EventTask DeleteOldBlobs = (EventTask) 6;
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
            Message = "Start serializing {0} app(s) to file: {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.SerializeApps,
            Opcode = EventOpcode.Start)]
        public void SerializeAppsStart(int appCount, string fileName)
        {
            WriteEvent(3, appCount, fileName);
        }

        [Event(
            4,
            Message = "Finished serializing {0} app(s) to file {1}",
            Keywords = Keywords.StorageBackup,
            Level = EventLevel.Informational,
            Task = Tasks.SerializeApps,
            Opcode = EventOpcode.Stop)]
        public void SerializeAppsStop(int appCount, string fileName)
        {
            WriteEvent(4, appCount, fileName);
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

        [Event(
            9,
            Message = "Start deleting old log entries",
            Keywords = Keywords.StorageCleanup | Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldLogEntries,
            Opcode = EventOpcode.Start)]
        public void DeleteOldLogEntriesStart()
        {
            WriteEvent(9);
        }

        [Event(
            10,
            Message = "Finished deleting old log entries ({0} deleted)",
            Keywords = Keywords.StorageCleanup | Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldLogEntries,
            Opcode = EventOpcode.Stop)]
        public void DeleteOldLogEntriesStop(int deleteCount)
        {
            WriteEvent(10, deleteCount);
        }

        [Event(
            11,
            Message = "Start deleting old blobs",
            Keywords = Keywords.StorageCleanup | Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldBlobs,
            Opcode = EventOpcode.Start)]
        public void DeleteOldBlobsStart()
        {
            WriteEvent(11);
        }

        [Event(
            12,
            Message = "Finished deleting old blobs ({0} backup + {1} job data blobs deleted)",
            Keywords = Keywords.StorageCleanup | Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldBlobs,
            Opcode = EventOpcode.Stop)]
        public void DeleteOldBlobsStop(int backupBlobsDeleted, int jobDataBlobsDeleted)
        {
            WriteEvent(12, backupBlobsDeleted, jobDataBlobsDeleted);
        }
    }
}
