using System;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights;

// ReSharper disable once CheckNamespace
namespace HowLongToBeatSteam.ErrorHandler //this namespace makes AI configuration understand we have it configured 
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)] 
    public sealed class AiHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException([NotNull] ExceptionContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            if (filterContext.HttpContext != null 
                && filterContext.Exception != null 
                && filterContext.HttpContext.IsCustomErrorEnabled) //If customError is Off, then AI HTTPModule will report the exception
            {
                var ai = new TelemetryClient();
                ai.TrackException(filterContext.Exception);
            }
            base.OnException(filterContext);
        }
    }
}