using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FM.ConsulInterop.Config
{
    public class ConsulRemoteServiceConfig
    {
        public string Name { get; set; }

        public string ServiceName { get; set; }


        public int FreshInterval { get; set; }

        public string ConsulAddress { get; set; }

        public bool ConsulIntegration { get; set; }

        public string ServiceAddress { get; set; }

        public override string ToString()
        {
            return $@"name:{this.Name},
serviceName:{this.ServiceName},
FreshInterval:{this.FreshInterval},
ConsulAddress:{this.ConsulAddress},
ServiceAddress:{this.ServiceAddress}";
        }


        /// <summary>
        /// parse connectionstring as ConsulRemoteServiceConfig
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConsulRemoteServiceConfig Parse(string connectionString)
        {
            try
            {
                var keyValueDict = new Dictionary<string, object>();
                foreach (var kv in connectionString.Split(';'))
                {
                    var keyValue = kv.Split('=');
                    keyValueDict[keyValue[0]] = keyValue[1];
                }

                var config = new ConsulRemoteServiceConfig();
                foreach (var p in config.GetType().GetProperties())
                {
                    if (p.CanRead && p.CanWrite)
                    {
                        if (keyValueDict.ContainsKey(p.Name))
                        {
                            p.SetValue(config, Convert.ChangeType(keyValueDict[p.Name], p.PropertyType));
                        }
                    }
                }

                return config;
            }
            catch (Exception e)
            {
                throw new ArgumentException("解析connectionstring错误", e);
            }
        }
    }
}