using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using HowLongToBeatSteam.Logging;

namespace HowLongToBeatSteam.Filters
{
    public class CsrfFilterAttribute : ActionFilterAttribute
    {
        private readonly Uri m_expectedUri = new Uri("https://www.howlongtobeatsteam.com");

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            IEnumerable<string> values;

            if (actionContext.Request.Headers.TryGetValues("Origin", out values))
            {
                if (!m_expectedUri.GetLeftPart(UriPartial.Authority).Equals(values.SingleOrDefault(), StringComparison.InvariantCultureIgnoreCase))
                {
                    SiteEventSource.Log.MismatchedOriginInRequest(actionContext.Request);
                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }

                return;
            }

            if (!actionContext.Request.Headers.TryGetValues("Referer", out values))
            {
                //Should be very rare according to OWASP - monitor event
                SiteEventSource.Log.NeitherOriginNorRefererSpecifiedInRequest(actionContext.Request);
                return;
            }

            Uri refererUri;
            if (!Uri.TryCreate(values.SingleOrDefault(), UriKind.Absolute, out refererUri)) //partial URI
            {
                SiteEventSource.Log.PartialRefererSpecifiedInRequest(actionContext.Request);
                return; // assuming the URL's base is our host so we're good
            }

            if (!m_expectedUri.GetLeftPart(UriPartial.Authority).Equals(refererUri.GetLeftPart(UriPartial.Authority), StringComparison.InvariantCultureIgnoreCase))
            {
                SiteEventSource.Log.MismatchedRefererHeader(actionContext.Request);
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
        }
    }
}