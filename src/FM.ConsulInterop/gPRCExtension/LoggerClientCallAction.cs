using Grpc.Core;

namespace FM.ConsulInterop
{
    /// <summary>
    /// logger client call api
    /// </summary>
    /// <seealso cref="FM.ConsulInterop.IClientCallAction" />
    public class LoggerClientCallAction : IClientCallAction
    {
        public void PostAction<TResponse>(TResponse response)
        {
            InnerLogger.Log(LoggerLevel.Info, response?.ToString());
        }

        public CallOptions PreAction<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options,
            TRequest request)
        {
            InnerLogger.Log(LoggerLevel.Info, $"{method.FullName}  {request.ToString()}");
            return options;
        }
    }
}