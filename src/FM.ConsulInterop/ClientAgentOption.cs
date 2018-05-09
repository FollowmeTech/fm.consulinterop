using Grpc.Core;
using System.Collections.Generic;

namespace FM.ConsulInterop
{
    public class ClientAgentOption
    {
        /// <summary>
        /// Gets or sets the client call action.
        /// </summary>
        /// <value>
        /// The client call action.
        /// </value>
        public ClientCallActionCollection ClientCallActionCollection { get; set; }

        /// <summary>
        /// grpc channel options
        /// </summary>
        public IEnumerable<ChannelOption> ChannelOptions { get; set; }
    }
}
