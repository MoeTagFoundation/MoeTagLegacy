using MoeTag.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoeTag.Network
{
    internal class DanbooruContentEndpoint : IContentEndpoint
    {
        public string GetContentEndpointName()
        {
            return "Danbooru";
        }

        public NetworkResponseModel? GetNetworkResponseModel(string response)
        {
            ICollection<NetworkResponseNode> networkResponseNodes = new List<NetworkResponseNode>();

            JArray results = null;
            try
            {
                results = JArray.Parse(response);
            }
            catch (JsonReaderException e)
            {
                return null;
            }

            if (results == null) { return null; }

            IReadOnlyCollection<JToken> tokens = results.ToList();
            if (tokens.Any())
            {
                foreach (JToken token in tokens)
                {
                    if (token["preview_file_url"] != null && token["file_url"] != null)
                    {
                        var url = token["preview_file_url"]!.ToString(); // gel = file_url
                        var urlPreview = token["file_url"]!.ToString();

                        // todo: ugoria support
                        if (urlPreview.EndsWith(".zip"))
                        {
                            Console.Error.WriteLine("This video is a .zip; skipping");
                            continue;
                        }

                        var tags_general = token["tag_string_general"]!.ToString(); // tags gen
                        var tags_character = token["tag_string_character"]!.ToString(); // tags char
                        var tags_copyright = token["tag_string_copyright"]!.ToString(); // tags cpy
                        var tags_artist = token["tag_string_artist"]!.ToString(); // tags art
                        var tags_meta = token["tag_string_meta"]!.ToString(); // tags met

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
        }

        public Uri GetUri(MoeNetworkHandler context, int page, int limit, string query)
        {
            query = query.Trim();
            query = query.Replace(' ', '+');
            if (!string.IsNullOrWhiteSpace(APILoginStorage.DanbooruAPILogin) && !string.IsNullOrWhiteSpace(APILoginStorage.DanbooruAPIKey))
            {
                return new Uri($"https://danbooru.donmai.us/posts.json?tags={query}&page={page}&limit={limit}" +
                    $"&api_key={APILoginStorage.DanbooruAPIKey}&login={APILoginStorage.DanbooruAPILogin}", UriKind.Absolute);
            }
            else
            {
                return new Uri($"https://danbooru.donmai.us/posts.json?tags={query}&page={page}&limit={limit}", UriKind.Absolute);
            }
        }

        public bool IsNSFW()
        {
            return true;
        }
    }
}
