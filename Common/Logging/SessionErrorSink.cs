using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using JetBrains.Annotations;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Schema;
using static System.FormattableString;

namespace Common.Logging
{
    public sealed class SessionErrorSink : IObserver<EventEntry>
    {
        public void OnNext(EventEntry value)
        {
            if (value != null && (value.Schema.Level == EventLevel.Error || value.Schema.Level == EventLevel.Critical))
            {
                EventSourceRegistrar.RecordSessionError(value);
            }
        }

        public void OnError(Exception error)
        {
            EventSourceRegistrar.RecordSessionError(
                new EventEntry(Guid.Empty, -1, Invariant($"Error in SLAB provider: {error}"), new ReadOnlyCollection<object>(new object[] {}), DateTimeOffset.UtcNow,
                new EventSchema(-1, Guid.Empty, "SLAB", EventLevel.Error, EventTask.None, "", EventOpcode.Info, "", EventKeywords.None, "", 0, new string[] {})));
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
