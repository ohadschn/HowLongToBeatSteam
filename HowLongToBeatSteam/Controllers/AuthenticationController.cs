using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using HowLongToBeatSteam.Logging;

namespace HowLongToBeatSteam.Controllers
{
    public class AuthenticationController : Controller
    {
        const string SteamOpenIdProviderIdentifier = "http://steamcommunity.com/openid";

        private static readonly OpenIdRelyingParty s_relyingParty = new OpenIdRelyingParty(); //thread-safe

        public ActionResult Index()
        {
            var response = s_relyingParty.GetResponse();

            if (response == null) //Stage 1 - user unauthenticated => redirect to Steam Login page
            {
                return CreateOpenIdRedirectingResponse();
            }

            //Stage 2 - user completed authentication => redirect back to HLTBS
            return CreateHltbsRedirectionResponse(response);
        }

        private ActionResult CreateHltbsRedirectionResponse(IAuthenticationResponse response)
        {
            if (response.Status != AuthenticationStatus.Authenticated)
            {
                SiteEventSource.Log.SteamAuthenticationFailed(response.Status);
                return CreateSiteRedirection();
            }

            long steam64Id;
            try
            {
                steam64Id = Int64.Parse(GetSteam64IdFromClaimedId(response.ClaimedIdentifier), CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                SiteEventSource.Log.MalformedSteamClaimedIdProvided(response.ClaimedIdentifier, e.Message);
                return CreateSiteRedirection();
            }
            catch (OverflowException e)
            {
                SiteEventSource.Log.MalformedSteamClaimedIdProvided(response.ClaimedIdentifier, e.Message);
                return CreateSiteRedirection();
            }

            SiteEventSource.Log.SteamAuthenticationSucceeded(response.FriendlyIdentifierForDisplay);
            return CreateSiteRedirection(steam64Id);
        }

        private RedirectResult CreateSiteRedirection(long? steam64Id = null)
        {
            // ReSharper disable once PossibleNullReferenceException
            return Redirect(String.Format(CultureInfo.InvariantCulture, "{0}/#/auth/{1}",
                Request.Url.GetLeftPart(UriPartial.Authority),
                steam64Id == null ? "failed" : steam64Id.ToString()));
        }

        private static string GetSteam64IdFromClaimedId(Identifier claimedId)
        {
            //The Steam Claimed ID format is: http://steamcommunity.com/openid/id/<steam64ID>
            return ((string)claimedId).Split('/').Last();
        }

        private static ActionResult CreateOpenIdRedirectingResponse()
        {
            SiteEventSource.Log.CreateAuthenticationRequestStart();
            var authenticationRequest = s_relyingParty.CreateRequest(SteamOpenIdProviderIdentifier);
            SiteEventSource.Log.CreateAuthenticationRequestStop();

            return authenticationRequest.RedirectingResponse.AsActionResultMvc5();
        }
    }
}