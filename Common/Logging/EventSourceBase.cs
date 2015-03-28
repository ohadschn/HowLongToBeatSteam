using System;
using System.Diagnostics.Tracing;
using Common.Storage;
using Common.Util;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public class EventSourceBase : EventSource
    {
        private static readonly bool s_inWebJob = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBJOBS_TYPE"));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public EventSourceBase()
        {
            var listener = new ObservableEventListener();
            listener.EnableEvents(this, EventLevel.LogAlways, Keywords.All);
            listener.LogToWindowsAzureTable("AzureTable", StorageHelper.AzureStorageTablesConnectionString);
            if (s_inWebJob)
            {
                listener.LogToConsole();
            }
            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}
