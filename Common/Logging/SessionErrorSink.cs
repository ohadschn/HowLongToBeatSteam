using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public class SessionErrorSink : IObserver<EventEntry>
    {
        public void OnNext(EventEntry value)
        {
            if (value != null && (value.Schema.Level == EventLevel.Error || value.Schema.Level == EventLevel.Critical))
            {
                EventSourceRegistrar.RecordSessionError(value);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "SessionErrorSink")] 
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "OnError")]
        public void OnError(Exception error)
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            // ReSharper disable once ConstantConditionalAccessQualifier
            SemanticLoggingEventSource.Log.CustomSinkUnhandledFault(error?.ToString() ?? "null exception in SessionErrorSink::OnError()");
        }

        public void OnCompleted()
        {
            //nothing to do
        }
    }

    public static class SessionErrorSinkExtensions
    {
        public static SinkSubscription<SessionErrorSink> LogSessionErrors([NotNull] this IObservable<EventEntry> eventStream)
        {
            if (eventStream == null) throw new ArgumentNullException(nameof(eventStream));

            var sink = new SessionErrorSink();
            return new SinkSubscription<SessionErrorSink>(eventStream.Subscribe(sink), sink);
        }
    }
}
