using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Net;
using Common.Entities;
using Common.Storage;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Logging
{
    [EventSource(Name = "OS-HowLongToBeatSteam-Common")]
    public sealed class CommonEventSource : EventSourceBase
    {
        public static readonly CommonEventSource Log = new CommonEventSource();
        private CommonEventSource()
        {
        }

// ReSharper disable ConvertToStaticClass
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() {}
            public const EventKeywords Http = (EventKeywords)1;
            public const EventKeywords StoreApi = (EventKeywords)2;
            public const EventKeywords TableStorage = (EventKeywords)4;
            public const EventKeywords Email = (EventKeywords) 8;
            public const EventKeywords BlobStorage = (EventKeywords)16; //consistent with HltbScraperEventSource
            public const EventKeywords Shell = (EventKeywords) 32;
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask RetrieveMissingStoreInformation = (EventTask)1;
            public const EventTask RetrieveStoreInformation = (EventTask)2;
            public const EventTask QueryAllEntities = (EventTask)3;
            public const EventTask RetrievePartitionBatchMappings = (EventTask)4;
            public const EventTask RetrieveOldLogEntries = (EventTask)5;
            public const EventTask ExecuteOperations = (EventTask)6;
            public const EventTask ExecutePartitionBatchOperation = (EventTask)7;
            public const EventTask InsertSuggestion = (EventTask)8;
            public const EventTask DeleteSuggestion = (EventTask) 9;
            public const EventTask AcceptSuggestion = (EventTask) 10;
            public const EventTask SendSuccessMail = (EventTask)11;
            public const EventTask RunProcess = (EventTask)12;
            public const EventTask DeleteOldEntities = (EventTask)13;
            public const EventTask ProcessContainerBlobs = (EventTask) 14;
            public const EventTask RetrieveContainerBlobBatch = (EventTask)15;
            public const EventTask ProcessContainerBlobBatch = (EventTask)16;
        }
// ReSharper restore ConvertToStaticClass

        [NonEvent]
        public void HttpRequestFailed(Uri uri, Exception exception, int attempt, int totalRetries, TimeSpan delay)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (!IsEnabled())
            {
                return;
            }

            HttpRequestFailed(uri.ToString(), String.Format(CultureInfo.InvariantCulture, "{0} / {1}", exception.GetType(), exception.Message), 
                attempt, totalRetries, delay.TotalSeconds);
        }

        [Event(
            1,
            Message = "Request to URI {0} failed due to: {1} - retrying attempt #{2} / {3} will take place in {4} seconds",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void HttpRequestFailed(string uri, string exceptionMessage, int attempt, int totalRetries, double delaySeconds)
        {
            WriteEvent(1, uri, exceptionMessage, attempt, totalRetries, delaySeconds);
        }

        [Event(
            2,
            Message = "Start retrieving Steam store information for missing apps",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveMissingStoreInformation,
            Opcode = EventOpcode.Start)]
        public void RetrieveMissingStoreInformationStart()
        {
            WriteEvent(2);
        }

        [Event(
            3,
            Message = "Finished retrieving Steam store information for missing apps",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveMissingStoreInformation,
            Opcode = EventOpcode.Stop)]
        public void RetrieveMissingStoreInformationStop()
        {
            WriteEvent(3);
        }

        [NonEvent]
        public void RetrieveStoreInformationStart(int startIndex, int endIndex, int total, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStart(uri.ToString(), startIndex, endIndex, total);
        }

        [Event(
            4,
            Message = "Start retrieving Steam store info for apps #{1}-#{2} / {3} from: {0}",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveStoreInformation,
            Opcode = EventOpcode.Start)]
        private void RetrieveStoreInformationStart(string uri, int start, int end, int total)
        {
            WriteEvent(4, uri, start, end, total);
        }

        [NonEvent]
        public void RetrieveStoreInformationStop(int startIndex, int endIndex, int total, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStop(uri.ToString(), startIndex, endIndex, total);
        }

        [Event(
            5,
            Message = "Finished retrieving Steam store info for apps #{1}-#{2} / {3} from: {0}",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveStoreInformation,
            Opcode = EventOpcode.Stop)]
        private void RetrieveStoreInformationStop(string uri, int start, int end, int total)
        {
            WriteEvent(5, uri, start, end, total);
        }

        [Event(
            6,
            Message = "Skipping already categorized app {0} / {1} ({2})",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational)]
        public void SkippedPopulatedApp(int appId, string name, string type)
        {
            WriteEvent(6, appId, name, type);
        }

        [Event(
            7,
            Message = "Categorizing {0} / {1} as 'Unknown'",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational)]
        public void PopulatingUnknownApp(int appId, string name)
        {
            WriteEvent(7, appId, name);
        }

        [Event(
            107,
            Message = "Categorizing {0} / {1} as '{2}'. Platforms: {3}. Categories: {4}. Genres: {5}. Publishers: {6}. Developers: {7}. Release date: {8}. MetaCritic score: {9}",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational)]
        public void PopulateApp(int appId, string name, string type, Platforms platforms, string categories, string genres, 
            string publishers, string developers, string releaseDate, int metacriticScore)
        {
            WriteEvent(107, appId, name, type, (int)platforms, categories, genres, publishers, developers, releaseDate, metacriticScore);
        }

        [Event(
            8,
            Message = "Start querying table storage for all {0} with filter: [{1}]",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllEntities,
            Opcode = EventOpcode.Start)]
        public void QueryAllEntitiesStart(string entitiesType, string rowFilter)
        {
            WriteEvent(8, entitiesType, rowFilter);
        }

        [Event(
            9,
            Message = "Finished querying table storage for all {0} with filter: [{1}] (count: {2})",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllEntities,
            Opcode = EventOpcode.Stop)]
        public void QueryAllEntitiesStop(string entitiesType, string rowFilter, int count)
        {
            WriteEvent(9, entitiesType, rowFilter, count);
        }

        [Event(
            10,
            Message = "Start retrieving mappings for partition {0} / batch {1} from table storage",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrievePartitionBatchMappings,
            Opcode = EventOpcode.Start)]
        public void RetrievePartitionBatchMappingsStart(string partition, int batch)
        {
            WriteEvent(10, partition, batch);
        }

        [Event(
            11,
            Message = "Finished retrieving mappings for partition {0} / batch {1} from table storage",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrievePartitionBatchMappings,
            Opcode = EventOpcode.Stop)]
        public void RetrievePartitionBatchMappingsStop(string partition, int batch)
        {
            WriteEvent(11, partition, batch);
        }

        [Event(
            100,
            Message = "Start deleting old entities of type {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldEntities,
            Opcode = EventOpcode.Start)]
        public void DeleteOldEntitiesStart(string description)
        {
            WriteEvent(100, description);
        }

        [Event(
            110,
            Message = "Finished deleting old entities of type {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteOldEntities,
            Opcode = EventOpcode.Stop)]
        public void DeleteOldEntitiesStop(string description)
        {
            WriteEvent(110, description);
        }

        [Event(
            12,
            Message = "Start retrieving old entities of type {0} (batch {1})",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrieveOldLogEntries,
            Opcode = EventOpcode.Start)]
        public void RetrieveOldLogEntriesStart(string description, int batch)
        {
            WriteEvent(12, description, batch);
        }

        [Event(
            13,
            Message = "Finished retrieving old entities of type {0} (batch {1})",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrieveOldLogEntries,
            Opcode = EventOpcode.Stop)]
        public void RetrieveOldLogEntriesStop(string description, int batch)
        {
            WriteEvent(13, description, batch);
        }

        [Event(
            14,
            Message = "Start executing table operations: {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteOperations,
            Opcode = EventOpcode.Start)]
        public void ExecuteOperationsStart(string description)
        {
            WriteEvent(14, description);
        }

        [Event(
            15,
            Message = "Finished executing table operations: {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteOperations,
            Opcode = EventOpcode.Stop)]
        public void ExecuteOperationsStop(string description)
        {
            WriteEvent(15, description);
        }

        [NonEvent]
        public void ExecutePartitionBatchOperationStart(string partition, int batch, string final)
        {
            ExecutePartitionBatchOperationStart(final, partition, batch);
        }

        [Event(
            16,
            Message = "Start executing batch operation for partition {1} / batch {2} {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.ExecutePartitionBatchOperation,
            Opcode = EventOpcode.Start)]
        private void ExecutePartitionBatchOperationStart(string final, string partition, int batch)
        {
            WriteEvent(16, final, partition, batch);
        }

        [NonEvent]
        public void ExecutePartitionBatchOperationStop(string partition, int batch, string final)
        {
            ExecutePartitionBatchOperationStop(final, partition, batch);
        }

        [Event(
            17,
            Message = "Finished executing batch operation for partition {1} / batch {2} {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.ExecutePartitionBatchOperation,
            Opcode = EventOpcode.Stop)]
        private void ExecutePartitionBatchOperationStop(string final, string partition, int batch)
        {
            WriteEvent(17, final, partition, batch);
        }

        [Event(
            18,
            Message = "Start inserting suggestion for Steam ID {0}: HLTB ID {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.InsertSuggestion,
            Opcode = EventOpcode.Start)]
        public void InsertSuggestionStart(int steamAppId, int hltbId)
        {
            WriteEvent(18, steamAppId, hltbId);
        }

        [NonEvent]
        public void ErrorExecutingPartitionBatchOperation(
            [NotNull] Exception exception, 
            int statusCode, string errorCode, string errorMessage,
            [NotNull] TableBatchOperation batchOperation)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            if (batchOperation == null) throw new ArgumentNullException(nameof(batchOperation));

            ErrorExecutingPartitionBatchOperation(exception.ToString(), statusCode, errorCode, errorMessage, String.Join(Environment.NewLine,
                    batchOperation.Select((o, i) => String.Format(CultureInfo.InvariantCulture, 
                        "[{0}] Type: {1} Partition: {2} Row: {3}", i, o.GetTableOperationType(), o.GetPartitionKey(), o.GetRowKey()))));
        }

        [Event(
            180,
            Message = "Error executing batch operation: {0}. Status code: {1}. Error code: {2}. Error message: {3}. Batch contents: {4}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Error)]
        private void ErrorExecutingPartitionBatchOperation(string exception, int statusCode, string errorCode, string errorMessage, string batchContents)
        {
            WriteEvent(180, exception, statusCode, errorCode, errorMessage, batchContents);
        }

        [Event(
            19,
            Message = "Finished inserting suggestion for Steam ID {0}: HLTB ID {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.InsertSuggestion,
            Opcode = EventOpcode.Stop)]
        public void InsertSuggestionStop(int steamAppId, int hltbId)
        {
            WriteEvent(19, steamAppId, hltbId);
        }

        [Event(
            20,
            Message = "Start deleting suggestion for Steam ID {0} : HLTB ID {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteSuggestion,
            Opcode = EventOpcode.Start)]
        public void DeleteSuggestionStart(int steamAppId, int hltbId)
        {
            WriteEvent(20, steamAppId, hltbId);
        }

        [Event(
            21,
            Message = "Finished deleting suggestion for Steam ID: {0} HLTB ID: {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.DeleteSuggestion,
            Opcode = EventOpcode.Stop)]
        public void DeleteSuggestionStop(int steamAppId, int hltbId)
        {
            WriteEvent(21, steamAppId, hltbId);
        }

        [Event(
            22,
            Message = "Start accepting suggestion for Steam ID: {0} HLTB ID: {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.AcceptSuggestion,
            Opcode = EventOpcode.Start)]
        public void AcceptSuggestionStart(int steamAppId, int hltbId)
        {
            WriteEvent(22, steamAppId, hltbId);
        }

        [Event(
            23,
            Message = "Finished accepting suggestion for Steam ID: {0} HLTB ID: {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.AcceptSuggestion,
            Opcode = EventOpcode.Stop)]
        public void AcceptSuggestionStop(int steamAppId, int hltbId)
        {
            WriteEvent(23, steamAppId, hltbId);
        }

        [Event(
            24,
            Message = "Start sending success email for: {0}",
            Keywords = Keywords.Email,
            Level = EventLevel.Informational,
            Task = Tasks.SendSuccessMail,
            Opcode = EventOpcode.Start)]
        public void SendSuccessMailStart(string description)
        {
            WriteEvent(24, description);
        }

        [Event(
            240,
            Message = "Error sending success mail for {0} ({1}) - retrying attempt #{2} / {3} will take place in {4} seconds",
            Keywords = Keywords.Email,
            Level = EventLevel.Warning)]
        public void ErrorSendingSuccessMail(string description, string error, int retryCount, int totalRetries, int delaySeconds)
        {
            WriteEvent(240, description, error, retryCount, totalRetries, delaySeconds);
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "MailStop")]
        [Event(
            25,
            Message = "Finished sending success email for: {0} (Status Code: {1}, Headers: {2}, Body: {3})",
            Keywords = Keywords.Email,
            Level = EventLevel.Informational,
            Task = Tasks.SendSuccessMail,
            Opcode = EventOpcode.Stop)]
        public void SendSuccessMailStop(string description, HttpStatusCode statusCode, string headers, string body)
        {
            WriteEvent(25, description, (int)statusCode, headers, body);
        }

        [Event(
            26,
            Message = "Start running process '{0}' with arguments '{1}'",
            Keywords = Keywords.Shell,
            Level = EventLevel.Informational,
            Task = Tasks.RunProcess,
            Opcode = EventOpcode.Start)]
        public void RunProcessStart(string fileName, string args)
        {
            WriteEvent(26, fileName, args);
        }

        [Event(
            27,
            Message = "Finished running process '{0} with arguments '{1}' (exit code: {2})",
            Keywords = Keywords.Shell,
            Level = EventLevel.Informational,
            Task = Tasks.RunProcess,
            Opcode = EventOpcode.Stop)]
        public void RunProcessStop(string fileName, string args, int exitCode)
        {
            WriteEvent(27, fileName, args, exitCode);
        }

        [Event(
            28,
            Message = "Start processing container blobs: {0}",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ProcessContainerBlobs,
            Opcode = EventOpcode.Start)]
        public void ProcessContainerBlobsStart(string description)
        {
            WriteEvent(28, description);
        }

        [Event(
            29,
            Message = "Finished processing container blobs: {0} ({1} blobs processed)",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ProcessContainerBlobs,
            Opcode = EventOpcode.Stop)]
        public void ProcessContainerBlobsStop(string description, int blobsProcessed)
        {
            WriteEvent(29, description, blobsProcessed);
        }

        [Event(
            30,
            Message = "Start retrieving blob container batch: {0} (batch {1})",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrieveContainerBlobBatch,
            Opcode = EventOpcode.Start)]
        public void RetrieveContainerBlobBatchStart(string description, int batch)
        {
            WriteEvent(30, description, batch);
        }

        [Event(
            31,
            Message = "Finished retrieving blob container batch: {0} (batch {1})",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.RetrieveContainerBlobBatch,
            Opcode = EventOpcode.Stop)]
        public void RetrieveContainerBlobBatchStop(string description, int batch)
        {
            WriteEvent(31, description, batch);
        }

        [Event(
            32,
            Message = "Start processing container blob: {0} (batch {1} - {2} blobs)",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.ProcessContainerBlobBatch,
            Opcode = EventOpcode.Start)]
        public void ProcessContainerBlobBatchStart(string description, int batch, int count)
        {
            WriteEvent(32, description, batch, count);
        }

        [Event(
            33,
            Message = "Finished processing container blobs: {0} (batch {1} - {2} blobs)",
            Keywords = Keywords.BlobStorage,
            Level = EventLevel.Verbose,
            Task = Tasks.ProcessContainerBlobBatch,
            Opcode = EventOpcode.Stop)]
        public void ProcessContainerBlobBatchStop(string description, int batch, int count)
        {
            WriteEvent(33, description, batch, count);
        }
    }
}