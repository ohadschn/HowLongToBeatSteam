using System.Web.Http;
using Common;
using HowLongToBeatSteam.Controllers;

namespace HowLongToBeatSteam
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Util.SetDefaultConnectionLimit();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GamesController.StartUpdatingCache(); //make sure caching starts as soon as site is up
        }
    }
}
