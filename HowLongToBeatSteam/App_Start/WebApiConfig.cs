using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Common.Util;
using HowLongToBeatSteam.Filters;
using HowLongToBeatSteam.Logging;

namespace HowLongToBeatSteam
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.Services.Add(typeof(IExceptionLogger), new AppInsightsExceptionLogger());

            if (SiteUtil.OnAzure)
            {
                config.Filters.Add(new CsrfFilterAttribute());
            }
        }
    }
}
