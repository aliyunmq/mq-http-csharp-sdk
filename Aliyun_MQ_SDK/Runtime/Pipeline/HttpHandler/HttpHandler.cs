﻿using System;
using System.Globalization;
using System.Text;
using Aliyun.MQ.Runtime.Internal;
using Aliyun.MQ.Runtime.Internal.Transform;
using Aliyun.MQ.Runtime.Internal.Util;
using Aliyun.MQ.Util;

namespace Aliyun.MQ.Runtime.Pipeline.HttpHandler
{
    /// <summary>
    /// The HTTP handler contains common logic for issuing an HTTP request that is 
    /// independent of the underlying HTTP infrastructure.
    /// </summary>
    /// <typeparam name="TRequestContent"></typeparam>
    public class HttpHandler<TRequestContent> : PipelineHandler, IDisposable
    {
        private bool _disposed;
        private IHttpRequestFactory<TRequestContent> _requestFactory;

        /// <summary>
        /// The sender parameter used in any events raised by this handler.
        /// </summary>
        public object CallbackSender { get; private set; }

        /// <summary>
        /// The constructor for HttpHandler.
        /// </summary>
        /// <param name="requestFactory">The request factory used to create HTTP Requests.</param>
        /// <param name="callbackSender">The sender parameter used in any events raised by this handler.</param>
        public HttpHandler(IHttpRequestFactory<TRequestContent> requestFactory, object callbackSender)
        {
            _requestFactory = requestFactory;
            this.CallbackSender = callbackSender;
        }

        /// <summary>
        /// Issues an HTTP request for the current request context.
        /// </summary>
        /// <param name="executionContext">The execution context which contains both the
        /// requests and response context.</param>
        public override void InvokeSync(IExecutionContext executionContext)
        {
            IHttpRequest<TRequestContent> httpRequest = null;
            try
            {
                IRequest wrappedRequest = executionContext.RequestContext.Request;
                httpRequest = CreateWebRequest(executionContext.RequestContext);
                httpRequest.SetRequestHeaders(wrappedRequest.Headers);

                try
                {
                    // Send request body if present.
                    // if (wrappedRequest.HasRequestBody())
                    // {
                        var requestContent = httpRequest.GetRequestContent();
                        WriteContentToRequestBody(requestContent, httpRequest, executionContext.RequestContext);
                    // }

                    executionContext.ResponseContext.HttpResponse = httpRequest.GetResponse();
                }
                finally
                {
                }
            }
            finally
            {
                if (httpRequest != null)
                    httpRequest.Dispose();
            }
        }

        /// <summary>
        /// Issues an HTTP request for the current request context.
        /// </summary>
        /// <param name="executionContext">The execution context which contains both the
        /// requests and response context.</param>
        /// <returns>IAsyncResult which represent an async operation.</returns>
        public override IAsyncResult InvokeAsync(IAsyncExecutionContext executionContext)
        {
            IHttpRequest<TRequestContent> httpRequest = null;
            try
            {
                httpRequest = CreateWebRequest(executionContext.RequestContext);
                executionContext.RuntimeState = httpRequest;

                IRequest wrappedRequest = executionContext.RequestContext.Request;
                if (executionContext.RequestContext.Retries == 0)
                {
                    // First call, initialize an async result.
                    executionContext.ResponseContext.AsyncResult =
                        new RuntimeAsyncResult(executionContext.RequestContext.Callback, 
                            executionContext.RequestContext.State);                    
                }

                // Set request headers
                httpRequest.SetRequestHeaders(executionContext.RequestContext.Request.Headers);

                if (wrappedRequest.HasRequestBody())
                {
                    // Send request body if present.
                    httpRequest.BeginGetRequestContent(new AsyncCallback(GetRequestStreamCallback), executionContext);
                }
                else
                {
                    // Get response if there is no response body to send.
                    httpRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), executionContext);
                }
                return executionContext.ResponseContext.AsyncResult;
            }
            catch (Exception)
            {
                if (executionContext.ResponseContext.AsyncResult != null)
                {
                    // An exception will be thrown back to the calling code.
                    // Dispose AsyncResult as it will not be used further.
                    executionContext.ResponseContext.AsyncResult.Dispose();
                    executionContext.ResponseContext.AsyncResult = null;
                }

                if (httpRequest != null)
                {                    
                    httpRequest.Dispose();
                }

                throw;
            }
        }
        
        
                /// <summary>
        /// Issues an HTTP request for the current request context.
        /// </summary>
        /// <typeparam name="T">The response type for the current request.</typeparam>
        /// <param name="executionContext">The execution context, it contains the
        /// request and response context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async System.Threading.Tasks.Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            IHttpRequest<TRequestContent> httpRequest = null;
            try
            {
                IRequest wrappedRequest = executionContext.RequestContext.Request;
                httpRequest = CreateWebRequest(executionContext.RequestContext);
                httpRequest.SetRequestHeaders(wrappedRequest.Headers);
                
                {
                    // Send request body if present.
                    if (wrappedRequest.HasRequestBody())
                    {
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo edi = null;
                        try
                        {
                            var requestContent = await httpRequest.GetRequestContentAsync().ConfigureAwait(false);
                            WriteContentToRequestBody(requestContent, httpRequest, executionContext.RequestContext);
                        }
                        catch(Exception e)
                        {
                            edi = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                        }

                        if (edi != null)
                        {
                            await CompleteFailedRequest(executionContext, httpRequest).ConfigureAwait(false);

                            edi.Throw();
                        }
                    }
                
                    var response = await httpRequest.GetResponseAsync(executionContext.RequestContext.CancellationToken).
                        ConfigureAwait(false);
                    executionContext.ResponseContext.HttpResponse = response;
                }
                // The response is not unmarshalled yet.
                return null;
            }            
            finally
            {
                if (httpRequest != null)
                    httpRequest.Dispose();
            }
        }

        private static async System.Threading.Tasks.Task CompleteFailedRequest(
            IExecutionContext executionContext, IHttpRequest<TRequestContent> httpRequest)
        {
            // In some cases where writing the request body fails, HttpWebRequest.Abort
            // may not dispose of the underlying Socket, so we need to retrieve and dispose
            // the web response to close the socket
            IWebResponseData iwrd = null;
            try
            {
                iwrd = await httpRequest.GetResponseAsync(executionContext.RequestContext.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
                if (iwrd != null && iwrd.ResponseBody != null)
                    iwrd.ResponseBody.Dispose();
            }
        }
        

        private void GetRequestStreamCallback(IAsyncResult result)
        {
            IAsyncExecutionContext executionContext = null;
            IHttpRequest<TRequestContent> httpRequest = null;
            try
            {
                executionContext = result.AsyncState as IAsyncExecutionContext;
                httpRequest = executionContext.RuntimeState as IHttpRequest<TRequestContent>;

                var requestContent = httpRequest.EndGetRequestContent(result);
                WriteContentToRequestBody(requestContent, httpRequest, executionContext.RequestContext);
                //var requestStream = httpRequest.EndSetRequestBody(result);                
                httpRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), executionContext);
            }
            catch(Exception exception)
            {   
                httpRequest.Dispose();

                // Capture the exception and invoke outer handlers to 
                // process the exception.
                executionContext.ResponseContext.AsyncResult.Exception = exception;
                base.InvokeAsyncCallback(executionContext);
            }
        }

        private void GetResponseCallback(IAsyncResult result)
        {
            IAsyncExecutionContext executionContext = null;
            IHttpRequest<TRequestContent> httpRequest = null;
            try
            {
                executionContext = result.AsyncState as IAsyncExecutionContext;
                httpRequest = executionContext.RuntimeState as IHttpRequest<TRequestContent>;

                var httpResponse = httpRequest.EndGetResponse(result);
                executionContext.ResponseContext.HttpResponse = httpResponse;
            }
            catch (Exception exception)
            {   
                // Capture the exception and invoke outer handlers to 
                // process the exception.
                executionContext.ResponseContext.AsyncResult.Exception = exception;
            }
            finally
            {
                httpRequest.Dispose();
                base.InvokeAsyncCallback(executionContext);
            }
        }

        /// <summary>
        /// Determines the content for request body and uses the HTTP request
        /// to write the content to the HTTP request body.
        /// </summary>
        /// <param name="requestContent">Content to be written.</param>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <param name="requestContext">The request context.</param>
        private void WriteContentToRequestBody(TRequestContent requestContent,
            IHttpRequest<TRequestContent> httpRequest,
            IRequestContext requestContext)
        {
            IRequest wrappedRequest = requestContext.Request;

            if (wrappedRequest.ContentStream == null)
            {
                byte[] requestData = wrappedRequest.Content;
                httpRequest.WriteToRequestBody(requestContent, requestData, requestContext.Request.Headers);
            }
            else
            {
                var originalStream = wrappedRequest.ContentStream;
                httpRequest.WriteToRequestBody(requestContent, originalStream, 
                    requestContext.Request.Headers, requestContext);

            }
        }

        /// <summary>
        /// Creates the HttpWebRequest and configures the end point, content, user agent and proxy settings.
        /// </summary>
        /// <param name="requestContext">The async request.</param>
        /// <returns>The web request that actually makes the call.</returns>
        protected virtual IHttpRequest<TRequestContent> CreateWebRequest(IRequestContext requestContext)
        {
            IRequest request = requestContext.Request;
            Uri url = AliyunServiceClient.ComposeUrl(request);
            var httpRequest = _requestFactory.CreateHttpRequest(url);
            httpRequest.ConfigureRequest(requestContext);
            
            httpRequest.Method = request.HttpMethod;
            if (request.MayContainRequestBody())
            {
                if (request.Content == null && (request.ContentStream == null))
                {
                    string queryString = AliyunSDKUtils.GetParametersAsString(request.Parameters);
                    request.Content = Encoding.UTF8.GetBytes(queryString);
                }
                
                if (request.Content!=null)
                {
                    request.Headers[Constants.ContentLengthHeader] = 
                        request.Content.Length.ToString(CultureInfo.InvariantCulture);
                }
                else if (request.ContentStream != null && !request.Headers.ContainsKey(Constants.ContentLengthHeader))
                {
                    request.Headers[Constants.ContentLengthHeader] =
                        request.ContentStream.Length.ToString(CultureInfo.InvariantCulture);
                }
            }
            else if (request.UseQueryString &&
                (request.HttpMethod == "POST" ||
                 request.HttpMethod == "PUT" ||
                 request.HttpMethod == "DELETE"))
            {
                request.Content = new Byte[0];
            }
            
            return httpRequest;
        }

        /// <summary>
        /// Disposes the HTTP handler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_requestFactory != null)
                    _requestFactory.Dispose();

                _disposed = true;
            }
        }
    }
}
