using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Storage;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public class EventSourceBase : EventSource
    {
        private static readonly EventLevel Verbosity = ResolveVerbosity();

        private static EventLevel ResolveVerbosity()
        {
            var verbosity = Environment.GetEnvironmentVariable("VERBOSITY");
            return (verbosity == null) ? EventLevel.LogAlways : (EventLevel)Enum.Parse(typeof (EventLevel), verbosity);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public EventSourceBase()
        {
            var listener = new ObservableEventListener();
            listener.EnableEvents(this, Verbosity, Keywords.All);
            listener.LogToWindowsAzureTable("AzureTable", StorageHelper.AzureStorageTablesConnectionString);
            listener.LogToConsole();
            listener.LogSessionErrors();
            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}
