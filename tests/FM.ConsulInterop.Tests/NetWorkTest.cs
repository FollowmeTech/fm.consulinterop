using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace FM.ConsulInterop.Tests
{
    public class NetWorkTest
    {
        [Fact]
        public void GetIp_By_FullIP()
        {
            var ip = "192.168.0.1";
            var result = NetHelper.GetIp(ip);
            Assert.Equal(ip, result);
        }


        [Fact]
        public void GetIp_By_Error_IPSegment()
        {
            var ip = "110.111.*.*";

            Assert.Throws<Exception>(() =>
            {
                var result = NetHelper.GetIp(ip);
            });
        }


        [Fact]
        public void GetIp_By_Star_IPSegment()
        {
            var host = new List<string>();
            foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    host.Add(ip.ToString());
                }
            }

            foreach (var ip in host)
            {
                //ip ,192.168.0.1 -> 192.168.0.*
                var part = ip.Split('.', StringSplitOptions.RemoveEmptyEntries);
                part[3] = "*";

                var ipsegment = string.Join(".", part);
                var result = NetHelper.GetIp(ip);
                Assert.Equal(ip, result);
            }
        }
    }
}
