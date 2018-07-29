using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using static System.FormattableString;

namespace Common.Logging
{
    public sealed class SystemDiagnosticsTraceSink : IObserver<EventEntry>
    {
        private bool m_flushing;

        ~SystemDiagnosticsTraceSink()
        {
            Flush(false);
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
            Flush(true);
        }

        private void Flush(bool completing)
        {
            if (m_flushing)
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
                    Trace.TraceError(Invariant($"{nameof(SystemDiagnosticsTraceSink)} was not disposed"));
                }
            }

            Trace.Flush();

            m_flushing = true;
        }
    }

    // ReSharper disable once UnusedMember.Global (R# issue)
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