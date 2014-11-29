using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common.Util
{
    public static class SiteUtil
    {
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
                    foreach (var item in enumerators.Where(e => e.MoveNext()).Select(e => e.Current))
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
        public static Task<T> GetAsync<T>(HttpRetryClient client, string url)
        {
            return GetAsync<T>(client, new Uri(url));
        }

        public static async Task<T> GetAsync<T>(HttpRetryClient client, Uri url)
        {
            T deserializedResponse;
            using (var response = await client.GetAsync(url).ConfigureAwait(false))
            {
                deserializedResponse = await response.Content.ReadAsAsync<T>().ConfigureAwait(false);
            }
            return deserializedResponse;
        }
    }
}