using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Common
{
    public static class Util
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            TValue ret;
            if (!dictionary.TryGetValue(key, out ret))
            {
                ret = new TValue();
                dictionary[key] = ret;
            }
            return ret;
        }

        [StringFormatMethod("format")]
        public static void TraceInformation(string format, params object[] args)
        {
            Trace.TraceInformation(GetTraceFormat(format), args);
        }

        [StringFormatMethod("format")]
        public static void TraceWarning(string format, params object[] args)
        {
            Trace.TraceWarning(GetTraceFormat(format), args);
        }

        [StringFormatMethod("format")]
        public static void TraceError(string format, params object[] args)
        {
            Trace.TraceError(GetTraceFormat(format), args);
        }

        private static string GetTraceFormat(string format)
        {
            return String.Format("{0:O} {1}", DateTime.Now, format);
        }
    }
}
