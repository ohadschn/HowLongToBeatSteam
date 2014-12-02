﻿using System.Diagnostics.Tracing;
using Common.Storage;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public class EventSourceBase : EventSource
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public EventSourceBase()
        {
            var listener = new ObservableEventListener();
            listener.EnableEvents(this, EventLevel.LogAlways, Keywords.All);
            listener.LogToWindowsAzureTable("AzureTable", TableHelper.TableStorageConnectionString);
            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}