using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeTag.Network
{
    internal class GelbooruContentEndpoint : IContentEndpoint
    {
        public string GetContentEndpointName()
        {
            return "Gelbooru";
        }

        public NetworkResponseModel? GetNetworkResponseModel(string response)
        {
            ICollection<NetworkResponseNode> networkResponseNodes = new List<NetworkResponseNode>();

            JToken results = null;
            try
            {
                results = JToken.Parse(response);
            }
            catch (JsonReaderException e)
            {
                return null;
            }

            if (results == null) { return null; }
            if (results["post"] == null) { return null; }

            IEnumerable<JToken> tokens = results["post"].Children();
            if (tokens.Count() > 0)
            {
                ICollection<Task> tasks = new List<Task>();
                foreach (JToken token in tokens)
                {
                    if (token["preview_url"] != null && token["file_url"] != null)
                    {
                        var url = token["preview_url"]!.ToString(); // gel = file_url
                        var urlPreview = token["file_url"]!.ToString();

                        // todo: ugoria support
                        if (urlPreview.EndsWith(".zip"))
                        {
                            Console.Error.WriteLine("This video is a .zip; skipping");
                            continue;
                        }

                        var tags_general = token["tags"]!.ToString(); // tags gen

                        networkResponseNodes.Add(new NetworkResponseNode()
                        {
                            PreviewUrl = urlPreview,
                            ThumbnailUrl = url,
                            Tags = tags_general,
                            TagCharacter = "",
                            TagArtist = "",
                            TagCopyright = "",
                            TagsMeta = ""
                        });
                    }
                }
            }

            return new NetworkResponseModel(networkResponseNodes);
        }

        public Uri GetUri(MoeNetworkHandler context, int page = 1, int limit = 100, string query = "")
        {
            return new Uri($"https://gelbooru.com/index.php?page=dapi&s=post&q=index&pid={page}&tags={query}&json=1", UriKind.Absolute);
        }

        public bool IsNSFW()
        {
            return true;
        }
    }
}
