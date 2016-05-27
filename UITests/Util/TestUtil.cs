using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;

namespace UITests.Util
{
    public static class TestUtil
    {
        private static int GetUnitCount([NotNull] string duration, [NotNull] string unit)
        {
            if (duration == null) throw new ArgumentNullException(nameof(duration));
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            var unitMatch = Regex.Match(duration, @"(\d+) " + unit);
            return unitMatch.Success ? Int32.Parse(unitMatch.Groups[1].Value) : 0;
        }

        public static TimeSpan FreetextDurationToTimespan([NotNull] string duration)
        {
            if (duration == null) throw new ArgumentNullException(nameof(duration));

            var hours = GetUnitCount(duration, "hour");
            var days = GetUnitCount(duration, "day");
            var weeks = GetUnitCount(duration, "week");
            var months = GetUnitCount(duration, "month");
            var years = GetUnitCount(duration, "year");

            return new TimeSpan(years * 365 + months * 30 + weeks * 7 + days, hours, 0, 0);
        }

        public static DateTime ParseBrowserDate([NotNull] IWebDriver driver, [NotNull] string date)
        {
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (date == null) throw new ArgumentNullException(nameof(date));

            return DateTime.ParseExact(date, driver is InternetExplorerDriver ? "d/MM/yyyy" : "M/d/yyyy", CultureInfo.InvariantCulture);
        }

        public static string StringJoin<T>([NotNull] this IEnumerable<T> enumerable)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            return String.Join(", ", enumerable);
        }
    }
}
