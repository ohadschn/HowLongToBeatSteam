using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using CredentialManagement;
using JetBrains.Annotations;
using Microsoft.Win32;
using OpenQA.Selenium;

namespace UITests.Util
{
    public static class TestUtil
    {
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SuppressMessage("Code Smell", "S3877:ExceptionsShouldNotBeThrownFromUnexpectedMethods", Justification = "We want to fail early when the system isn't properly configured for IE")]
        static TestUtil()
        {
            var seleniumIeRegFlag = Registry.GetValue(Environment.Is64BitOperatingSystem
                    ? @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BFCACHE"
                    : @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BFCACHE",
                "iexplore.exe", null) as int?;

            if (seleniumIeRegFlag != 0)
            {
                throw new InvalidOperationException("It appears you have not configured your system for use with Selenium and IE. Please follow the instructions at: https://github.com/SeleniumHQ/selenium/wiki/InternetExplorerDriver (even if you don't have IE11 please set the registry key as an indicatio that you've followed the instructions");
            }
        }

        public static Credential GetCredentialFromManager(string target)
        {
            var cm = new Credential { Target = target };
            if (!cm.Load())
            {
               throw new ArgumentException("Could not load credentials for target: " + target);
            }

            return cm;
        }

        private static int GetUnitCount([NotNull] string duration, [NotNull] string unit)
        {
            if (duration == null) throw new ArgumentNullException(nameof(duration));
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            var unitMatch = Regex.Match(duration, @"(\d+) " + unit);
            return unitMatch.Success ? Int32.Parse(unitMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
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

            //stripping the Left-to-Right-Mark (LRM) inserted by IE
            return DateTime.Parse(Regex.Replace(date, @"\u200e", string.Empty).Trim(), CultureInfo.InvariantCulture);
        }

        public static string StringJoin<T>([NotNull] this IEnumerable<T> enumerable)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

            return String.Join(", ", enumerable);
        }
    }
}
