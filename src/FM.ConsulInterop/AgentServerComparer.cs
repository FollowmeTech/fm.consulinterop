using Consul;
using System.Collections.Generic;

namespace FM.ConsulInterop
{
    /// <summary>
    /// Consul Agent Server Comparer
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{Consul.AgentService}" />
    internal class AgentServerComparer : IEqualityComparer<AgentService>
    {
        public bool Equals(AgentService x, AgentService y)
        {
            return x.ID == y.ID;
        }

        public int GetHashCode(AgentService obj)
        {
            return $"{obj.ID}".GetHashCode();
        }
    }

}
