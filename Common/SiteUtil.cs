using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Common
{
    public static class SiteUtil
    {
        private static readonly bool TracingDisabled = TryParseBool(ConfigurationManager.AppSettings["DisableTracing"]);
        private static readonly bool OnCloud = TryParseBool(ConfigurationManager.AppSettings["OnCloud"]);

        private static bool TryParseBool(string value)
        {
            bool result;
            Boolean.TryParse(value, out result);
            return result;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

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
                                await taskFactory(partition.Current).ConfigureAwait(false);
                            }
                        }
                    })));
        }

        public static IEnumerable<T> Interleave<T>(this IEnumerable<IEnumerable<T>> source)
        {
            var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
            try
            {
                bool itemsRemaining;
                do
                {
                    itemsRemaining = false;
                    foreach (var item in
                             enumerators.Where(e => e.MoveNext()).Select(e => e.Current))
                    {
                        yield return item;
                        itemsRemaining = true;
                    }
                }
                while (itemsRemaining);
            }
            finally
            {
                Array.ForEach(enumerators, e => e.Dispose());
            }
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
            return String.Format(CultureInfo.InvariantCulture, "{0:O} {1}", DateTime.Now, format);
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
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static T[] GenerateInitializedArray<T>(int size, Func<int, T> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException("factory");
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size");
            }

            var arr = new T[size];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = factory(i);
            }
            return arr;
        }

        public static void SetDefaultConnectionLimit()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
        }

        private static readonly Lazy<int> s_maxConcurrentHttpRequests = new Lazy<int>(() =>
            Environment.ProcessorCount*
            Int32.Parse(ConfigurationManager.AppSettings["MaxDegreeOfConcurrencyFactor"], CultureInfo.InvariantCulture));

        public static int MaxConcurrentHttpRequests
        {
            get { return s_maxConcurrentHttpRequests.Value; }
        }
    }
}