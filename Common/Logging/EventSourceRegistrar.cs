using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Common.Util;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public static class EventSourceRegistrar
    {
        private class EventListenerInfo
        {
            public EventSource EventSource { get; private set; }
            public ObservableEventListener Listener { get; private set; }

            public EventListenerInfo(EventSource eventSource, ObservableEventListener listener)
            {
                Listener = listener;
                EventSource = eventSource;
            }
        }

        private static readonly bool s_logTplEvents = SiteUtil.GetOptionalValueFromConfig("LogTplEvents", false);
        static private readonly ConcurrentBag<EventListenerInfo> s_listeners = new ConcurrentBag<EventListenerInfo>(); 
        public static void RegisterEventListener(EventSource source, ObservableEventListener listener)
        {
            s_listeners.Add(new EventListenerInfo(source, listener));

            if (s_logTplEvents)
            {
                //TODO consider taking only transfer events from TplEtwProvider
                listener.EnableEvents("System.Threading.Tasks.TplEventSource", EventLevel.Informational, Keywords.All);
            }
        }

        public static void DisposeEventListeners()
        {
            foreach (var listenerInfo in s_listeners)
            {
                listenerInfo.Listener.DisableEvents(listenerInfo.EventSource);
                listenerInfo.Listener.Dispose();
            }
        }
    }
}
