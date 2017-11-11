using System.Web;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace HowLongToBeatSteam.Telemetry
{
    public class UrlReferrerInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry)
            {
                telemetry.Context.Properties.Add("Referrer", HttpContext.Current.Request.UrlReferrer?.ToString());
            }
        }
    }
}