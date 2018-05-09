using System;

namespace FM.ConsulInterop
{
    internal static class EnviromentParameters
    {
        /// <summary>
        /// 用于docker环境中,传递的环境变量SERVICE_ADDRESS
        /// </summary>
        public static string ServiceAddress { get; } = Environment.GetEnvironmentVariable("SERVICE_ADDRESS");

        /// <summary>
        /// 用于dockers环境中,环境CONSUL_ADDRESS
        /// </summary>
        public static string ConsulAddress { get; } = Environment.GetEnvironmentVariable("CONSUL_ADDRESS");
    }
}
