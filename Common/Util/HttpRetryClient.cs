﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Common.Util
{
    public sealed class HttpRetryClient : IDisposable
    {
        public static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3.0);
        public static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(120.0);
        public static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(4.0);

        private static readonly TransientErrorCatchAllStrategy CatchAllStrategy = new TransientErrorCatchAllStrategy();

        private readonly int m_retries;
        private readonly HttpClient m_client;

        public AuthenticationHeaderValue DefaultRequestAuthorization
        {
            get { return m_client.DefaultRequestHeaders.Authorization; }
            set { m_client.DefaultRequestHeaders.Authorization = value; }
        }

        public HttpRetryClient(int retries)
        {
            m_retries = retries;
            m_client = new HttpClient();
        }

        public Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, string url)
        {
            return SendAsync(requestFactory, new Uri(url));            
        }

        public Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            return RequestAsync(url, () => m_client.SendAsync(requestFactory())); 
        }

        public Task<HttpResponseMessage> GetAsync(string url)
        {
            return GetAsync(new Uri(url));
        }

        public Task<HttpResponseMessage> GetAsync(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            return RequestAsync(url, () => m_client.GetAsync(url));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
        {
            return PostAsJsonAsync(new Uri(requestUri), value);
        }

        public Task<HttpResponseMessage> PostAsJsonAsync<T>(Uri requestUri, T value)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException("requestUri");
            }

            return RequestAsync(requestUri, () => m_client.PostAsJsonAsync(requestUri, value));
        }

        private Task<HttpResponseMessage> RequestAsync(Uri uri, Func<Task<HttpResponseMessage>> requester)
        {
            var retryPolicy = new RetryPolicy(CatchAllStrategy, new ExponentialBackoff(m_retries, MinBackoff, MaxBackoff, DefaultClientBackoff));

            retryPolicy.Retrying += (sender, args) => CommonEventSource.Log.HttpRequestFailed(uri, args.LastException, args.CurrentRetryCount, args.Delay);

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