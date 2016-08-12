﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

namespace Common.Logging
{
    public abstract class EventSourceBase : EventSource
    {
        private static readonly EventLevel Verbosity = ResolveVerbosity();

        private static EventLevel ResolveVerbosity()
        {
            var verbosity = Environment.GetEnvironmentVariable("VERBOSITY");
            return (verbosity == null) ? EventLevel.LogAlways : (EventLevel)Enum.Parse(typeof (EventLevel), verbosity);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected EventSourceBase()
        {
            var listener = new ObservableEventListener();
            listener.EnableEvents(this, Verbosity, Keywords.All);
            listener.LogToSystemDiagnosticsTrace();
            listener.LogSessionErrors();
            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}
