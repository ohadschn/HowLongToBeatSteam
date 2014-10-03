using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Common
{
    public static class Util
    {
        private static readonly bool TracingDisabled;
        private static readonly bool OnCloud;

        static Util()
        {
            Boolean.TryParse(ConfigurationManager.AppSettings["DisableTracing"], out TracingDisabled);
            Boolean.TryParse(ConfigurationManager.AppSettings["OnCloud"], out OnCloud);
        }

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

        public static Task ForEachAsync<T>(this IEnumerable<T> collection, int maxDegreeOfConcurrency, Func<T, Task> taskFactory)
        {
            return Task.WhenAll(Partitioner.Create(collection).GetPartitions(maxDegreeOfConcurrency).Select(partition => Task.Run(async delegate
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                await taskFactory(partition.Current);
                            }
                        }
                    })));
        }

        [StringFormatMethod("format")]
        public static void TraceInformation(string format, params object[] args)
        {
            if (!TracingDisabled)
            {
                Trace.TraceInformation(OnCloud ? format : GetTraceFormat(format), args);
            }
        }

        [StringFormatMethod("format")]
        public static void TraceWarning(string format, params object[] args)
        {
            if (!TracingDisabled)
            {
                Trace.TraceWarning(OnCloud ? format : GetTraceFormat(format), args);
            }
        }

        [StringFormatMethod("format")]
        public static void TraceError(string format, params object[] args)
        {
            if (!TracingDisabled)
            {
                Trace.TraceError(OnCloud ? format : GetTraceFormat(format), args);
            }
        }

        private static string GetTraceFormat(string format)
        {
            return String.Format("{0:O} {1}", DateTime.Now, format);
        }

        public static IEnumerable<IList<T>> Partition<T>(this IEnumerable<T> enumerable, int groupSize)
        {
            var list = new List<T>(groupSize);
            foreach (T item in enumerable)
            {
                list.Add(item);
                if (list.Count == groupSize)
                {
                    yield return list;
                    list = new List<T>(groupSize);
                }
            }

            if (list.Count != 0)
            {
                yield return list;
            }
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}