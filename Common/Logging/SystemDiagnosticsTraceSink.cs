﻿using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;

namespace Common.Logging
{
    public sealed class SystemDiagnosticsTraceSink : IObserver<EventEntry>
    {
        private bool m_disposed = false;

        ~SystemDiagnosticsTraceSink()
        {
            Dispose(false);
        }

        public void OnNext(EventEntry value)
        {
            if (value == null) return;
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                new EventTextFormatter().WriteEvent(value, writer);
                var eventText = writer.ToString();

                switch (value.Schema.Level)
                {
                    case EventLevel.LogAlways:
                    case EventLevel.Critical:
                    case EventLevel.Error:
                        Trace.TraceError(eventText);
                        return;
                    case EventLevel.Warning:
                        Trace.TraceWarning(eventText);
                        return;
                    case EventLevel.Informational:
                    case EventLevel.Verbose:
                        Trace.TraceInformation(eventText);
                        return;
                    default:
                        Trace.TraceError("Unknown event level: " + value.Schema.Level);
                        Trace.TraceInformation(eventText);
                        return;
                }
            }
        }

        public void OnError(Exception error)
        {
            Trace.TraceError("SystemDiagnosticsTraceSink.OnError() called with exception: " + error);
        }

        public void OnCompleted()
        {
            Dispose(true);
        }

        private void Dispose(bool completing)
        {
            if (m_disposed)
            {
                return;
            }

            if (!completing)
            {
                //we shouldn't reach here - it means ObservableEventListener.Dispose() wasn't called
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    Trace.TraceError($"{nameof(SystemDiagnosticsTraceSink)} was not disposed");
                }
            }

            Trace.Flush();

            m_disposed = true;
        }
    }

    public static class SystemDiagnosticsTraceSinkExtensions
    {
        public static SinkSubscription<SystemDiagnosticsTraceSink> LogToSystemDiagnosticsTrace([NotNull] this IObservable<EventEntry> eventStream)
        {
            if (eventStream == null) throw new ArgumentNullException(nameof(eventStream));

            var sink = new SystemDiagnosticsTraceSink();
            return new SinkSubscription<SystemDiagnosticsTraceSink>(eventStream.Subscribe(sink), sink);
        }
    }
}
