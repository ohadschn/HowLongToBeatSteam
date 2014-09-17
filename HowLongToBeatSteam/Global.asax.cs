using System.Web.Http;
using HowLongToBeatSteam.Controllers;

namespace HowLongToBeatSteam
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GamesController.Touch(); //make sure caching starts as soon as site is up
        }
    }
}
