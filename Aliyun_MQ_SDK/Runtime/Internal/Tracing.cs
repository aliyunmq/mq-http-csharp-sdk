using Aliyun.MQ.Runtime.Pipeline;
using System.Diagnostics;

namespace Aliyun_MQ_SDK.Runtime.Internal
{
    internal static class Tracing
    {
        public const string DiagnosticSourceName = "aliyun.rocketmq.http";

        internal static readonly DiagnosticListener DiagnosticListener = new DiagnosticListener(DiagnosticSourceName);

        internal static void BeforeInvoke(IExecutionContext context) => Write(context.RequestContext.RequestName + "BeforeInvokeSync", context);
        internal static void AfterInvoke(IExecutionContext context) => Write(context.RequestContext.RequestName + "AfterInvokeSync", context);

        internal static void Write(string name,object payload)
        {
            if (DiagnosticListener.IsEnabled(name))
            {
                DiagnosticListener.Write(name, payload);
            }
        }
    }
}
