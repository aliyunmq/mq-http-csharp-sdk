using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Aliyun.MQ.Runtime.Internal.Transform;
using Aliyun.MQ.Util;
using NLog;

namespace Aliyun.MQ.Runtime.Pipeline.HttpHandler
{
    
        [CLSCompliant(false)]
    public abstract class HttpClientFactory
    {
        /// <summary>
        /// Create and configure an HttpClient.
        /// </summary>
        /// <returns></returns>
        public abstract HttpClient CreateHttpClient(ClientConfig clientConfig);

        /// <summary>
        /// If true the SDK will internally cache the clients created by CreateHttpClient.
        /// If false the sdk will not cache the clients.
        /// Override this method to return false if your HttpClientFactory will handle its own caching
        /// or if you don't want clients to be cached.
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <returns></returns>
        public virtual bool UseSDKHttpClientCaching(ClientConfig clientConfig)
        {
            return clientConfig.CacheHttpClient;
        }

        /// <summary>
        /// Determines if the SDK will dispose clients after they're used.
        /// If HttpClients are cached, either by the SDK or by your HttpClientFactory, this should be false.
        /// If there is no caching then this should be true.
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <returns></returns>
        public virtual bool DisposeHttpClientsAfterUse(ClientConfig clientConfig)
        {
            return !UseSDKHttpClientCaching(clientConfig);
        }

        /// <summary>
        /// Returns a string that's used to group equivalent HttpClients into caches.
        /// This method isn't used unless UseSDKHttpClientCaching returns true;
        /// 
        /// A null return value signals the SDK caching mechanism to cache HttpClients per SDK client.
        /// So when the SDK client is disposed, the HttpClients are as well.
        /// 
        /// A non-null return value signals the SDK that HttpClients created with the given clientConfig
        /// should be cached and reused globally.  ClientConfigs that produce the same result for
        /// GetConfigUniqueString will be grouped together and considered equivalent for caching purposes.
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <returns></returns>
        public virtual string GetConfigUniqueString(ClientConfig clientConfig)
        {
            return null;
        }
    }
    
    public class HttpRequestMessageFactory : IHttpRequestFactory<HttpContent>
    {
        private static readonly Logger Logger = MqLogManager.Instance.GetCurrentClassLogger();
        // This is the global cache of HttpClient for service clients that are using 
        static readonly ReaderWriterLockSlim _httpClientCacheRWLock = new ReaderWriterLockSlim();
        static readonly IDictionary<string, HttpClientCache> _httpClientCaches = new Dictionary<string, HttpClientCache>();

        
        private HttpClientCache _httpClientCache;
        private bool _useGlobalHttpClientCache;
        private ClientConfig _clientConfig;
        
        /// <summary>
        /// The constructor for HttpRequestMessageFactory.
        /// </summary>
        /// <param name="clientConfig">Configuration setting for a client.</param>
        public HttpRequestMessageFactory(ClientConfig clientConfig)
        {
            _clientConfig = clientConfig;
        }

        public IHttpRequest<HttpContent> CreateHttpRequest(Uri requestUri)
        {
            HttpClient httpClient = null;
            if (ClientConfig.CacheHttpClients(_clientConfig))
            {
                if (_httpClientCache == null)
                {
                    _useGlobalHttpClientCache = false;

                    _httpClientCacheRWLock.EnterWriteLock();
                    try
                    {
                        if (_httpClientCache == null)
                        {
                            _httpClientCache = CreateHttpClientCache(_clientConfig);
                        }
                    }
                    finally
                    {
                        _httpClientCacheRWLock.ExitWriteLock();
                    }
                }

                // Now that we have a HttpClientCache from either the global cache or just created a new HttpClientCache
                // get the next HttpClient to be used for making a web request.
                httpClient = _httpClientCache.GetNextClient();
            }
            else
            {
                httpClient = CreateHttpClient(_clientConfig);
            }

            return new HttpWebRequestMessage(httpClient, requestUri, _clientConfig);
        }

        public void Dispose()
        {
            // Dispose(true);
            // GC.SuppressFinalize(this);
        }
        
        private static HttpClientCache CreateHttpClientCache(ClientConfig clientConfig)
        {
            var clients = new HttpClient[clientConfig.HttpClientCacheSize];
            for(int i = 0; i < clients.Length; i++)
            {
                clients[i] = CreateHttpClient(clientConfig);
            }
            var cache = new HttpClientCache(clients);
            return cache;
        }
        
        private static HttpClient CreateHttpClient(ClientConfig clientConfig)
        {
            if (clientConfig.HttpClientFactory == null)
            {
                return CreateManagedHttpClient(clientConfig);
            }
            else
            {
                return clientConfig.HttpClientFactory.CreateHttpClient(clientConfig);
            }
        }
        
                 /// <summary>
         /// Create and configure a managed HttpClient instance.
         /// The use of HttpClientHandler in the constructor for HttpClient implicitly creates a managed HttpClient.
         /// </summary>
         /// <param name="clientConfig"></param>
         /// <returns></returns>
        private static HttpClient CreateManagedHttpClient(ClientConfig clientConfig)
        {
            var httpMessageHandler = new HttpClientHandler();

            if (clientConfig.MaxConnectionsPerServer.HasValue)
                httpMessageHandler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;

            try
            {
                // If HttpClientHandler.AllowAutoRedirect is set to true (default value),
                // redirects for GET requests are automatically followed and redirects for POST
                // requests are thrown back as exceptions.
                // If HttpClientHandler.AllowAutoRedirect is set to false (e.g. S3),
                // redirects are returned as responses.
                httpMessageHandler.AllowAutoRedirect = clientConfig.AllowAutoRedirect;

                // Disable automatic decompression when Content-Encoding header is present
                httpMessageHandler.AutomaticDecompression = DecompressionMethods.None;
            }
            catch (PlatformNotSupportedException pns)
            {
                Logger.Debug(pns, $"The current runtime does not support modifying the configuration of HttpClient.");
            }
            

            var httpClient = new HttpClient(httpMessageHandler);
            
            if (clientConfig.Timeout.HasValue)
            {
                // Timeout value is set to ClientConfig.MaxTimeout for S3 and Glacier.
                // Use default value (100 seconds) for other services.
                httpClient.Timeout = clientConfig.Timeout.Value;
            }

            return httpClient;
        }
    }
    
    /// <summary>
    /// A cache of HttpClient objects. The GetNextClient method does a round robin cycle through the clients
    /// to distribute the load even across.
    /// </summary>
    public class HttpClientCache : IDisposable
    {
        HttpClient[] _clients;

        /// <summary>
        /// Constructs a container for a cache of HttpClient objects
        /// </summary>
        /// <param name="clients">The HttpClient to cache</param>
        public HttpClientCache(HttpClient[] clients)
        {
            _clients = clients;
        }

        private int count = 0;
        /// <summary>
        /// Returns the next HttpClient using a round robin rotation. It is expected that individual clients will be used
        /// by more then one Thread.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetNextClient()
        {
            if (_clients.Length == 1)
            {
                return _clients[0];
            }
            else
            {
                int next = Interlocked.Increment(ref count);
                int nextIndex = Math.Abs(next % _clients.Length);
                return _clients[nextIndex];
            }
        }

        /// <summary>
        /// Disposes the HttpClientCache.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the HttpClientCache
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_clients != null)
                {
                    foreach (var client in _clients)
                    {
                        client.Dispose();
                    }
                }
            }
        }
    }

    public class HttpWebRequestMessage : IHttpRequest<HttpContent>
    {
        /// <summary>
        /// Set of content header names.
        /// </summary>
        private static HashSet<string> ContentHeaderNames = new HashSet<string>
        {
            Constants.ContentLengthHeader,
            Constants.ContentTypeHeader,
            Constants.ContentRangeHeader,
            Constants.ContentMD5Header,
            Constants.ContentEncodingHeader,
            Constants.ContentDispositionHeader,
            Constants.Expires
        };
        
        private bool _disposed;
        private HttpRequestMessage _request;
        private HttpClient _httpClient;
        private ClientConfig _clientConfig;
        
        /// <summary>
        /// The constructor for HttpWebRequestMessage.
        /// </summary>
        /// <param name="httpClient">The HttpClient used to make the request.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="config">The service client config.</param>
        public HttpWebRequestMessage(HttpClient httpClient, Uri requestUri, ClientConfig config)
        {
            _clientConfig = config;
            _httpClient = httpClient;

            _request = new HttpRequestMessage();
            _request.RequestUri = requestUri;
        }

        /// <summary>
        /// Disposes the HttpWebRequestMessage.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_request != null)
                    _request.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// The HTTP method or verb.
        /// </summary>
        public string Method
        {
            get { return _request.Method.Method; }
            set { _request.Method = new System.Net.Http.HttpMethod(value); }
        }
        
        /// <summary>
        /// The request URI.
        /// </summary>
        public Uri RequestUri
        {
            get { return _request.RequestUri; }
        }
        
        public void ConfigureRequest(IRequestContext requestContext)
        {
            // Configure the Expect 100-continue header
            if (requestContext != null && requestContext.OriginalRequest != null)
            {
                _request.Headers.ExpectContinue = requestContext.OriginalRequest.GetExpect100Continue();
            }
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            foreach (var kvp in headers)
            {
                if (ContentHeaderNames.Contains(kvp.Key))
                    continue;
                // if (ContentHeaderNames.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                //     continue;
                _request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
            // TODO
            _request.Headers.Add("Connection", "Keep-Alive");
        }

        public HttpContent GetRequestContent()
        {
            try
            {
                return this.GetRequestContentAsync().Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }
        
        /// <summary>
        /// Gets a handle to the request content.
        /// </summary>
        /// <returns></returns>
        public System.Threading.Tasks.Task<HttpContent> GetRequestContentAsync()
        {
            return System.Threading.Tasks.Task.FromResult(_request.Content);
        }
        
                /// <summary>
        /// Returns the HTTP response.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task<IWebResponseData> GetResponseAsync(System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                var responseMessage = await _httpClient.SendAsync(_request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                bool disposeClient = ClientConfig.DisposeHttpClients(_clientConfig);
                // If AllowAutoRedirect is set to false, HTTP 3xx responses are returned back as response.
                if (!_clientConfig.AllowAutoRedirect &&
                    responseMessage.StatusCode >= HttpStatusCode.Ambiguous &&
                    responseMessage.StatusCode < HttpStatusCode.BadRequest)
                {
                    return new HttpClientResponseData(responseMessage, _httpClient, disposeClient);
                }

                if (!responseMessage.IsSuccessStatusCode)
                {
                    // For all responses other than HTTP 2xx, return an exception.
                    throw new HttpErrorResponseException(
                        new HttpClientResponseData(responseMessage, _httpClient, disposeClient));
                }

                return new HttpClientResponseData(responseMessage, _httpClient, disposeClient);
            }
            catch (HttpRequestException httpException)
            {
                if (httpException.InnerException != null)
                {
                    if (httpException.InnerException is IOException)
                    {
                        throw httpException.InnerException;
                    }
                }

                throw;
            }
            catch (OperationCanceledException canceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    //OperationCanceledException thrown by HttpClient not the CancellationToken supplied by the user.
                    //This exception can wrap at least IOExceptions, ObjectDisposedExceptions and should be retried.
                    //Throw the underlying exception if it exists.
                    if(canceledException.InnerException != null)
                    {
                        throw canceledException.InnerException;
                    }
                }

                throw;
            }
        }

        public IWebResponseData GetResponse()
        {
            try
            {
                return this.GetResponseAsync(System.Threading.CancellationToken.None).Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public void WriteToRequestBody(HttpContent requestContent, Stream contentStream, IDictionary<string, string> contentHeaders,
            IRequestContext requestContext)
        {
            _request.Content = new StreamContent(contentStream, requestContext.ClientConfig.BufferSize);
            _request.Content.Headers.ContentLength = contentStream.Length;
            WriteContentHeaders(contentHeaders);
        }

        public void WriteToRequestBody(HttpContent requestContent, byte[] content, IDictionary<string, string> contentHeaders)
        {
            content ??= Encoding.UTF8.GetBytes("a");
            _request.Content = new ByteArrayContent(content);
            _request.Content.Headers.ContentLength = content.Length;
            WriteContentHeaders(contentHeaders);
        }
        
        
        private void WriteContentHeaders(IDictionary<string, string> contentHeaders)
        {
            _request.Content.Headers.ContentType =
                MediaTypeHeaderValue.Parse(contentHeaders[Constants.ContentTypeHeader]);

            if (contentHeaders.ContainsKey(Constants.ContentRangeHeader))
                _request.Content.Headers.TryAddWithoutValidation(Constants.ContentRangeHeader,
                    contentHeaders[Constants.ContentRangeHeader]);

            if (contentHeaders.ContainsKey(Constants.ContentMD5Header))
                _request.Content.Headers.TryAddWithoutValidation(Constants.ContentMD5Header,
                    contentHeaders[Constants.ContentMD5Header]);

            if (contentHeaders.ContainsKey(Constants.ContentEncodingHeader))
                _request.Content.Headers.TryAddWithoutValidation(Constants.ContentEncodingHeader,
                    contentHeaders[Constants.ContentEncodingHeader]);

            if (contentHeaders.ContainsKey(Constants.ContentDispositionHeader))
                _request.Content.Headers.TryAddWithoutValidation(Constants.ContentDispositionHeader,
                    contentHeaders[Constants.ContentDispositionHeader]);

            DateTime expires;
            if (contentHeaders.ContainsKey(Constants.Expires) &&
                DateTime.TryParse(contentHeaders[Constants.Expires], CultureInfo.InvariantCulture, DateTimeStyles.None, out expires))
                _request.Content.Headers.Expires = expires;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginGetRequestContent(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public HttpContent EndGetRequestContent(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public IWebResponseData EndGetResponse(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }
    }
}