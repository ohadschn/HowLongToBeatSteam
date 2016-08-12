using System;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;

namespace HowLongToBeatSteam.Logging
{
    /// <summary>
    /// Exception handler for ASP.NET MVC
    /// https://azure.microsoft.com/en-us/documentation/articles/app-insights-asp-net-exceptions/#mvc
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AppInsightsExceptionHandlerAttribute : HandleErrorAttribute
    {
        private readonly TelemetryClient m_appInsightsClient = new TelemetryClient();
        public override void OnException([NotNull] ExceptionContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            if (filterContext.HttpContext != null && filterContext.Exception != null)
            {
                //If customError is Off, then AI HTTPModule will report the exception
                if (filterContext.HttpContext.IsCustomErrorEnabled)
                {
                    m_appInsightsClient.TrackException(filterContext.Exception);
                }
            }
            base.OnException(filterContext);
        }
    }
}