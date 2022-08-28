
using MoeTag.Debug;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MoeTag.Network
{
    internal class SafebooruContentEndpoint : IContentEndpoint
    {
        public string GetContentEndpointName()
        {
            return "Safebooru";
        }

        public NetworkResponseModel? GetNetworkResponseModel(string response)
        {
            ICollection<NetworkResponseNode> networkResponseNodes = new List<NetworkResponseNode>();

            XDocument results = XDocument.Parse(response);
            if (results != null)
            {
                XElement? element = results.Element("posts");
                if(element == null)
                {
                    MoeLogger.Log(this, "error: no element posts found in safebooru");
                    return null;
                }
                IEnumerable<XElement> tokens = element.Elements();
                if(tokens == null)
                {
                    MoeLogger.Log(this, "error: no elements in post found in safebooru");
                    return null;
                }
                if (tokens.Any())
                {
                    MoeLogger.Log(this, tokens.First().Value + ", " + tokens.First().Name);
                    foreach (XElement token in tokens)
                    {
                        if (token.Attribute("preview_url") != null && token.Attribute("file_url") != null)
                        {
                            var url = token.Attribute("preview_url")!.Value; // gel = file_url
                            var urlPreview = token.Attribute("file_url")!.Value;

                            // todo: ugoria support
                            if (urlPreview.EndsWith(".zip"))
                            {
                                Console.Error.WriteLine("This video is a .zip; skipping");
                                continue;
                            }

                            var tags_general = token.Attribute("tags")!.Value; // tags gen
                            var tags_character = ""; // tags char
                            var tags_copyright = ""; // tags cpy
                            var tags_artist = ""; // tags art
                            var tags_meta = ""; // tags met

                            networkResponseNodes.Add(new NetworkResponseNode()
                            {
                                PreviewUrl = urlPreview,
                                ThumbnailUrl = url,
                                Tags = tags_general,
                                TagCharacter = tags_character,
                                TagArtist = tags_artist,
                                TagCopyright = tags_copyright,
                                TagsMeta = tags_meta
                            });
                        }
                    }
                }

                return new NetworkResponseModel(networkResponseNodes);
            } else
            {
                return null;
            }
        }

        public Uri GetUri(MoeNetworkHandler context, int page = 1, int limit = 100, string query = "")
        {
            return new Uri($"https://safebooru.org/index.php?page=dapi&s=post&q=index&pid={page}&tags={query}", UriKind.Absolute);
        }

        public bool IsNSFW()
        {
            return false;
        }
    }
}
