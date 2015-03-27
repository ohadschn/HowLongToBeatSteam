using System;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Common.Util
{
    public delegate void AttemptFailureHandler(Exception lastException, int currentRetryCount, TimeSpan delay);

    public class ExponentialBackoff : RetryStrategy
    {
        private readonly int m_retryCount;
        private readonly int m_minBackoff;
        private readonly int m_maxBackoffMin;
        private readonly int m_maxBackoffMax;
        private readonly int m_deltaBackoffMin;
        private readonly int m_deltaBackoffMax;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class. 
        /// </summary>
        public ExponentialBackoff()
            : this(DefaultClientRetryCount, DefaultMinBackoff, DefaultMaxBackoff, DefaultClientBackoff)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified retry settings.
        /// </summary>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
            : this(null, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name and retry settings.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
            : this(name, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name, retry settings, and fast retry option.
        /// </summary>
        /// <param name="name">The name of the retry strategy.</param>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum backoff time</param>
        /// <param name="maxBackoff">The maximum backoff time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
        /// <param name="firstFastRetry">true to immediately retry in the first attempt; otherwise, false. The subsequent retries will remain subject to the configured retry interval.</param>
        public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff, bool firstFastRetry)
            : base(name, firstFastRetry)
        {
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException("retryCount");
            }
            if (minBackoff.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("minBackoff");
            }

            if (maxBackoff.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("maxBackoff");
            }
            if (deltaBackoff.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("deltaBackoff");
            }
            if (minBackoff.TotalMilliseconds > maxBackoff.TotalMilliseconds)
            {
                throw new ArgumentOutOfRangeException("minBackoff",
                    String.Format(CultureInfo.InvariantCulture, "min backoff {0} larger than max backoff {1}", minBackoff.TotalMilliseconds, maxBackoff.TotalMilliseconds));
            }

            //all checked integers + shared random + report issue/fix
            m_retryCount = retryCount;
            m_minBackoff = checked((int)(minBackoff.TotalMilliseconds));
            m_maxBackoffMin = checked((int)(maxBackoff.TotalMilliseconds * 0.6));
            m_maxBackoffMax = checked((int)(maxBackoff.TotalMilliseconds));
            m_deltaBackoffMin = checked((int)(deltaBackoff.TotalMilliseconds * 0.8));
            m_deltaBackoffMax = checked((int)(deltaBackoff.TotalMilliseconds * 1.2));
        }

        /// <summary>
        /// Returns the corresponding ShouldRetry delegate.
        /// </summary>
        /// <returns>The ShouldRetry delegate.</returns>
        public override ShouldRetry GetShouldRetry()
        {
            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                if (currentRetryCount < m_retryCount)
                {
                    var delta = (Math.Pow(2.0, currentRetryCount) - 1.0) * RandomGenerator.Next(m_deltaBackoffMin, m_deltaBackoffMax);
                    var interval = Math.Min(m_minBackoff + delta, RandomGenerator.Next(m_maxBackoffMin, m_maxBackoffMax));

                    retryInterval = TimeSpan.FromMilliseconds(interval);
                    return true;
                }

                retryInterval = TimeSpan.Zero;
                return false;
            };
        }

        public static Task<T> ExecuteAsyncWithExponentialRetries<T>(
            Func<Task<T>> executor,
            AttemptFailureHandler attemptFailureHandler,
            Predicate<Exception> transientErrorDetector,
            int retries,
            TimeSpan minBackoff,
            TimeSpan maxBackoff,
            TimeSpan deltaBackoff,
            CancellationToken ct)
        {
            var retryPolicy = new RetryPolicy(
                new GenericTransientErrorDetectionStrategy(transientErrorDetector),
                new ExponentialBackoff(retries, minBackoff, maxBackoff, deltaBackoff));

            retryPolicy.Retrying += (sender, args) => attemptFailureHandler(args.LastException, args.CurrentRetryCount, args.Delay);

            return retryPolicy.ExecuteAsync(executor, ct);
        }
    }
}
