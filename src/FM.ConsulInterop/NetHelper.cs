using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace FM.ConsulInterop
{
    public static class NetHelper
    {
        /// <summary>
        /// The ip segment regex
        /// </summary>
        private const string IPSegmentRegex = @"\d{0,3}";

        /// <summary>
        /// Gets the ip.
        /// </summary>
        /// <param name="ipSegment">ip段</param>
        /// <returns></returns>
        public static string GetIp(string ipSegment)
        {
            if (string.IsNullOrWhiteSpace(ipSegment))
                throw new ArgumentNullException(nameof(ipSegment));

            //如果设置的IP支持* 的时候,再去智能的选择ip
            if (!ipSegment.Contains("*"))
            {
                return ipSegment;
            }

            ipSegment = ipSegment.Replace("*", IPSegmentRegex).Replace(".", "\\.");

            var hostAddrs = NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => !(a.IsIPv6LinkLocal || a.IsIPv6Multicast || a.IsIPv6SiteLocal || a.IsIPv6Teredo))
                .ToList();

            foreach (var ip in hostAddrs)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && System.Text.RegularExpressions.Regex.IsMatch(ip.ToString(), ipSegment))
                {
                    return ip.ToString();
                }
            }

            var allIps = string.Join("|", hostAddrs.ConvertAll(p => p.ToString()));
            throw new Exception($"所有的IP:({allIps})中, 找不到ipsegement:{ipSegment}匹配的ip");
        }
    }
}
