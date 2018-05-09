using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FM.ConsulInterop.Config;
using Grpc.Core;

namespace FM.ConsulInterop
{
    public static class GrpcServiceRegisterExtension
    {
        static Dictionary<int, ServiceRegister> serviceDict = new Dictionary<int, ServiceRegister>();

        /// <summary>
        /// Starts the and register service.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serviceConfigConnectionString">The service configuration connection string.</param>
        /// <returns></returns>
        public static Task<Server> StartAndRegisterService(this Grpc.Core.Server server,
            String serviceConfigConnectionString)
        {
            return StartAndRegisterService(server,
                serviceConfigConnectionString.ParseConnectionString<ConsulLocalServiceConfig>());
        }

        /// <summary>
        /// Starts the and register service.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serviceConfig">The service configuration.</param>
        /// <returns></returns>
        public static async Task<Server> StartAndRegisterService(this Grpc.Core.Server server,
            ConsulLocalServiceConfig serviceConfig)
        {
            /*
             * 需要解析ServiceAddress
             * 支持的几种类型
             * 1. 全指定 ip:port
             * 2. 指定Ip:0 (由grpc自动选择port)
             * 3. 0.0.0.0:9090 (这是自动选择当前host ip)
             * 
             * 支持环境变量设置serviceaddress, consuladdresss
             */
            if (!string.IsNullOrWhiteSpace(EnviromentParameters.ServiceAddress))
            {
                InnerLogger.Log(LoggerLevel.Info, $"使用环境变量中配置的serviceaddress:{EnviromentParameters.ServiceAddress}");
                serviceConfig.SetServiceAddress(EnviromentParameters.ServiceAddress);
            }

            if (!string.IsNullOrWhiteSpace(EnviromentParameters.ConsulAddress))
            {
                InnerLogger.Log(LoggerLevel.Info, $"使用环境变量中的consuladdress:{EnviromentParameters.ConsulAddress}");
                serviceConfig.SetConsulAddress(EnviromentParameters.ConsulAddress);
            }

            //解析ip
            var ipPortPair = serviceConfig.ServiceAddress.Split(':');

            ipPortPair[0] = NetHelper.GetIp(ipPortPair[0]);
            InnerLogger.Log(LoggerLevel.Info, "选择IP:" + ipPortPair[0]);
            
            server.Ports.Add(new ServerPort(ipPortPair[0], int.Parse(ipPortPair[1]),
                ServerCredentials.Insecure));

            server.Start();
            InnerLogger.Log(LoggerLevel.Info, "grpc服务启动");

            //处理端口
            if (ipPortPair[1] == "0") //PickUnused
            {
                ipPortPair[1] = server.Ports.First().BoundPort.ToString();
                InnerLogger.Log(LoggerLevel.Info, "自动选择port:" + ipPortPair[1]);
            }

            //重新设置ServiceAddress
            serviceConfig.ServiceAddress = $"{ipPortPair[0]}:{ipPortPair[1]}";

            var serviceRegisterProxy = new ServiceRegister(serviceConfig);
            await serviceRegisterProxy.Register();

            serviceDict[server.GetHashCode()] = serviceRegisterProxy;

            InnerLogger.Log(LoggerLevel.Info, "注册服务发现");
            return server;
        }

        /// <summary>
        /// Stops the and deregister.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="stopServer">The stop server.</param>
        /// <returns></returns>
        /// <exception cref="Exception">当前服务没有注册,或者是已经被反注册过..</exception>
        public static async Task<Server> StopAndDeregister(this Grpc.Core.Server server,
            Action<Server> stopServer = null)
        {
            if (!serviceDict.ContainsKey(server.GetHashCode()))
            {
                throw new Exception("当前服务没有注册,或者是已经被反注册过..");
            }

            await serviceDict[server.GetHashCode()].Deregister();

            serviceDict.Remove(server.GetHashCode());
            InnerLogger.Log(LoggerLevel.Info, "反注册服务发现");

            if (stopServer == null)
            {
                await server.KillAsync();
            }
            else
            {
                stopServer(server);
            }

            InnerLogger.Log(LoggerLevel.Info, "grpc服务停止");
            return server;
        }
    }
}
