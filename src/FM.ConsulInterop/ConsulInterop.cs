using Consul;
using System;

namespace FM.ConsulInterop
{
    /// <summary>
    /// consul interop
    /// </summary>
    public abstract class ConsulInterop
    {
        /// <summary>
        /// consul client
        /// </summary>
        protected ConsulClient ConsulClient { get; private set; }

        /// <summary>
        /// 初始化consul客戶端
        /// </summary>
        /// <param name="consulIntegration"></param>
        /// <param name="consulAddress"></param>
        protected void InitConsulClient(string consulAddress)
        {
            this.ConsulClient = CreateConsulClient(consulAddress);
        }

        /// <summary>
        /// Creates the consul client.
        /// </summary>
        /// <param name="consulAddress">The consul address.</param>
        /// <returns></returns>
        protected static ConsulClient CreateConsulClient(string consulAddress)
        {
            return new ConsulClient(p =>
            {
                p.Address = new Uri(consulAddress);
            });
        }
    }
}
