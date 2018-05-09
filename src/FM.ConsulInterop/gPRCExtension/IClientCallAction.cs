using Grpc.Core;

namespace FM.ConsulInterop
{
    /// <summary>
    /// grpc client call aop
    /// </summary>
    public interface IClientCallAction
    {
        CallOptions PreAction<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request);
        void PostAction<TResponse>(TResponse response);
    }                                                        
}
