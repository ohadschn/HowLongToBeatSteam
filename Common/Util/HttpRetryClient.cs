using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using static System.FormattableString;
using JetBrains.Annotations;

namespace Common.Util
{
    public sealed class HttpResponseWithContent<T> : IDisposable
    {
        public HttpResponseMessage ResponseMessage { get; }
        public T Content { get;  }

        public HttpResponseWithContent(HttpResponseMessage responseMessage, T content)
        {
            ResponseMessage = responseMessage;
            Content = content;
        }

        public void Dispose()
        {
            ResponseMessage.Dispose();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Note that request and content factories are required below, since the HttpClient will dispose of them after the first try
    /// </summary>
    public sealed class HttpRetryClient : IDisposable
    {
        public const string BearerAuthorizationScheme = "Bearer";

        public static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(3.0);
        public static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(120.0);
        public static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(4.0);

        private readonly int m_retries;
        private readonly HttpClient m_client;

        public AuthenticationHeaderValue DefaultRequestAuthorization
        {
            get => m_client.DefaultRequestHeaders.Authorization;
            set => m_client.DefaultRequestHeaders.Authorization = value;
        }

        [PublicAPI]
        public HttpRetryClient(int retries)
        {
            m_retries = retries;
            m_client = new HttpClient();
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> SendAsync<T>([NotNull] Func<HttpRequestMessage> requestFactory, [NotNull] string url)
        {
            if (requestFactory == null) throw new ArgumentNullException(nameof(requestFactory));
            if (url == null) throw new ArgumentNullException(nameof(url));

            return SendAsync<T>(requestFactory, new Uri(url), CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> SendAsync<T>([NotNull] Func<HttpRequestMessage> requestFactory, [NotNull] string url, CancellationToken ct)
        {
            if (requestFactory == null) throw new ArgumentNullException(nameof(requestFactory));
            if (url == null) throw new ArgumentNullException(nameof(url));

            return SendAsync<T>(requestFactory, new Uri(url), ct);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> SendAsync<T>([NotNull] Func<HttpRequestMessage> requestFactory, [NotNull] Uri url)
        {
            if (requestFactory == null) throw new ArgumentNullException(nameof(requestFactory));
            if (url == null) throw new ArgumentNullException(nameof(url));

            return SendAsync<T>(requestFactory, url, CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> SendAsync<T>([NotNull] Func<HttpRequestMessage> requestFactory, [NotNull] Uri url, CancellationToken ct)
        {
            if (requestFactory == null) throw new ArgumentNullException(nameof(requestFactory));
            if (url == null) throw new ArgumentNullException(nameof(url));

            return RequestAsync<T>(url, () => m_client.SendAsync(requestFactory(), ct), ct); 
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> GetAsync<T>([NotNull] string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return GetAsync<T>(new Uri(url), CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> GetAsync<T>([NotNull] string url, CancellationToken ct)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return GetAsync<T>(new Uri(url), ct);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> GetAsync<T>([NotNull] Uri url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return GetAsync<T>(url, CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> GetAsync<T>([NotNull] Uri url, CancellationToken ct)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return RequestAsync<T>(url, () => m_client.GetAsync(url, ct), ct);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> PostAsync<T>([NotNull] string uri, [NotNull] Func<HttpContent> contentFactory)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (contentFactory == null) throw new ArgumentNullException(nameof(contentFactory));

            return PostAsync<T>( new Uri(uri), contentFactory);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<T>> PostAsync<T>([NotNull] Uri uri, [NotNull] Func<HttpContent> contentFactory)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (contentFactory == null) throw new ArgumentNullException(nameof(contentFactory));

            return RequestAsync<T>(uri, () => m_client.PostAsync(uri, contentFactory()), CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<TResponse>> PostAsJsonAsync<TValue, TResponse>([NotNull] string requestUri, [NotNull] TValue value)
        {
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return PostAsJsonAsync<TValue, TResponse>(new Uri(requestUri), value, CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<TResponse>> PostAsJsonAsync<TValue, TResponse>([NotNull] string requestUri, [NotNull] TValue value, CancellationToken ct)
        {
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return PostAsJsonAsync<TValue, TResponse>(new Uri(requestUri), value, ct);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<TResponse>> PostAsJsonAsync<TValue, TResponse>([NotNull] Uri requestUri, [NotNull] TValue value)
        {
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return PostAsJsonAsync<TValue, TResponse>(requestUri, value, CancellationToken.None);
        }

        [PublicAPI]
        public Task<HttpResponseWithContent<TResponse>> PostAsJsonAsync<TValue, TResponse>([NotNull] Uri requestUri, [NotNull] TValue value, CancellationToken ct)
        {
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return RequestAsync<TResponse>(requestUri, () => m_client.PostAsJsonAsync(requestUri, value, ct), ct);
        }

        private Task<HttpResponseWithContent<T>> RequestAsync<T>(Uri uri, Func<Task<HttpResponseMessage>> requester, CancellationToken ct)
        {
            int attempt = 0;
            return ExponentialBackoff.ExecuteAsyncWithExponentialRetries(async () =>
            {

                CommonEventSource.Log.SendHttpRequestStart(uri, ++attempt, m_retries + 1);
                HttpResponseMessage response;
                try
                {
                    response = await requester().ConfigureAwait(false);
                }
                catch (TaskCanceledException e) when (!ct.IsCancellationRequested) 
                {
                    //HttpClient throws TaskCanceledException when a request times out (bug in HttpClient)
                    //Since we only want to cancel the entire operation if our CT has been signaled, we identify this case
                    //and convert it to the correct (HTTP) exception - otherwise ExecuteAsync will assume we want to cancel everything
                    throw new HttpRequestException(Invariant($"Request to '{uri}' timed out"), e);
                }
                CommonEventSource.Log.SendHttpRequestStop(uri, attempt, m_retries + 1);

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException e)
                {
                    throw new HttpRequestException(Invariant($"HTTP request to '{uri}' failed on attempt #{attempt} / {m_retries + 1}"), e);
                }

                object content;
                if (typeof(T) == typeof(Stream))
                {
                    ct.ThrowIfCancellationRequested(); //ReadAsStreamAsync doesn't take a CT so we make the best effort here
                    content = await response.Content.ReadAsStreamAsync();
                }
                else
                {
                    content = await response.Content.ReadAsAsync<T>(ct);
                }

                return new HttpResponseWithContent<T>(response, (T)content);
            },
            (lastException, retryCount, delay) => CommonEventSource.Log.HttpRequestFailed(uri, lastException, retryCount, m_retries, delay),
                e =>
                {
                    var transient = e is HttpRequestException || e is WebException;
                    CommonEventSource.Log.HttpRequestFailedWithException(uri, e, transient, attempt, m_retries + 1);
                    return transient;
                }, m_retries, MinBackoff, MaxBackoff, DefaultClientBackoff, ct);
        }

        public void Dispose()
        {
            m_client.Dispose();
        }
    }
}