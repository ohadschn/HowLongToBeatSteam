using System;
using System.Diagnostics.Tracing;
using Common.Entities;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Keywords
        {
            private Keywords() {}
            public const EventKeywords Http = (EventKeywords)1;
            public const EventKeywords StoreApi = (EventKeywords)2;
            public const EventKeywords TableStorage = (EventKeywords)4;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public sealed class Tasks
        {
            private Tasks() { }
            public const EventTask RetrieveMissingStoreInformation = (EventTask)1;
            public const EventTask RetrieveStoreInformation = (EventTask)2;
            public const EventTask QueryAllApps = (EventTask)3;
            public const EventTask RetrieveBucketBatchMappings = (EventTask)4;
            public const EventTask ProcessBucketBatch = (EventTask)5;
            public const EventTask ExecuteOperations = (EventTask)6;
            public const EventTask ExecuteBucketBatchOperation = (EventTask)7;
            public const EventTask InsertSuggestion = (EventTask)8;
            public const EventTask QueryAllSuggestions = (EventTask) 9;
            public const EventTask DeleteSuggestion = (EventTask) 10;
            public const EventTask AcceptSuggestion = (EventTask) 11;
        }
// ReSharper restore ConvertToStaticClass

        [NonEvent]
        public void HttpRequestFailed(Uri uri, Exception exception, int attempt, int totalRetries, TimeSpan delay)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (!IsEnabled())
            {
                return;
            }

            HttpRequestFailed(uri.ToString(), exception.Message, attempt, totalRetries, delay.TotalSeconds);
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
        public void RetrieveStoreInformationStart(int startIndex, int endIndex, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStart(uri.ToString(), startIndex, endIndex);
        }

        [Event(
            4,
            Message = "Start retrieving Steam store info for apps {1}-{2} from {0}",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveStoreInformation,
            Opcode = EventOpcode.Start)]
        private void RetrieveStoreInformationStart(string uri, int start, int end)
        {
            WriteEvent(4, uri, start, end);
        }

        [NonEvent]
        public void RetrieveStoreInformationStop(int startIndex, int endIndex, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStop(uri.ToString(), startIndex, endIndex);
        }

        [Event(
            5,
            Message = "Finished retrieving Steam store info for apps {1}-{2} from {0}",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveStoreInformation,
            Opcode = EventOpcode.Stop)]
        private void RetrieveStoreInformationStop(string uri, int start, int end)
        {
            WriteEvent(5, uri, start, end);
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
            Message = "Querying table storage for all apps with filter: {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllApps,
            Opcode = EventOpcode.Start)]
        public void QueryAllAppsStart(string rowFilter)
        {
            WriteEvent(8, rowFilter);
        }

        [Event(
            9,
            Message = "Finished querying table storage for all apps with filter: {0} (count: {1})",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllApps,
            Opcode = EventOpcode.Stop)]
        public void QueryAllAppsStop(string rowFilter, int count)
        {
            WriteEvent(9, rowFilter, count);
        }

        [Event(
            80,
            Message = "Querying table storage for all suggestions with filter: {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllSuggestions,
            Opcode = EventOpcode.Start)]
        public void QueryAllSuggestionsStart(string rowFilter)
        {
            WriteEvent(80, rowFilter);
        }

        [Event(
            81,
            Message = "Finished querying table storage for all suggestions with filter: {0} (count: {1})",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.QueryAllSuggestions,
            Opcode = EventOpcode.Stop)]
        public void QueryAllSuggestionsStop(string rowFilter, int count)
        {
            WriteEvent(81, rowFilter, count);
        }

        [Event(
            10,
            Message = "Start retrieving mappings for bucket {0} / batch {1} from table storage",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveBucketBatchMappings,
            Opcode = EventOpcode.Start)]
        public void RetrieveBucketBatchMappingsStart(int bucket, int batch)
        {
            WriteEvent(10, bucket, batch);
        }

        [Event(
            11,
            Message = "Finished retrieving mappings for bucket {0} / batch {1} from table storage",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.RetrieveBucketBatchMappings,
            Opcode = EventOpcode.Stop)]
        public void RetrieveBucketBatchMappingsStop(int bucket, int batch)
        {
            WriteEvent(11, bucket, batch);
        }

        [Event(
            12,
            Message = "Start processing app entity bucket {0} / batch {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ProcessBucketBatch,
            Opcode = EventOpcode.Start)]
        public void ProcessBucketBatchStart(int bucket, int batch)
        {
            WriteEvent(12, bucket, batch);
        }

        [Event(
            13,
            Message = "Finished processing app entity bucket {0} / batch {1}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ProcessBucketBatch,
            Opcode = EventOpcode.Stop)]
        public void ProcessBucketBatchStop(int bucket, int batch)
        {
            WriteEvent(13, bucket, batch);
        }

        [Event(
            14,
            Message = "Start executing table operations",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteOperations,
            Opcode = EventOpcode.Start)]
        public void ExecuteOperationsStart()
        {
                WriteEvent(14);
        }

        [Event(
            15,
            Message = "Finished executing table operations",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteOperations,
            Opcode = EventOpcode.Stop)]
        public void ExecuteOperationsStop()
        {
            WriteEvent(15);
        }

        [NonEvent]
        public void ExecuteBucketBatchOperationStart(int bucket, int batch, string final)
        {
            ExecuteBucketBatchOperationStart(final, bucket, batch);
        }

        [Event(
            16,
            Message = "Start executing batch operation for bucket {1} / batch {2} {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteBucketBatchOperation,
            Opcode = EventOpcode.Start)]
        private void ExecuteBucketBatchOperationStart(string final, int bucket, int batch)
        {
            WriteEvent(16, final, bucket, batch);
        }

        [NonEvent]
        public void ExecuteBucketBatchOperationStop(int bucket, int batch, string final)
        {
            ExecuteBucketBatchOperationStop(final, bucket, batch);
        }

        [Event(
            17,
            Message = "Finished executing batch operation for bucket {1} / batch {2} {0}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteBucketBatchOperation,
            Opcode = EventOpcode.Stop)]
        private void ExecuteBucketBatchOperationStop(string final, int bucket, int batch)
        {
            WriteEvent(17, final, bucket, batch);
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
    }
}