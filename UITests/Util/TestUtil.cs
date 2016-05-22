using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;

namespace UITests.Util
{
    public static class TestUtil
    {
        private static int GetUnitCount(string duration, string unit)
        {
            var unitMatch = Regex.Match(duration, @"(\d+) " + unit);
            return unitMatch.Success ? Int32.Parse(unitMatch.Groups[1].Value) : 0;
        }

        public static TimeSpan FreetextDurationToTimespan(string duration)
        {
            var hours = GetUnitCount(duration, "hour");
            var days = GetUnitCount(duration, "day");
            var weeks = GetUnitCount(duration, "week");
            var months = GetUnitCount(duration, "month");
            var years = GetUnitCount(duration, "year");

            return new TimeSpan(years * 365 + months * 30 + weeks * 7 + days, hours, 0, 0);
        }

        public static DateTime ParseBrowserDate(IWebDriver driver, string date)
        {
            return DateTime.ParseExact(date, driver is InternetExplorerDriver ? "d/MM/yyyy" : "M/d/yyyy", CultureInfo.InvariantCulture);
        }

        public static string StringJoin<T>(this IEnumerable<T> enumerable)
        {
            return String.Join(", ", enumerable);
        }
    }
}
