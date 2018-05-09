using Consul;
using FM.ConsulInterop.Config;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FM.ConsulInterop
{
    /// <summary>
    /// grpc ChannelPool Manager
    /// </summary>
    /// <seealso cref="FM.ConsulInterop.ConsulInterop" />
    public class GRPCChannelPoolManager : ConsulInterop
    {
        private ClientAgentOption ClientAgentOption { get; set; }
        
        /// <summary>
        /// timer for fresh service  
        /// </summary>
        Timer _freshServiceListTimer = null;
        /// <summary>
        /// fresh ServiceList Interval
        /// </summary>
        int _freshServiceListInterval = Timeout.Infinite;
        
        /// <summary>
        /// round point
        /// </summary>
        private int _roundProxyIndex = 0;

        /// <summary>
        /// config
        /// </summary>
        private Config.ConsulRemoteServiceConfig clientConfig = null;
        
        public GRPCChannelPoolManager(ConsulRemoteServiceConfig config, ClientAgentOption option)
        {
            this.clientConfig = config;
            this.ClientAgentOption = option;

            InitGrpcChannel();
        }

        /// <summary>
        /// init grpc channel
        /// </summary>
        private void InitGrpcChannel()
        {
            if (this.clientConfig.ConsulIntegration)
            {
                base.InitConsulClient(clientConfig.ConsulAddress);

                InitUpdateServiceListTimer(this.clientConfig.FreshInterval);
                InnerLogger.Log(LoggerLevel.Debug, $"InitUpdateServiceListTimer: {this.clientConfig.FreshInterval}ms");
            }
            else
            {
                InnerLogger.Log(LoggerLevel.Debug, "direct connect:" + this.clientConfig.ServiceAddress);
                var addressList = this.clientConfig.ServiceAddress.Split(',');
                foreach (var address in addressList)
                {
                    if (string.IsNullOrWhiteSpace(address)) continue;

                    var hostIp = address.Split(':');
                    AddGrpcChannel(hostIp[0], int.Parse(hostIp[1]), new AgentService { ID = $"direct:{address}" });
                }
            }
        }

        public struct AgentServiceChannelPair
        {
            /// <summary>
            /// Gets or sets the agent service.
            /// </summary>
            /// <value>
            /// The agent service.
            /// </value>
            public AgentService AgentService { get; set; }
            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            /// <value>
            /// The channel.
            /// </value>
            public Channel Channel { get; set; }
            /// <summary>
            /// Gets or sets the proxy.
            /// </summary>
            /// <value>
            /// The proxy.
            /// </value>
            public Object Proxy { get; set; }
        }

        /// <summary>
        /// Gets the connected agent service channels.
        /// </summary>
        /// <value>
        /// The connected agent service channels.
        /// </value>
        public List<AgentServiceChannelPair> ConnectedAgentServiceChannels { get; private set; } = new List<AgentServiceChannelPair>();

        /// <summary>
        /// Initializes the update service list timer.
        /// </summary>
        /// <param name="consulSerivceName">Name of the consul serivce.</param>
        /// <param name="freshServiceListInterval">The fresh service list interval.</param>
        private void InitUpdateServiceListTimer(int freshServiceListInterval)
        {
            this._freshServiceListInterval = freshServiceListInterval;

            _freshServiceListTimer = new Timer(async obj =>
            {
                await this.DownLoadServiceListAsync();
                InnerLogger.Log(LoggerLevel.Debug, $"{this._freshServiceListInterval}后，继续timer");
            });

            _freshServiceListTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        private async Task DownLoadServiceListAsync()
        {
            try
            {
                this._freshServiceListTimer.Change(Timeout.Infinite, Timeout.Infinite);
                InnerLogger.Log(LoggerLevel.Debug, $"start DownLoadServiceList.");

                //当前正在使用的servicelist
                var currentUsageServiceChannels =
                    this.ConnectedAgentServiceChannels.ConvertAll<AgentService>(p => p.AgentService);

                var newestService = new List<AgentService>();
                var passOnlyService = await this.ConsulClient.Health.Service(this.clientConfig.ServiceName, "", true);

                passOnlyService.Response.ToList().ForEach(p =>
                {
                    newestService.Add(p.Service);
                });

                if (newestService.Count == 0)
                {
                    InnerLogger.Log(LoggerLevel.Info, $"找不到对应的consul服务  {this.clientConfig.ServiceName}");
                    return;
                }

                //检查consul服务是否有变化；
                var newServices = newestService.Except(currentUsageServiceChannels, new AgentServerComparer());
                var abandonServices = currentUsageServiceChannels.Except(newestService, new AgentServerComparer());
                if (newServices.Count() == 0 && abandonServices.Count() == 0)
                {
                    InnerLogger.Log(LoggerLevel.Debug, $"consul服务没有更新..");
                    return;
                }

                //update consul服务
                //移除已经失效的channel
                abandonServices.ToList().ForEach(p =>
                {
                    var abandonPair = this.ConnectedAgentServiceChannels.First(pair => pair.AgentService == p);
                    this.ConnectedAgentServiceChannels.Remove(abandonPair);
                    abandonPair.Channel.ShutdownAsync();

                    InnerLogger.Log(LoggerLevel.Info,
                        $"移除失效的service: {abandonPair.AgentService.Service}:{abandonPair.AgentService.ID}  {abandonPair.AgentService.Address}:{abandonPair.AgentService.Port}, ");
                });

                //添加新的channel
                newServices.ToList().ForEach(p => AddGrpcChannel(p.Address, p.Port, p));
                Interlocked.Exchange(ref _roundProxyIndex, 0);
                InnerLogger.Log(LoggerLevel.Debug, "roundProxyIndex 变更为0");
            }
            catch (Exception ex)
            {
                InnerLogger.Log(LoggerLevel.Error, ex.ToString());
            }
            finally
            {
                this._freshServiceListTimer.Change(this._freshServiceListInterval, Timeout.Infinite);
                InnerLogger.Log(LoggerLevel.Debug,
                    $"register time, {this._freshServiceListInterval} 后继续downloadServiceList");
            }
        }

        private void AddGrpcChannel(string address, int port, AgentService agentService)
        {
            var newChannle = new Channel(address, port, ChannelCredentials.Insecure,
                ClientAgentOption?.ChannelOptions ?? new List<ChannelOption>());

            var newPair = new AgentServiceChannelPair
            {
                AgentService = agentService,
                Channel = newChannle
            };

            ConnectedAgentServiceChannels.Add(newPair);
            InnerLogger.Log(LoggerLevel.Info,
                $"添加新的service: {newPair.AgentService?.Service}:{newPair.AgentService?.ID}  {newPair.AgentService?.Address}:{newPair.AgentService?.Port}, ");
        }

        /// <summary>
        /// fetch one channel
        /// </summary>
        /// <exception cref="Exception"></exception>
        public Channel FetchOneChannel => FetchOneAgentServiceChannelPair.Channel;

        
        private readonly object _fetchLock=new object();

        /// <summary>
        /// Gets the fetch one agent service channel pair.
        /// </summary>
        /// <value>
        /// The fetch one agent service channel pair.
        /// </value>
        /// <exception cref="Exception"></exception>
        public AgentServiceChannelPair FetchOneAgentServiceChannelPair
        {
            get
            {
                var entryed = false;
                try
                {

                    /*
                     * 移除lock的原因在于lock没有timeout机制
                     * 如果突然大量的请求来了之后,
                     * 会全部卡在这个方法之中就会导致大量的阻塞(并且阻塞之后没有任何意义)
                     * 出现阻塞的地方只有可能是正在更新,正在更新的过程之中,所有的调用都应该全部失败..
                     */
                    entryed = Monitor.TryEnter(_fetchLock, 100);
                    if (!entryed)
                    {
                        //timeout
                        throw new Exception("Fetch timeout, 服务暂不可用");
                    }

                    fetch:

                    // 当connectedAgentServiceChannles为空的时候,需要clean list
                    // download service list
                    if (this.ConnectedAgentServiceChannels.Count == 0)
                    {
                        DownLoadServiceListAsync().Wait(); //sync
                    }

                    if (this.ConnectedAgentServiceChannels.Count == 0)
                    {
                        throw new Exception($"[no-available-grpc-service->{this.clientConfig}]");
                    }

                    //循环调度
                    if (_roundProxyIndex == this.ConnectedAgentServiceChannels.Count)
                    {
                        Interlocked.Exchange(ref _roundProxyIndex, 0);
                    }

                    var choosePair = this.ConnectedAgentServiceChannels[_roundProxyIndex];

                    //当channel相关的service shutdown之后,该状态一直会处于connecting的状态
                    //如果此时采用的是random port,问题比较严重
                    //所以,一旦服务检测到挂了之后,就直接清除该connection
                    if (choosePair.Channel.State == ChannelState.Shutdown ||
                        choosePair.Channel.State == ChannelState.TransientFailure ||
                        choosePair.Channel.State == ChannelState.Connecting)
                    {
                        this.ConnectedAgentServiceChannels.Remove(choosePair);
                        InnerLogger.Log(LoggerLevel.Error,
                            $"当前Channel异常,状态：{choosePair.Channel.State}  ServiceId:{choosePair.AgentService.ID} ,已经被移除");

                        Interlocked.Exchange(ref _roundProxyIndex, 0);
                        goto fetch;
                    }

                    Interlocked.Increment(ref _roundProxyIndex);

                    InnerLogger.Log(LoggerLevel.Debug, $"使用proxy:{choosePair.AgentService.ID} ");
                    return choosePair;
                }
                finally
                {
                    if (entryed) Monitor.Exit(_fetchLock);
                }
            }
        }

        /// <summary>
        /// clean grpc channel resources
        /// </summary>
        public void Clean()
        {
            this._freshServiceListTimer?.Dispose();
            this.ConnectedAgentServiceChannels?.ForEach(pair =>
            {
                pair.Channel.ShutdownAsync().Wait();
            });
            this.ConsulClient?.Dispose();

            InnerLogger.Log(LoggerLevel.Info, $"clean");
        }
    }
}
