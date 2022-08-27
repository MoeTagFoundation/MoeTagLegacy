using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Network
{
    internal interface IContentEndpoint
    {
        public bool IsNSFW();

        public string GetContentEndpointName();

        public Uri GetUri(MoeNetworkHandler context, int page = 1, int limit = 100, string query = "");

        public NetworkResponseModel? GetNetworkResponseModel(string response);
    }
}
