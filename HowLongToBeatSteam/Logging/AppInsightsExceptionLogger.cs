using System.Diagnostics.CodeAnalysis;
using System.Web.Http.ExceptionHandling;
using Microsoft.ApplicationInsights;

namespace HowLongToBeatSteam.Logging
{
    /// <summary>
    /// Exception logger for ASP.NET WEB API 2.X
    /// https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net-exceptions/#web-api-2x
    /// </summary>
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