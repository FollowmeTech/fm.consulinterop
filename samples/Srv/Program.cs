using FM.ConsulInterop.Config;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using FM.ConsulInterop;

namespace Srv
{
    class Program
    {
        static void Main(string[] args)
        {
            InnerLogger.ConsulLog += (c) => Console.WriteLine(c.Content);

            #region get srvConfig
            var conf = new ConfigurationBuilder()
  .SetBasePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory))
  .AddJsonFile("appsetting.json", false, false)
  .Build();

            var srvConfig = conf.
                GetSection("consul:service").Get<ConsulLocalServiceConfig>();

            #endregion
            new Server
            {
                Services = { FM.Demo.HelloSrv.BindService(new HelloSrvImp()) },
            }.StartAndRegisterService(srvConfig).Wait();


            Console.WriteLine("---startup----");
            //end 
            ShutdownProcessor.Process();
        }
    }
}
