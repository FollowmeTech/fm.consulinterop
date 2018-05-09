using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using FM.ConsulInterop;
using FM.ConsulInterop.Config;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            InnerLogger.ConsulLog += c => Console.WriteLine(c.Content);

            //#region get srvConfig
            var conf = new ConfigurationBuilder()
  .SetBasePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory))
  .AddJsonFile("appsetting.json", false, false)
  .Build();

            var clientConfig = conf.
                GetSection("consul:remotes:demo").
                Get<ConsulRemoteServiceConfig>();

            //#endregio

            var clientWithClientMiddleware =
                new ClientAgent<FM.Demo.HelloSrv.HelloSrvClient>(clientConfig,
                new ClientAgentOption
                {
                    ClientCallActionCollection = new ClientCallActionCollection { new InvokeTimeoutMiddleware(10000), new LoggerClientCallAction() }
                });
            try
            {
                clientWithClientMiddleware.Proxy.Hi(new FM.Demo.HiRequest());
                //client.Proxy.Hi(new FM.Demo.HiRequest(), null, DateTime.UtcNow.AddMilliseconds(3000));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            var rawClient =
            new ClientAgent<FM.Demo.HelloSrv.HelloSrvClient>(clientConfig);
            try
            {
                rawClient.Proxy.Hi(new FM.Demo.HiRequest());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //end 
            Console.ReadLine();
        }
    }
}
