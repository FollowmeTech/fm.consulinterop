using System;
using FM.ConsulInterop.Config;

namespace FM.ConsulInterop
{
    /// <summary>
    /// consul client agent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="FM.ConsulInterop.ConsulInterop" />
    public class ClientAgent<T> where T :class
    {
        /// <summary>
        /// The channel pool manager
        /// </summary>
        public GRPCChannelPoolManager ChannelPoolManager { get; private set; }
        private ClientAgentOption _clientAgentOption;

        private ClientAgent(ClientAgentOption agentOption)
        {
            this._clientAgentOption = agentOption;
        }

        /// <summary>
        /// create Client Agent for GrpcClient
        /// </summary>
        /// <param name="connectionString">serviceName=followme.srv.xxx;freshInterval=100;consulAddress=ip:port;cosul..</param>
        /// <param name="option"></param>
        public ClientAgent(string connectionString, ClientAgentOption option=null) :
            this(connectionString.ParseConnectionString<ConsulRemoteServiceConfig>(), option)
        {

        }

        public ClientAgent(ConsulRemoteServiceConfig config, ClientAgentOption option = null) : this(
            option ?? new ClientAgentOption())
        {
            ChannelPoolManager = new GRPCChannelPoolManager(config, option);
        }


        /// <summary>
        /// Get Client Proxy
        /// </summary>
        public T Proxy
        {
            get
            {
                var availableServiceChannelPair = this.ChannelPoolManager.FetchOneAgentServiceChannelPair;
                if (availableServiceChannelPair.Proxy == null)
                {
                    var proxy = (T)Activator.CreateInstance(
                        typeof(T),
                        new ClientCallInvoker(availableServiceChannelPair.Channel, this._clientAgentOption?.ClientCallActionCollection));

                    availableServiceChannelPair.Proxy = proxy;
                }

                return availableServiceChannelPair.Proxy as T;
            }
        }

        /// <summary>
        /// clean
        /// </summary>
        public void Clean()
        {
            this.ChannelPoolManager.Clean();
        }
    }
}
