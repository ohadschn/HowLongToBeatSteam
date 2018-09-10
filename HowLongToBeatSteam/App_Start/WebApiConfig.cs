using System;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Common.Util;
using HowLongToBeatSteam.Filters;
using HowLongToBeatSteam.Logging;
using JetBrains.Annotations;

namespace HowLongToBeatSteam
{
    public static class WebApiConfig
    {
        public static void Register([NotNull] HttpConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            config.MapHttpAttributeRoutes();
            config.Services.Add(typeof(IExceptionLogger), new AppInsightsExceptionLogger());

            if (SiteUtil.OnAzure)
            {
                config.Filters.Add(new CsrfFilterAttribute());
            }
        }
    }
}
