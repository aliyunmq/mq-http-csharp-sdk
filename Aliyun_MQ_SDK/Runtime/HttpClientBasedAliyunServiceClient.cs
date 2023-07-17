using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Aliyun.MQ.Runtime.Internal;
using Aliyun.MQ.Runtime.Internal.Auth;
using Aliyun.MQ.Runtime.Internal.Transform;
using Aliyun.MQ.Runtime.Pipeline;
using Aliyun.MQ.Runtime.Pipeline.ErrorHandler;
using Aliyun.MQ.Runtime.Pipeline.Handlers;
using Aliyun.MQ.Runtime.Pipeline.HttpHandler;
using Aliyun.MQ.Runtime.Pipeline.RetryHandler;
using ExecutionContext = Aliyun.MQ.Runtime.Internal.ExecutionContext;

namespace Aliyun.MQ.Runtime
{
    public abstract class HttpClientBasedAliyunServiceClient : IDisposable
    {
        private bool _disposed;

        protected RuntimePipeline RuntimePipeline { get; set; }
        protected ServiceCredentials Credentials { get; private set; }
        internal ClientConfig Config { get; private set; }

        internal HttpClientBasedAliyunServiceClient(ServiceCredentials credentials, ClientConfig config)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = config.ConnectionLimit;
            ServicePointManager.MaxServicePointIdleTime = config.MaxIdleTime;

            this.Config = config;
            this.Credentials = credentials;
            Signer = CreateSigner();

            Initialize();

            BuildRuntimePipeline();
        }

        protected IServiceSigner Signer { get; private set; }

        protected virtual void Initialize()
        {
        }

        protected virtual void CustomizeRuntimePipeline(RuntimePipeline pipeline)
        {
        }

        private void BuildRuntimePipeline()
        {
            // Replace HttpWebRequestFactory by HttpRequestMessageFactory
            var httpRequestFactory = new HttpRequestMessageFactory(Config);
            var httpHandler = new HttpHandler<HttpContent>(httpRequestFactory, this);

            this.RuntimePipeline = new RuntimePipeline(new List<IPipelineHandler>
                {
                    httpHandler,
                    new Unmarshaller(),
                    new ErrorHandler(),
                    new Signer(),
                    new CredentialsRetriever(this.Credentials),
                    new RetryHandler(new DefaultRetryPolicy(this.Config.MaxErrorRetry)),
                    new Marshaller()
                }
            );

            CustomizeRuntimePipeline(this.RuntimePipeline);
        }

        internal HttpClientBasedAliyunServiceClient(string accessKeyId, string secretAccessKey, ClientConfig config,
            string stsToken)
            : this(new BasicServiceCredentials(accessKeyId, secretAccessKey, stsToken), config)
        {
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract IServiceSigner CreateSigner();

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        internal TResponse Invoke<TRequest, TResponse>(TRequest request,
            IMarshaller<IRequest, WebServiceRequest> marshaller, ResponseUnmarshaller unmarshaller)
            where TRequest : WebServiceRequest
            where TResponse : WebServiceResponse
        {
            ThrowIfDisposed();

            var executionContext = new ExecutionContext(
                new RequestContext()
                {
                    ClientConfig = this.Config,
                    Marshaller = marshaller,
                    OriginalRequest = request,
                    Signer = Signer,
                    Unmarshaller = unmarshaller,
                    IsAsync = false
                },
                new ResponseContext()
            );

            var response = (TResponse)this.RuntimePipeline.InvokeSync(executionContext).Response;
            return response;
        }

        internal System.Threading.Tasks.Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest request,
            IMarshaller<IRequest, WebServiceRequest> marshaller,
            ResponseUnmarshaller unmarshaller,
            System.Threading.CancellationToken cancellationToken)
            where TRequest : WebServiceRequest
            where TResponse : WebServiceResponse, new()
        {
            var executionContext = new ExecutionContext(
                new RequestContext()
                {
                    ClientConfig = this.Config,
                    Marshaller = marshaller,
                    OriginalRequest = request,
                    Unmarshaller = unmarshaller,
                    IsAsync = true,
                    CancellationToken = cancellationToken,
                    Signer = Signer,
                },
                new ResponseContext()
            );
            return this.RuntimePipeline.InvokeAsync<TResponse>(executionContext);
        }
    }
}