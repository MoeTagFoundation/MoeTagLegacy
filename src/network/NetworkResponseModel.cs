using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Network
{
    internal class NetworkResponseModel
    {
        public ICollection<NetworkResponseNode> Nodes;

        public NetworkResponseModel(ICollection<NetworkResponseNode> nodes)
        {
            Nodes = nodes;
        }
    }
}
