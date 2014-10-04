using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Common
{
    public class HttpRetryClient : IDisposable
    {
        public static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(4.0);
        public static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3.0);
        public static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(120.0);

        private static readonly TransientErrorCatchAllStrategy CatchAllStrategy = new TransientErrorCatchAllStrategy();

        private readonly int m_retries;
        private readonly HttpClient m_client;

        public HttpRetryClient(int retries)
        {
            m_retries = retries;
            m_client = new HttpClient();
        }

        public Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, string url)
        {
            return RequestAsync(url, () => m_client.SendAsync(requestFactory()));
        }

        public Task<HttpResponseMessage> GetAsync(string url)
        {
            return RequestAsync(url, () => m_client.GetAsync(url));            
        }

        private Task<HttpResponseMessage> RequestAsync(string uri, Func<Task<HttpResponseMessage>> requester)
        {
            var retryPolicy = new RetryPolicy(CatchAllStrategy, new ExponentialBackoff(m_retries, MinBackoff, MaxBackoff, DefaultClientBackoff));

            retryPolicy.Retrying += (sender, args) =>
                Util.TraceWarning(
                    "Request to URI {0} failed due to: {1}. Retrying attempt #{2} will take place in {3}",
                    uri, args.LastException.Message, args.CurrentRetryCount, args.Delay);

            return retryPolicy.ExecuteAsync(async () =>
            {
                HttpResponseMessage response;
                try
                {
                    response = await requester().ConfigureAwait(false);
                }
                catch (TaskCanceledException e) //HttpClient throws this when a request times out (bug in HttpClient)
                {                               //If we don't catch it here ExecuteAsync will fail as well (bug in RetryPolicy)
                    throw new HttpRequestException("Request timed out", e);
                }
                return response.EnsureSuccessStatusCode();
            });
        }

        private sealed class TransientErrorCatchAllStrategy : ITransientErrorDetectionStrategy
        {
            public bool IsTransient(Exception ex)
            {
                return true;
            }
        }

        public void Dispose()
        {
            m_client.Dispose();
        }
    }
}