using System.Diagnostics.CodeAnalysis;
using System.Web.Http.ExceptionHandling;
using Microsoft.ApplicationInsights;

namespace HowLongToBeatSteam.Logging
{
    public class AppInsightsExceptionLogger : ExceptionLogger
    {
        private readonly TelemetryClient m_appInsightsClient = new TelemetryClient();

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public override void Log(ExceptionLoggerContext context)
        {
            if (context?.Exception != null)
            {
                m_appInsightsClient.TrackException(context.Exception);
            }
            base.Log(context);
        }
    }
}