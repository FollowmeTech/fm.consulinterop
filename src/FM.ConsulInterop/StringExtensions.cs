using System;
using System.Collections.Generic;

namespace FM.ConsulInterop
{
    static class StringExtensions
    {
        /// <summary>
        /// Parses the connection string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">解析connectionstring错误</exception>
        public static T ParseConnectionString<T>(this string connectionString)
        {
            try
            {
                var keyValueDict = new Dictionary<string, object>();
                foreach (var kv in connectionString.Split(';'))
                {
                    var keyValue = kv.Split('=');
                    keyValueDict[keyValue[0]] = keyValue[1];
                }

                var config = Activator.CreateInstance(typeof(T));
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

                return (T)config;
            }
            catch (Exception e)
            {
                throw new ArgumentException("解析connectionstring错误", e);
            }
        }
    }
}
