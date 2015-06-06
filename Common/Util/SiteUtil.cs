using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using SendGrid;

namespace Common.Util
{
    public static class SiteUtil
    {
        private static readonly string SendGridUser = GetMandatoryValueFromConfig("SendGridUser");
        private static readonly string SendGridPassword = GetMandatoryValueFromConfig("SendGridPassword");
        private static readonly string NotificationEmailAddress = GetMandatoryValueFromConfig("NotificationEmailAddress");

        public static T GetNonpublicInstancePropertyValue<T>([NotNull] object instance, string propName)
        {
            if (instance == null) throw new ArgumentNullException("instance");

            return (T)instance.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance, null);
        }

        public static string CleanString(string text, HashSet<char> disallowedChars)
        {
            return new String(text.Where(c => !disallowedChars.Contains(c)).ToArray());
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

        //http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        public static async Task ForEachAsync<T>(this IEnumerable<T> collection, int maxDegreeOfConcurrency, Func<T, Task> taskFactory, bool failEarly=true)
        {
            var cts = new CancellationTokenSource();
            var exceptions = new ConcurrentBag<Exception>();

            await Task.WhenAll(Partitioner.Create(collection).GetPartitions(maxDegreeOfConcurrency).Select(partition => Task.Run(async delegate
                    {
                        using (partition)
                        {
                            while (partition.MoveNext())
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                try
                                {
                                    await taskFactory(partition.Current).ConfigureAwait(false);
                                }
                                catch (Exception e)
                                {
                                    if (failEarly)
                                    {
                                        cts.Cancel();
                                        throw;
                                    }

                                    exceptions.Add(e);
                                }
                            }
                        }
                    }, cts.Token)));

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
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
            var cts = new CancellationTokenSource();

            var tasks = taskFactories.Select(tf => tf(cts.Token)).ToArray();
            foreach (var task in tasks)
            {
                task.ContinueWith(t => exceptionHandler(t.Exception), TaskContinuationOptions.OnlyOnFaulted).Forget();
            }

            var first = await Task.WhenAny(tasks).ConfigureAwait(false);
            cts.Cancel();

            return first.GetAwaiter().GetResult(); //will throw the original exception if first failed (we want to fail fast)
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task) //suppress CS4014
        {
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

        public static void AddRange<T>([NotNull] this ConcurrentBag<T> bag, [NotNull] IEnumerable<T> range)
        {
            if (bag == null) throw new ArgumentNullException("bag");
            if (range == null) throw new ArgumentNullException("range");

            foreach (var item in range)
            {
                bag.Add(item);
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

        [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
        public static Task<T> GetAsync<T>(HttpRetryClient client, string url)
        {
            return GetAsync<T>(client, new Uri(url), CancellationToken.None);
        }

        [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads")]
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

        private static readonly int KeepAliveIntervalSeconds = GetOptionalValueFromConfig("KeepAliveIntervalSeconds", 10);
        public static void KeepWebJobAlive()
        {
            Console.WriteLine("Keepalive...");
            Task.Delay(KeepAliveIntervalSeconds*1000).ContinueWith(t => KeepWebJobAlive());
        }

        public static string WebJobName
        {
            get { return Environment.GetEnvironmentVariable("WEBJOBS_NAME"); }
        }

        public static string WebJobRunId
        {
            get { return Environment.GetEnvironmentVariable("WEBJOBS_RUN_ID "); }
        }

        public static async Task SendSuccessMail([NotNull] string description)
        {
            if (description == null) throw new ArgumentNullException("description");

            CommonEventSource.Log.SendSuccessMailStart(description);
            await new Web(new NetworkCredential(SendGridUser, SendGridPassword)).DeliverAsync(new SendGridMessage
            {
                From = new MailAddress("webjobs@howlongtobeatsteam.com", "HowLongToBeatSteam WebJob notifier"),
                Subject = String.Format(CultureInfo.InvariantCulture, "{0} ({1}) - Success", WebJobName, WebJobRunId),
                To = new[] { new MailAddress(NotificationEmailAddress) },
                Text = "Sent by SendGrid"
            });
            CommonEventSource.Log.SendSuccessMailStop(description);
        }
    }
}