using System;
using System.Diagnostics.Tracing;

namespace Common
{
    public interface ICommonEventSource
    {
        void HttpRequestFailed(Uri uri, Exception exception, int attempt, TimeSpan delay);
        void RetrieveMissingStoreInformationStart();
        void RetrieveMissingStoreInformationStop();
        void RetrieveStoreInformationStart(int start, int end, Uri uri);
        void RetrieveStoreInformationStop(int start, int end, Uri uri);
        void SkippedCategorizedApp(int appId, string name, string type);
        void CategorizingApp(int appId, string name, string type);
        void QueryAllAppsStart(string rowFilter);
        void QueryAllAppsStop(string rowFilter, int count);
        void RetrieveBucketBatchMappingsStart(int bucket, int batch);
        void RetrieveBucketBatchMappingsStop(int bucket, int batch);
        void ProcessBucketBatchStart(int bucket, int batch);
        void ProcessBucketBatchStop(int bucket, int batch);
        void ExecuteOperationsStart();
        void ExecuteOperationsStop();
        void ExecuteBucketBatchOperationStart(int bucket, int batch, string final);
        void ExecuteBucketBatchOperationStop(int bucket, int batch, string final);
    }

    [EventSource(Name = "OS-HowLongToBeatSteam-Common")]
    public sealed class CommonEventSource : EventSource, ICommonEventSource
    {
        public static readonly ICommonEventSource Log = new CommonEventSource();
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
        }
// ReSharper restore ConvertToStaticClass

        [NonEvent]
        public void HttpRequestFailed(Uri uri, Exception exception, int attempt, TimeSpan delay)
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

            HttpRequestFailed(uri.ToString(), exception.Message, attempt, delay.TotalSeconds);
        }

        [Event(
            1,
            Message = "Request to URI {0} failed due to: {1}. Retrying attempt #{2} will take place in {3}",
            Keywords = Keywords.Http,
            Level = EventLevel.Warning)]
        private void HttpRequestFailed(string uri, string exceptionMessage, int attempt, double delaySeconds)
        {
            WriteEvent(1, uri, exceptionMessage, attempt, delaySeconds);
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
        public void RetrieveStoreInformationStart(int start, int end, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStart(uri.ToString(), start, end);
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
        public void RetrieveStoreInformationStop(int start, int end, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (!IsEnabled())
            {
                return;
            }

            RetrieveStoreInformationStop(uri.ToString(), start, end);
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
        public void SkippedCategorizedApp(int appId, string name, string type)
        {
            WriteEvent(6, appId, name, type);
        }

        [Event(
            7,
            Message = "Categorizing {0} / {1} as '{2}'",
            Keywords = Keywords.StoreApi,
            Level = EventLevel.Informational)]
        public void CategorizingApp(int appId, string name, string type)
        {
            WriteEvent(7, appId, name, type);
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
            Message = "Start executing batch operation for bucket {0} / batch {1} {2}",
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
            Message = "Finished executing batch operation for bucket {0} / batch {1} {2}",
            Keywords = Keywords.TableStorage,
            Level = EventLevel.Informational,
            Task = Tasks.ExecuteBucketBatchOperation,
            Opcode = EventOpcode.Stop)]
        private void ExecuteBucketBatchOperationStop(string final, int bucket, int batch)
        {
            WriteEvent(17, final, bucket, batch);
        }
    }
}