using System;
using System.Web.Http;
using Common.Logging;
using Common.Util;
using HowLongToBeatSteam.Controllers;
using System.Web.Mvc;
using System.Web.Routing;
using HowLongToBeatSteam.Telemetry;

namespace HowLongToBeatSteam
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            TelemetryManager.Setup("d0a63409-84bf-4f88-a8cf-8440ce670471");
            SiteUtil.SetDefaultConnectionLimit();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GamesController.StartUpdatingCache(); //make sure caching starts as soon as site is up

            InitMvc();
        }

        protected void Application_End(object sender, EventArgs e)
        {
            EventSourceRegistrar.DisposeEventListeners();
        }

        private static void InitMvc()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            MvcHandler.DisableMvcResponseHeader = true;
        }
    }
}
