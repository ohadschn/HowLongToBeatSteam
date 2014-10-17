using System.Web.Http;
using Common.Util;
using HowLongToBeatSteam.Controllers;

namespace HowLongToBeatSteam
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            SiteUtil.SetDefaultConnectionLimit();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GamesController.StartUpdatingCache(); //make sure caching starts as soon as site is up
        }
    }
}
