using Grpc.Core;

namespace FM.ConsulInterop
{
    /// <summary>
    /// invoke timeout middleware
    /// </summary>
    /// <seealso cref="FM.ConsulInterop.IClientCallAction" />
    public class InvokeTimeoutMiddleware : IClientCallAction
    {
        const string TIMEOUT_KEY = "grpc-timeout";
        public int TimoutMilliseconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeTimeoutMiddleware"/> class.
        /// </summary>
        /// <param name="timeout">timeout毫秒</param>
        public InvokeTimeoutMiddleware(int timeout)
        {
            this.TimoutMilliseconds  = timeout;
        }

        public void PostAction<TResponse>(TResponse response)
        {                                              
            //bypass
        }

        public CallOptions PreAction<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            if (options.Headers == null)
                options = options.WithHeaders(new Metadata());

            if (!options.Deadline.HasValue)
            {
                options.Headers.Add(TIMEOUT_KEY, $"{this.TimoutMilliseconds}m");
            }
            return options;
        }
    }
}
