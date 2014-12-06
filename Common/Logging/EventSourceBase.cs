using System.Diagnostics.Tracing;
using Common.Storage;
using Common.Util;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public class EventSourceBase : EventSource
    {
        private static readonly bool s_logToConsole = SiteUtil.GetOptionalValueFromConfig("LogToConsole", true);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public EventSourceBase()
        {
            var listener = new ObservableEventListener();
            listener.EnableEvents(this, EventLevel.LogAlways, Keywords.All);
            listener.LogToWindowsAzureTable("AzureTable", StorageHelper.AzureStorageConnectionString);
            if (s_logToConsole)
            {
                listener.LogToConsole();
            }
            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}
