﻿using System;
using System.Net;
using System.Runtime.InteropServices;
using Aliyun.MQ.Runtime.Pipeline.HttpHandler;
using Aliyun.MQ.Util;

namespace Aliyun.MQ.Runtime
{
    public abstract partial class ClientConfig
    {
        private Uri _regionEndpoint;

        private readonly string _userAgent = AliyunSDKUtils.SDKUserAgent;
        private int _maxErrorRetry = 3;
        private int _bufferSize = AliyunSDKUtils.DefaultBufferSize;
        private ICredentials _proxyCredentials;
        private bool _disableLogging = true;
        private TimeSpan? _timeout = TimeSpan.FromSeconds(3);
        private bool _allowAutoRedirect = false;

        private string _proxyHost;
        private int _proxyPort = -1;
        private int? _connectionLimit = 200;
        private int? _maxIdleTime;
        private TimeSpan? _readWriteTimeout = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan InfiniteTimeout = TimeSpan.FromMilliseconds(-1);
        private static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        /// <summary>
        /// Gets Service Version.
        /// </summary>
        public abstract string ServiceVersion
        {
            get;
        }

        /// <summary>
        /// Gets Service Name.
        /// </summary>
        public abstract string ServiceName
        {
            get;
        }

        /// <summary>
        /// Gets of the UserAgent property.
        /// </summary>
        public string UserAgent
        {
            get { return this._userAgent; }
        }

        /// <summary>
        /// Gets and sets the RegionEndpoint property. 
        /// </summary>
        public Uri RegionEndpoint
        {
            get
            {
                if (_regionEndpoint == null)
                {
                    throw new ArgumentException("Endpoint must be specified.");
                }
                return _regionEndpoint;
            }
            set { _regionEndpoint = value; }
        }

        /// <summary>
        /// Gets and sets of the MaxErrorRetry property.
        /// </summary>
        public int MaxErrorRetry
        {
            get { return this._maxErrorRetry; }
            set { this._maxErrorRetry = value; }
        }

        /// <summary>
        /// Gets and Sets the BufferSize property.
        /// The BufferSize controls the buffer used to read in from input streams and write 
        /// out to the request.
        /// </summary>
        public int BufferSize
        {
            get { return this._bufferSize; }
            set { this._bufferSize = value; }
        }

        /// <summary>
        /// This flag controls if .NET HTTP infrastructure should follow redirection responses.
        /// </summary>
        internal bool AllowAutoRedirect
        {
            get
            {
                return this._allowAutoRedirect;
            }
            set
            {
                this._allowAutoRedirect = value;
            }
        }

        /// <summary>
        /// Flag on whether to completely disable logging for this client or not.
        /// </summary>
        internal bool DisableLogging
        {
            get { return this._disableLogging; }
            set { this._disableLogging = value; }
        }

        /// <summary>
        /// Credentials to use with a proxy.
        /// </summary>
        public ICredentials ProxyCredentials
        {
            get { return this._proxyCredentials; }
            set { this._proxyCredentials = value; }
        }

        #region Constructor 
        public ClientConfig()
        {
            Initialize();
        }
        #endregion

        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Gets and sets of the ProxyHost property.
        /// </summary>
        public string ProxyHost
        {
            get { return this._proxyHost; }
            set { this._proxyHost = value; }
        }


        /// <summary>
        /// Gets and sets of the ProxyPort property.
        /// </summary>
        public int ProxyPort
        {
            get { return this._proxyPort; }
            set { this._proxyPort = value; }
        }

        /// <summary>
        /// Gets and sets the max idle time set on the ServicePoint for the WebRequest.
        /// </summary>
        public int MaxIdleTime
        {
            get { return AliyunSDKUtils.GetMaxIdleTime(this._maxIdleTime); }
            set { this._maxIdleTime = value; }
        }

        /// <summary>
        /// Gets and sets the connection limit set on the ServicePoint for the WebRequest.
        /// </summary>
        public int ConnectionLimit
        {
            get { return AliyunSDKUtils.GetConnectionLimit(this._connectionLimit); }
            set { this._connectionLimit = value; }
        }

        /// <summary>
        /// Overrides the default read-write timeout value.
        /// </summary>
        public TimeSpan? ReadWriteTimeout
        {
            get { return this._readWriteTimeout; }
            set
            {
                ValidateTimeout(value);
                this._readWriteTimeout = value;
            }
        }

        /// <summary>
        /// Overrides the default request timeout value.
        /// </summary>
        public TimeSpan? Timeout
        {
            get { return this._timeout; }
            set
            {
                ValidateTimeout(value);
                this._timeout = value;
            }
        }

        internal static void ValidateTimeout(TimeSpan? timeout)
        {
            if (!timeout.HasValue)
            {
                throw new ArgumentNullException("timeout");
            }

            if (timeout != InfiniteTimeout && (timeout <= TimeSpan.Zero || timeout > MaxTimeout))
            {
                throw new ArgumentOutOfRangeException("timeout");
            }
        }

        /// <summary>
        /// Returns the request timeout value if its value is set, 
        /// else returns client timeout value.
        /// </summary>        
        internal static TimeSpan? GetTimeoutValue(TimeSpan? clientTimeout, TimeSpan? requestTimeout)
        {
            return requestTimeout.HasValue ? requestTimeout
                : (clientTimeout.HasValue ? clientTimeout : null);
        }
        
        /// <summary>
        /// <para>
        /// This is a switch used for performance testing and is not intended for production applications 
        /// to change. This switch may be removed in a future version of the SDK as the .NET Core platform matures.
        /// </para>
        /// <para>
        /// If true, the HttpClient is cached and reused for every request made by the service client 
        /// and shared with other service clients.
        /// </para>
        /// <para>
        /// For the .NET Core platform this is default to true because the HttpClient manages the connection
        /// pool.
        /// </para>
        /// </summary>
        public bool CacheHttpClient {get; set;} = true;
        
        /// <summary>
        /// HttpClientFactory used to create new HttpClients.
        /// If null, an HttpClient will be created by the SDK.
        /// Note that IClientConfig members such as ProxyHost, ProxyPort, GetWebProxy, and AllowAutoRedirect
        /// will have no effect unless they're used explicitly by the HttpClientFactory implementation.
        ///
        /// See https://docs.microsoft.com/en-us/xamarin/cross-platform/macios/http-stack?context=xamarin/ios and
        /// https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/http-stack?context=xamarin%2Fcross-platform&tabs=macos#ssltls-implementation-build-option
        /// for guidance on creating HttpClients for your platform.
        /// </summary>
        public HttpClientFactory HttpClientFactory { get; set; } = null;
        
        /// <summary>
        /// Returns true if the clients should be cached by HttpRequestMessageFactory, false otherwise.
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <returns></returns>
        internal static bool CacheHttpClients(ClientConfig clientConfig)
        {
            if (clientConfig.HttpClientFactory == null)
                return clientConfig.CacheHttpClient;
            else
                return clientConfig.HttpClientFactory.UseSDKHttpClientCaching(clientConfig);
        }
        
        /// <summary>
        /// Returns true if the SDK should dispose HttpClients after one use, false otherwise.
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <returns></returns>
        internal static bool DisposeHttpClients(ClientConfig clientConfig)
        {
            if (clientConfig.HttpClientFactory == null)
                return !clientConfig.CacheHttpClient;
            else
                return clientConfig.HttpClientFactory.DisposeHttpClientsAfterUse(clientConfig);
        }

        private int? _httpClientCacheSize;
        /// <summary>
        /// If CacheHttpClient is set to true then HttpClientCacheSize controls the number of HttpClients cached.
        /// <para>
        /// On Windows the default value is 1 since the underlying native implementation does not have throttling constraints
        /// like the non Windows Curl based implementation. For non Windows based platforms the default is the value return from 
        /// System.Environment.ProcessorCount.
        /// </para>
        /// </summary>
        public int HttpClientCacheSize
        {
            get
            {
                if(_httpClientCacheSize.HasValue)
                {
                    return _httpClientCacheSize.Value;
                }

                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 1 : Environment.ProcessorCount;
            }
            set => _httpClientCacheSize = value;
        }
        
        /// <summary>
        /// Get or set the value to use for <see cref="System.Net.Http.HttpClientHandler.MaxConnectionsPerServer"/> on requests.
        /// If this property is null, <see cref="System.Net.Http.HttpClientHandler.MaxConnectionsPerServer"/>
        /// will be left at its default value of <see cref="int.MaxValue"/>.
        /// </summary>
        public int? MaxConnectionsPerServer
        {
            get;
            set;
        }
        
        /// <summary>
        /// Generates a <see cref="CancellationToken"/> based on the value
        /// for <see cref="DefaultConfiguration.TimeToFirstByteTimeout"/>.
        /// <para />
        /// NOTE: <see cref="HttpWebRequestMessage.GetResponseAsync"/> uses 
        /// </summary>
        // internal CancellationToken BuildDefaultCancellationToken()
        // {
            // // legacy mode never had a working cancellation token, so keep it to default()
            // if (DefaultConfiguration.Name == Runtime.DefaultConfigurationMode.Legacy)
            //     return default(CancellationToken);
            //
            // // TimeToFirstByteTimeout is not a perfect match with HttpWebRequest/HttpClient.Timeout.  However, given
            // // that both are configured to only use Timeout until the Response Headers are downloaded, this value
            // // provides a reasonable default value.
            // var cancelTimeout = DefaultConfiguration.TimeToFirstByteTimeout;
            //
            // return cancelTimeout.HasValue
            //     ? new CancellationTokenSource(cancelTimeout.Value).Token
            //     : default(CancellationToken);
        // }
    }
}
