using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Common.Util;
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
            listener.LogToSystemDiagnosticsTrace(); //application diagnostics will upload these to azure tables

            if (SiteUtil.InWebJob)
            {
                listener.LogToConsole(); //needed for webjob logs (e.g. https://howlongtobeatsteam.scm.azurewebsites.net/azurejobs/#/jobs)
                listener.LogSessionErrors(); // log errors so they can be sent at the end of the run
            }

            EventSourceRegistrar.RegisterEventListener(this, listener);
        }
    }
}
