using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
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

        static private readonly ConcurrentBag<EventListenerInfo> s_listeners = new ConcurrentBag<EventListenerInfo>(); 
        public static void RegisterEventListener(EventSource source, ObservableEventListener listener)
        {
            s_listeners.Add(new EventListenerInfo(source, listener));
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
