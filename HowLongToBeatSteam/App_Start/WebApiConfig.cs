using System.Web.Http;

namespace HowLongToBeatSteam
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
        }
    }
}
