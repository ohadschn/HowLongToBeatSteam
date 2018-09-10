using System;
using System.Web.Http.ExceptionHandling;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;

namespace HowLongToBeatSteam.Logging
{
    /// <inheritdoc />
    /// <summary>
    /// Exception logger for ASP.NET WEB API 2.X
    /// https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net-exceptions/#web-api-2x
    /// </summary>
    public class AppInsightsExceptionLogger : ExceptionLogger
    {
        private readonly TelemetryClient m_appInsightsClient = new TelemetryClient();

        public override void Log([NotNull] ExceptionLoggerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Exception != null)
            {
                m_appInsightsClient.TrackException(context.Exception);
            }
            base.Log(context);
        }
    }
}