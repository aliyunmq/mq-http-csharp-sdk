using Aliyun.MQ.Runtime.Internal;
using Aliyun.MQ.Runtime.Internal.Transform;
using Aliyun.MQ.Runtime.Internal.Util;

namespace Aliyun.MQ.Runtime.Pipeline.Handlers
{
    public class Unmarshaller : PipelineHandler
    {
        public override void InvokeSync(IExecutionContext executionContext)
        {
            base.InvokeSync(executionContext);

            if (executionContext.ResponseContext.HttpResponse.IsSuccessStatusCode)
            {
                // Unmarshall the http response.
                Unmarshall(executionContext);  
            }                      
        }
        
        /// <summary>
        /// Unmarshalls the response returned by the HttpHandler.
        /// </summary>
        /// <typeparam name="T">The response type for the current request.</typeparam>
        /// <param name="executionContext">The execution context, it contains the
        /// request and response context.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async System.Threading.Tasks.Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
            // Unmarshall the response
            await UnmarshallAsync(executionContext).ConfigureAwait(false);
            return (T)executionContext.ResponseContext.Response;
        }
        
        /// <summary>
        /// Unmarshalls the HTTP response.
        /// </summary>
        /// <param name="executionContext">
        /// The execution context, it contains the request and response context.
        /// </param>
        private async System.Threading.Tasks.Task UnmarshallAsync(IExecutionContext executionContext)
        {
            var requestContext = executionContext.RequestContext;
            var responseContext = executionContext.ResponseContext;

            {
                var unmarshaller = requestContext.Unmarshaller;
                try
                {
                    var responseStream = await responseContext.HttpResponse.
                        ResponseBody.OpenResponseAsync().ConfigureAwait(false);
                    var context = unmarshaller.CreateContext(responseContext.HttpResponse, responseStream);

                    var response = UnmarshallResponse(context, requestContext);
                    responseContext.Response = response;
                }
                finally
                {
                    if (!unmarshaller.HasStreamingProperty)
                        responseContext.HttpResponse.ResponseBody.Dispose();
                }
            }
        }

        protected override void InvokeAsyncCallback(IAsyncExecutionContext executionContext)
        {
            // Unmarshall the response if an exception hasn't occured
            if (executionContext.ResponseContext.AsyncResult.Exception == null)
            {
                Unmarshall(ExecutionContext.CreateFromAsyncContext(executionContext));
            }            
            base.InvokeAsyncCallback(executionContext);
        }

        private void Unmarshall(IExecutionContext executionContext)
        {
            var requestContext = executionContext.RequestContext;
            var responseContext = executionContext.ResponseContext;

            try
            {
                var unmarshaller = requestContext.Unmarshaller;
                try
                {
                    var context = unmarshaller.CreateContext(responseContext.HttpResponse,
                            responseContext.HttpResponse.ResponseBody.OpenResponse());

                    var response = UnmarshallResponse(context, requestContext);
                    responseContext.Response = response;                    
                }
                finally
                {
                    if (!unmarshaller.HasStreamingProperty)
                        responseContext.HttpResponse.ResponseBody.Dispose();
                }
            }
            finally
            {
            }
        }

        private WebServiceResponse UnmarshallResponse(UnmarshallerContext context,
            IRequestContext requestContext)
        {
            var unmarshaller = requestContext.Unmarshaller;
            WebServiceResponse response = null;
            try
            {
                response = unmarshaller.UnmarshallResponse(context);
            }
            finally
            {
            }

            return response;
        }
    }
}
