using System.Web.Http.ExceptionHandling;
using Microsoft.ApplicationInsights;

namespace HowLongToBeatSteam.Logging
{
    public class AppInsightsExceptionLogger : ExceptionLogger
    {
        private readonly TelemetryClient m_appInsightsClient = new TelemetryClient();

        public override void Log(ExceptionLoggerContext context)
        {
            if (context != null && context.Exception != null)
            {
                m_appInsightsClient.TrackException(context.Exception);
            }
            base.Log(context);
        }
    }
}