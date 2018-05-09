using System;

namespace FM.ConsulInterop.Config
{
    /// <summary>
    /// service config
    /// </summary>
    public class ConsulLocalServiceConfig
    {
        public string ServiceName
        {
            get; set;
        }

        public string ConsulAddress
        {
            get; set;
        }

        public string ServiceAddress
        {
            get; set;
        }

        public string IP
        {
            get { return ServiceAddress.Split(':')[0]; }
        }
        public int Port
        {
            get { return int.Parse(ServiceAddress.Split(':')[1]); }
        }

        /// <summary>
        /// The consul service identifier, 一个consul服务实例在一个生命周期之中使用一个consulserviceid
        /// </summary>
        private string _consulServiceId = string.Empty;

        //移除了ConsulServiceId自定义
        public string GetConsulServiceId()
        {
            if (!string.IsNullOrWhiteSpace(_consulServiceId))
            {
                return _consulServiceId;
            }

            _consulServiceId = $"{this.ServiceName}-{this.ServiceAddress.Replace(":", "-")}-" + Guid.NewGuid();
            return _consulServiceId;
        }

        /// <summary>
        /// Sets the host ip.
        /// </summary>
        /// <param name="serviceAddress">The ip.</param>
        public void SetServiceAddress(string serviceAddress)
        {
            this.ServiceAddress = serviceAddress;
        }

        /// <summary>
        /// Sets the consul address.
        /// </summary>
        /// <param name="consulAddress">The consul address.</param>
        public void SetConsulAddress(string consulAddress)
        {
            this.ConsulAddress = consulAddress;
        }

        public bool ConsulIntegration
        {
            get; set;
        }


        public string ConsulTags
        {
            get; set;
        }

        public int TCPInterval
        {
            get; set;
        }

        /// <summary>
        /// Validations this instance.
        /// </summary>
        /// <exception cref="Exception">consultags IsNullOrWhiteSpace</exception>
        /// <exception cref="FormatException">consultags格式必须是v-*格式(为了和go-micro框架通讯)</exception>
        public ConsulLocalServiceConfig Validation()
        {
            if (string.IsNullOrWhiteSpace(this.ConsulTags))
                throw new Exception("consultags IsNullOrWhiteSpace");

            if (!System.Text.RegularExpressions.Regex.IsMatch(this.ConsulTags, "v-.*"))
                throw new FormatException("consultags格式必须是v-*格式(为了和go-micro框架通讯)");

            return this;
        }
    }
}