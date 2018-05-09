using Consul;
using FM.ConsulInterop.Config;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FM.ConsulInterop
{
    /// <summary>
    /// 服务注册
    /// </summary>
    public class ServiceRegister : ConsulInterop
    {
        /// <summary>
        /// service config
        /// </summary>
        public ConsulLocalServiceConfig ServiceConfig;

        /// <summary>
        /// The consul service TTL register timer
        /// </summary>
        static System.Timers.Timer consulServiceTTLRegisterTimer;


        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceRegister"/>
        /// </summary>
        public ServiceRegister(ConsulLocalServiceConfig config)
        {
            ServiceConfig = config;
        }

        public ServiceRegister(string connectionString) : this(connectionString
            .ParseConnectionString<ConsulLocalServiceConfig>())
        {

        }


        /// <summary>
        /// Registers the specified agent service registration.
        /// </summary>
        /// <param name="agentServiceRegistration">The agent service registration.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        private static async Task Register(AgentServiceRegistration agentServiceRegistration,
            ConsulLocalServiceConfig config)
        {
            if (!config.ConsulIntegration)
            {
                InnerLogger.Log(LoggerLevel.Info, "ConsulIntegration=false，当前服务不集成consul环境");
                return;
            }

            InnerLogger.Log(LoggerLevel.Info,
                "register:" + Newtonsoft.Json.JsonConvert.SerializeObject(agentServiceRegistration));

            try
            {
                var client = CreateConsulClient(config.ConsulAddress);
                var rs = await client.Agent.ServiceRegister(agentServiceRegistration);

                client.Dispose();
                InnerLogger.Log(LoggerLevel.Info, Newtonsoft.Json.JsonConvert.SerializeObject(rs));
            }
            catch (Exception ex)
            {
                InnerLogger.Log(LoggerLevel.Error, $"consul Register failed {Environment.NewLine}{ex.ToString()}");
            }
        }

        public async Task Register()
        {
            if (!ServiceConfig.ConsulIntegration)
            {
                InnerLogger.Log(LoggerLevel.Info, "ConsulIntegration=false，当前服务不集成consul环境");
                return;
            }

            var registerTimerLLtTime = (ServiceConfig.TCPInterval / 2) * 1000;
            if (registerTimerLLtTime <= 0)
            {
                throw new ArgumentException("TCPInterval配置错误");
            }

            InnerLogger.Log(LoggerLevel.Info, "register: use file config");

            await RegisterService();
            await RegisterTTLCheck();

            /*
             * timer本身的精度就不够
             * 不能再timer里面做更多的耗时的操作
             */
            consulServiceTTLRegisterTimer = new System.Timers.Timer
            {
                AutoReset = false,
                Interval = registerTimerLLtTime,
                Enabled = false
            };

            consulServiceTTLRegisterTimer.Elapsed += async (s, r) =>
            {
                try
                {
                    var stopWatch = Stopwatch.StartNew();

                    var client = CreateConsulClient(ServiceConfig.ConsulAddress);
                    await client.Agent.PassTTL(ServiceConfig.GetConsulServiceId() + ":ttlcheck",
                        "timer:" + DateTime.Now);

                    client.Dispose();
                    InnerLogger.Log(LoggerLevel.Debug, $"passing TTL,耗时:{stopWatch.ElapsedMilliseconds}");
                }
                catch (Exception ex)
                {
                    var content = ex.ToString();
                    InnerLogger.Log(LoggerLevel.Error, content);

                    /*
                     * passTTL会出现如下几种情况：
                     * 1. consul服务重启中，ex会显示 connection refused by ip:port
                     *          这种情况下，不去处理，等consul服务重启之后就好了
                     * 2. consul服务重启之后，会丢失之前的service，check，会有如下的错误：
                     *          Unexpected response, status code InternalServerError: CheckID "followme.srv.sms-192.168.3.10-10086-07f21040-0be9-4a73-b0a1-71755c6d6d46:ttlcheck" does not have associated TTL
                     *          在这种情况下，需要处理，重新注册服务，check；     
                     */
                    if (content
                        .Contains(
                            $"CheckID \"{ServiceConfig.GetConsulServiceId() + ":ttlcheck"}\" does not have associated TTL")
                    )
                    {
                        InnerLogger.Log(LoggerLevel.Error, "consul PASSTTL failed:需要重新注册");
                        await RegisterService();
                        await RegisterTTLCheck();
                    }
                }
                finally
                {
                    consulServiceTTLRegisterTimer.Enabled = true;
                }
            };

            //start timer
            consulServiceTTLRegisterTimer.Enabled = true;
        }

        private async Task RegisterTTLCheck()
        {
            var client = CreateConsulClient(ServiceConfig.ConsulAddress);
            var registerTtlCheckResult = await client.Agent.CheckRegister(new AgentCheckRegistration
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1),
                TTL = TimeSpan.FromSeconds(ServiceConfig.TCPInterval),
                Status = HealthStatus.Passing,
                ID = ServiceConfig.GetConsulServiceId() + ":ttlcheck",
                ServiceID = ServiceConfig.GetConsulServiceId(),
                Name = "ttlcheck"
            });

            InnerLogger.Log(LoggerLevel.Info,
                "RegisterTTLCheck:" + Newtonsoft.Json.JsonConvert.SerializeObject((registerTtlCheckResult)));

            client.Dispose();
        }

        private Task RegisterService()
        {
            var agentServiceRegistration = new AgentServiceRegistration
            {
                Address = ServiceConfig.IP,
                Port = ServiceConfig.Port,
                Name = ServiceConfig.ServiceName,
                ID = ServiceConfig.GetConsulServiceId(),
                Tags = ServiceConfig.ConsulTags.Split(' '),
                EnableTagOverride = true
            };

            return Register(agentServiceRegistration, ServiceConfig);
        }

        /// <summary>
        /// 取消consul服务
        /// </summary>
        /// <param name="serviceId">注册consul服务的serviceID</param>
        /// <returns></returns>
        public async Task Deregister(string serviceId)
        {
            if (!ServiceConfig.ConsulIntegration)
            {
                return;
            }

            InnerLogger.Log(LoggerLevel.Info, "deregister timer");
            consulServiceTTLRegisterTimer.Dispose();

            InnerLogger.Log(LoggerLevel.Info, "Deregister:" + serviceId);

            var rs = await CreateConsulClient(ServiceConfig.ConsulAddress).Agent.ServiceDeregister(serviceId);
            InnerLogger.Log(LoggerLevel.Info, Newtonsoft.Json.JsonConvert.SerializeObject(rs));
        }

        /// <summary>
        /// 取消consul服务
        /// </summary>
        /// <returns></returns>
        public Task Deregister()
        {
            return Deregister(ServiceConfig.GetConsulServiceId());
        }
    }
}
