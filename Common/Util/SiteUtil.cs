using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Formatters;
using SendGrid.Helpers.Mail;
using static System.FormattableString;

namespace Common.Util
{
    public static class SiteUtil
    {
        private static readonly HttpRetryClient SendGridClient = GetSendGridClient();
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private static HttpRetryClient GetSendGridClient()
        {
            var apiKey = GetMandatoryCustomConnectionStringFromConfig("SendGridApiKey");
            var retries = GetOptionalValueFromConfig("SendGridRetries", 10);
            return new HttpRetryClient(retries) { DefaultRequestAuthorization = new AuthenticationHeaderValue(HttpRetryClient.BearerAuthorizationScheme, apiKey) };
        }

        private const string WebjobNameEnvironmentVariable = "WEBJOBS_NAME";
        private const string WebjobRunIdEnvironmentVariable = "WEBJOBS_RUN_ID";
        private const string WebsiteNameEnvironmentVariable = "WEBSITE_SITE_NAME";

        //used to determine if running on azure or not (never mocked)
        private const string WebSiteHostNameEnvironmentVariable = "WEBSITE_HOSTNAME";

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#"), SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        public static void Swap<T>(ref T first, ref T second)
        {
            T temp = first;
            first = second;
            second = temp;
        }

        public static string ReplaceNonLettersAndNumbersWithSpaces(string text)
        {
            return Regex.Replace(text, @"[^\p{L}\p{N}]", " ", RegexOptions.Compiled);
        }

        public static DateTime AddYears(this DateTime dateTime, double years)
        {
            int roundedYears = (int) Math.Floor(years);
            var roundedDate = dateTime.AddYears(roundedYears);
            var lastYearSpan = roundedDate.AddYears(1) - roundedDate;
            return roundedDate.AddDays((years % 1) * lastYearSpan.TotalDays);
        }

        public static T GetNonpublicInstancePropertyValue<T>([NotNull] object instance, string propName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var propertyInfo = instance.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException(Invariant($"Non-public instance property '{propName}' does not exist in type {instance.GetType()}"));    
            }

            return (T) propertyInfo.GetValue(instance, null);
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
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.TryGetValue(key, out TValue ret))
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
            }, cts.Token))).ConfigureAwait(false);

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
                throw new ArgumentNullException(nameof(strings));
            }
            return String.Join(ListSeparator.ToString(CultureInfo.InvariantCulture),
                strings.Select(s => s.Replace(ListSeparator, ListSeparatorReplacement)));
        }

        public static string[] ToStringArray([NotNull] this string flatList)
        {
            if (flatList == null)
            {
                throw new ArgumentNullException(nameof(flatList));
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
            if (bag == null) throw new ArgumentNullException(nameof(bag));
            if (range == null) throw new ArgumentNullException(nameof(range));

            foreach (var item in range)
            {
                bag.Add(item);
            }
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static void SetDefaultConnectionLimit()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
        }

        private static readonly Lazy<int> s_maxConcurrentHttpRequests = new Lazy<int>(() =>
            Environment.ProcessorCount*GetOptionalValueFromConfig("MaxDegreeOfConcurrencyFactor", 12));

        public static int MaxConcurrentHttpRequests => s_maxConcurrentHttpRequests.Value;

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
            return Int32.TryParse(GetValueFromConfig(key), out int val)
                ? val
                : defaultValue;
        }

        public static bool GetOptionalValueFromConfig(string key, bool defaultValue)
        {
            return Boolean.TryParse(GetValueFromConfig(key), out bool val)
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

        public static bool OnAzure => Environment.GetEnvironmentVariable(WebSiteHostNameEnvironmentVariable) != null;

        public static bool InWebJob =>Environment.GetEnvironmentVariable("WEBJOBS_TYPE") != null;

        public static string WebJobName => Environment.GetEnvironmentVariable(WebjobNameEnvironmentVariable);

        public static string WebJobRunId => Environment.GetEnvironmentVariable(WebjobRunIdEnvironmentVariable);

        public static string WebsiteName => Environment.GetEnvironmentVariable(WebsiteNameEnvironmentVariable);

        public static void MockWebJobEnvironmentIfMissing(string name)
        {
            if (WebJobName != null || WebJobRunId != null || WebsiteName != null) //only mock if not in actual webjob context
            {
                return;
            }

            Environment.SetEnvironmentVariable(WebjobNameEnvironmentVariable, name);
            Environment.SetEnvironmentVariable(WebjobRunIdEnvironmentVariable, RandomGenerator.Next(0, int.MaxValue).ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable(WebsiteNameEnvironmentVariable, "[mock]");
        }

        private static string GetTriggeredRunUrl()
        {
            return String.Format(CultureInfo.InvariantCulture,
                "https://{0}.scm.azurewebsites.net/api/triggeredwebjobs/{1}/history/{2}", WebsiteName, WebJobName, WebJobRunId);
        }

        private static string GetTriggeredLogUrl()
        {
            return String.Format(CultureInfo.InvariantCulture,
                "https://{0}.scm.azurewebsites.net/vfs/data/jobs/triggered/{1}/{2}/output_log.txt", WebsiteName, WebJobName, WebJobRunId);
        }

        public static Task SendSuccessMail([NotNull] string description, [NotNull] string message, int ticks)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (message == null) throw new ArgumentNullException(nameof(message));

            return SendSuccessMail(description, message, GetTimeElapsedFromTickCount(ticks));
        }

        public static async Task SendSuccessMail([NotNull] string description, [NotNull] string message, TimeSpan duration)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var errors = EventSourceRegistrar.GetSessionErrors();
            var eventFormatter = new EventTextFormatter();
            string errorsText;
            using (var writer = new StringWriter())
            {
                foreach (var error in errors)
                {
                    eventFormatter.WriteEvent(error, writer);
                }
                errorsText = writer.ToString();
            }

            CommonEventSource.Log.SendSuccessMailStart(description);

            var from = new EmailAddress("webjobs@howlongtobeatsteam.com", "HLTBS webjobs");
            var to = new EmailAddress("contact@howlongtobeatsteam.com", "HLTBS contact");
            var subject = String.Format(CultureInfo.InvariantCulture, "{0} - Success ({1}) [{2}]",
                    WebJobName, duration, message + (errors.Length == 0 ? String.Empty : " (with session errors)"));
            var text = String.Format(CultureInfo.InvariantCulture, "{1}{0}Run ID: {2}{0}Start time: {3}{0}End time:{4}{0}Output log file: {5}{6}",
                Environment.NewLine, GetTriggeredRunUrl(), WebJobRunId, DateTime.UtcNow - duration, DateTime.UtcNow, GetTriggeredLogUrl(),
                errors.Length == 0 ? String.Empty : String.Format("{0}Session Errors:{0}{1}", Environment.NewLine, errorsText));
            var mail = MailHelper.CreateSingleEmail(from, to, subject, text, null);

            using (var response = await SendGridClient.PostAsJsonAsync<SendGridMessage, string>("https://api.sendgrid.com/v3/mail/send", mail))
            {
                CommonEventSource.Log.SendSuccessMailStop(
                    description, response.ResponseMessage.StatusCode, response.ResponseMessage.Headers.ToString(), response.Content);
            }
        }

        public static TimeSpan GetTimeElapsedFromTickCount(int tickCount)
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount - tickCount);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static void DataContractSerializeToFile<T>(T objectToSerialize, string fileName)
        {
            using (var fileStream = File.CreateText(fileName))
            using (var writer = new XmlTextWriter(fileStream))
            {
                new DataContractSerializer(typeof(T)).WriteObject(writer, objectToSerialize);
            }
        }

        public static async Task<int> RunProcessAsync(string fileName, string args)
        {
            using (var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            })
            {
                CommonEventSource.Log.RunProcessStart(fileName, args);
                var exitCode = await RunProcessAsync(process).ConfigureAwait(false);
                CommonEventSource.Log.RunProcessStop(fileName, args, exitCode);

                return exitCode;
            }
        }

        private static Task<int> RunProcessAsync(Process process)
        {
            var tcs = new TaskCompletionSource<int>();

            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);
            process.OutputDataReceived += (s, ea) => Console.WriteLine(ea.Data);
            process.ErrorDataReceived += (s, ea) => Console.WriteLine("ERR: " + ea.Data);

            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Could not start process: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}