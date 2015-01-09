using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

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

        //http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
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

        private const char ListSeparator = ';';
        private const char ListSeparatorReplacement = '-';
        public static string ToFlatString([NotNull] this IEnumerable<string> strings)
        {
            if (strings == null)
            {
                throw new ArgumentNullException("strings");
            }
            return String.Join(ListSeparator.ToString(CultureInfo.InvariantCulture),
                strings.Select(s => s.Replace(ListSeparator, ListSeparatorReplacement)));
        }

        public static string[] ToStringArray([NotNull] this string flatList)
        {
            if (flatList == null)
            {
                throw new ArgumentNullException("flatList");
            }
            return flatList.Split(ListSeparator);
        }

        public static Task<T> GetFirstResult<T>(Func<CancellationToken, Task<T>> taskFactory, int parallelization, Action<Exception> exceptionHandler)
        {
            return GetFirstResult(Enumerable.Repeat(0, parallelization).Select(i => taskFactory), exceptionHandler);
        }

        public static async Task<T> GetFirstResult<T>(this IEnumerable<Func<CancellationToken, Task<T>>> taskFactories, Action<Exception> exceptionHandler)
        {
            T ret = default(T);
            var cts = new CancellationTokenSource();

            var proxified = taskFactories.Select(tf => tf(cts.Token)).ProxifyByCompletion();

            int i;
            for (i = 0; i < proxified.Length; i++)
            {
                try
                {
                    ret = await proxified[i].ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    exceptionHandler(e);
                    if (i == proxified.Length - 1)
                    {
                        throw new InvalidOperationException("All tasks failed. See inner exception for last failure.", e);
                    }
                }
            }

            cts.Cancel();

            for (int j = i+1; j < proxified.Length; j++)
            {
                proxified[j].ContinueWith(t => exceptionHandler(t.Exception), TaskContinuationOptions.OnlyOnFaulted).Forget();
            }

            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task) //suppress CS4014
        {
        }

        //http://blogs.msdn.com/b/pfxteam/archive/2012/08/02/processing-tasks-as-they-complete.aspx
        public static Task<T>[] ProxifyByCompletion<T>(this IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToArray();
            var buckets = new TaskCompletionSource<T>[inputTasks.Length];
            var results = new Task<T>[inputTasks.Length];

            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TaskCompletionSource<T>();
                results[i] = buckets[i].Task;
            }

            int nextTaskIndex = -1;
            foreach (var inputTask in inputTasks)
            {
                inputTask.ContinueWith(completed =>
                {
                    var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                    if (completed.IsFaulted)
                    {
                        Trace.Assert(completed.Exception != null, "faulted exception has null Exception field");
                        bucket.TrySetException(completed.Exception.InnerExceptions);
                    }
                    else if (completed.IsCanceled)
                    {
                        bucket.TrySetCanceled();
                    }
                    else
                    {
                        bucket.TrySetResult(completed.Result);
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return results;
        }

        //http://stackoverflow.com/a/26276284/67824
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
            Environment.ProcessorCount*GetOptionalValueFromConfig("MaxDegreeOfConcurrencyFactor", 12));

        public static int MaxConcurrentHttpRequests
        {
            get { return s_maxConcurrentHttpRequests.Value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
        public static Task<T> GetAsync<T>(HttpRetryClient client, string url)
        {
            return GetAsync<T>(client, new Uri(url), CancellationToken.None);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
        public static Task<T> GetAsync<T>(HttpRetryClient client, string url, CancellationToken ct)
        {
            return GetAsync<T>(client, new Uri(url), ct);
        }

        public static Task<T> GetAsync<T>(HttpRetryClient client, Uri url)
        {
            return GetAsync<T>(client, url, CancellationToken.None);
        }

        public static async Task<T> GetAsync<T>(HttpRetryClient client, Uri url, CancellationToken ct)
        {
            T deserializedResponse;
            using (var response = await client.GetAsync(url, ct).ConfigureAwait(false))
            {
                deserializedResponse = await response.Content.ReadAsAsync<T>(ct).ConfigureAwait(false);
            }
            return deserializedResponse;
        }

        public static string CurrentTimestamp
        {
            get
            {
                var now = DateTime.Now;
                return String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}-{3}.{4}.{5}.{6}",
                    now.Year, now.Month.ToString("00", CultureInfo.InvariantCulture), now.Day.ToString("00", CultureInfo.InvariantCulture),
                    now.Hour, now.Minute.ToString("00", CultureInfo.InvariantCulture), now.Second.ToString("00", CultureInfo.InvariantCulture), now.Millisecond.ToString("000", CultureInfo.InvariantCulture));
            }
        }

        public static string GetOptionalValueFromConfig(string key, string defaultValue)
        {
            return GetValueFromConfig(key) ?? defaultValue;
        }

        public static int GetOptionalValueFromConfig(string key, int defaultValue)
        {
            int val;
            return Int32.TryParse(GetValueFromConfig(key), out val)
                ? val
                : defaultValue;
        }

        public static bool GetOptionalValueFromConfig(string key, bool defaultValue)
        {
            bool val;
            return Boolean.TryParse(GetValueFromConfig(key), out val)
                ? val
                : defaultValue;
        }

        public static string GetMandatoryValueFromConfig(string key)
        {
            var val = GetValueFromConfig(key);
            if (val == null)
            {
                throw new ConfigurationErrorsException("Missing mandatory configuration: " + key);
            }
            return val;
        }

        public static string GetValueFromConfig(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? Environment.GetEnvironmentVariable(key);
        }

        public static string GetMandatoryCustomConnectionStringFromConfig(string key)
        {
            var val = GetCustomConnectionStringFromConfig(key);
            if (val == null)
            {
                throw new ConfigurationErrorsException("Missing mandatory connection string: " + key);
            }
            return val; 
        }

        private static string GetCustomConnectionStringFromConfig(string key)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[key];
            return (connectionString != null)
                ? connectionString.ConnectionString
                : Environment.GetEnvironmentVariable("CUSTOMCONNSTR_" + key);
        }
    }
}