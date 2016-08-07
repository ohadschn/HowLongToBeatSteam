using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace HowLongToBeatSteam.Filters
{
    public class CsrfFilterAttribute : ActionFilterAttribute
    {
        private readonly Uri m_expectedUri = new Uri("https://www.howlongtobeatsteam.com");

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            IEnumerable<string> values;

            bool hostSpecified = actionContext.Request.Headers.TryGetValues("Host", out values);
            if (!hostSpecified || !m_expectedUri.Host.Equals(values.SingleOrDefault(), StringComparison.InvariantCultureIgnoreCase))
            {
                //log
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            bool originSpecified = actionContext.Request.Headers.TryGetValues("Origin", out values);
            if (originSpecified && !m_expectedUri.GetLeftPart(UriPartial.Authority).Equals(values.SingleOrDefault(), StringComparison.InvariantCultureIgnoreCase))
            {
                //log
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            if (originSpecified)
            {
                // Origin header check passed
                return;
            }

            var refererSpecified = actionContext.Request.Headers.TryGetValues("Referer", out values);
            if (!refererSpecified)
            {
                //log (should be very rare)
                return;
            }

            Uri refererUri;
            if (!Uri.TryCreate(values.SingleOrDefault(), UriKind.Absolute, out refererUri))
            {
                //partial URI - assuming the base is our host so we're good (should be rare too)
                //log
                return;
            }

            if (!m_expectedUri.GetLeftPart(UriPartial.Authority).Equals(refererUri.GetLeftPart(UriPartial.Authority), StringComparison.InvariantCultureIgnoreCase))
            {
                //log
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }
        }
    }
}