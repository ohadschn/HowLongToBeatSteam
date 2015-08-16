using System;
using System.Web.Mvc;
using JetBrains.Annotations;

namespace HowLongToBeatSteam
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters([NotNull] GlobalFilterCollection filters)
        {
            if (filters == null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            filters.Add(new HandleErrorAttribute());
        }
    }
}
