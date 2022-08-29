using MoeTag.Debug;
using MoeTag.Graphics;
using MoeTag.UI;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Security.Authentication;
using System.Text;

namespace MoeTag.Network
{
    internal class MoeNetworkHandler : IDisposable
    {
        private HttpClientHandler _httpClientHandler;
        private HttpClient _client;
        private short _timeout = 5;

        public int Limit = 100;

        private TimeSpan _lastSearchTime;
        private TimeSpan _searchTime;


        private Dictionary<IContentEndpoint, bool> _endpoints;

        public Dictionary<IContentEndpoint, bool> GetEndpoints()
        {
            return _endpoints;
        }

        public async Task BuildLocalTagCache()
        {
            for(int i = 0; i < 300; i++)
            {
                await BuildLocalTagCachePage(i);
            }
            string output = builder.ToString();

            MoeLogger.Log(this, "============= OUTPUT BEGIN =================");

            MoeLogger.Log(this, output);
        }

        StringBuilder builder = new StringBuilder();
        int count = 0;

        public async Task BuildLocalTagCachePage(int page)
        {
            Uri uri = new Uri($"https://danbooru.donmai.us/tags.json?search[order]=count&page={page}");
            MoeLogger.Log(this, "Requesting " + uri + " current total " + count);
            string? result = await _client.GetStringAsync(uri);
            if (result != null)
            {
                JArray arr = JArray.Parse(result);
                List<JToken> tokens = arr.Children().ToList();
                foreach (JToken token in tokens)
                {
                    string name = token["name"].ToString();
                    string post_count = token["post_count"].ToString();

                    string code = $"new TagData(\"{name}\", {post_count}),\n";

                    builder.Append(code);
                    count++;
                }
            }
        }

        public MoeNetworkHandler()
        {
            _httpClientHandler = new HttpClientHandler()
            {
                // https://www.cloudflare.com/learning/ssl/why-use-tls-1.3/
                // Only use TLS 1.3 and 1.2 (pref 1.3)
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                // No use for cookies, so disable so theres no cross tracking
                UseCookies = false,
            };
            _client = new HttpClient(_httpClientHandler);

            // Set Client Timeout
            _client.Timeout = TimeSpan.FromSeconds(_timeout);

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/DNT
            _client.DefaultRequestHeaders.Add("DNT", "1"); // Do not track header

            _endpoints = new Dictionary<IContentEndpoint, bool>();
            _endpoints.Add(new DanbooruContentEndpoint(), true);
            _endpoints.Add(new GelbooruContentEndpoint(), true);
            _endpoints.Add(new SafebooruContentEndpoint(), true);
        }

        public string GetSearchString(SearchState state)
        {
            switch(state)
            {
                case SearchState.FINISHED:
                    if (_searchTime.TotalSeconds != 0.0)
                    {
                        return "Finished in " + _searchTime.TotalSeconds + "s";
                    } else
                    {
                        return "Press 'search' to search";
                    }
                case SearchState.PREPARING:
                    return "Preparing...";
                case SearchState.API_FETCHING:
                    return "Fetching API...";
                case SearchState.TEXTURE_GENERATION:
                    return "Generating Textures...";
                case SearchState.DATA_DOWNLOADING:
                    return "Downloading Data...";
                case SearchState.NO_RESULTS:
                    return "No results";
                default:
                    return "Invalid State";
            }
        }

        private async Task<NetworkResponseModel?> GetApiResult(IContentEndpoint endpoint, int page, string tags)
        {
            Uri uri = endpoint.GetUri(this, page, 100, tags);
            if (uri == null)
            {
                MoeLogger.Log(this, "Invalid Endpoint Uri : NULL");
                return null;
            }

            HttpResponseMessage? resultHttp;
            try
            {
                resultHttp = await _client.GetAsync(uri);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                MoeLogger.Log(this, "Time out");
                return null;
            }

            string? result = null;
            using (StreamReader s = new StreamReader(resultHttp.Content.ReadAsStream()))
            {
                result = await s.ReadToEndAsync();
            }

            if (result == null || string.IsNullOrWhiteSpace(result))
            {
                MoeLogger.Log(this, "error: API Returned NULL Result");
                return null;
            }

            return endpoint.GetNetworkResponseModel(result);
        }

        public bool IsTypeActive(Type type)
        {
            return _endpoints[_endpoints.Keys.Where(e => type.IsInstanceOfType(e)).First()];
        }

        public void SetTypeState(Type type, bool state)
        {
            _endpoints[_endpoints.Keys.Where(e => type.IsInstanceOfType(e)).First()] = state;
        }

        public async Task<bool> GetApiResults(int page, string tags, Action<NetworkResponseModel?> callback)
        {
            _lastSearchTime = DateTime.Now.TimeOfDay;

            // Ensure valid page number
            if(page < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }

            ICollection<Task> tasks = new List<Task>();

            foreach(IContentEndpoint endpoint in _endpoints.Keys)
            {
                if (_endpoints[endpoint])
                {
                    tasks.Add(GetApiResult(endpoint, page, tags).ContinueWith(async (resp) => callback(await resp)));
                }
            }
            await Task.WhenAll(tasks);

            EndTimer();

            return true;
        }

        public void EndTimer()
        {
            _searchTime = DateTime.Now.TimeOfDay - _lastSearchTime;
        }

        public async Task DownloadContentThumbnail(MoeContentModel model)
        {
            MoeLogger.Log(this, "[Attempting Load]: " + model.ThumbnailUrl);

            Uri tUri = new Uri(model.ThumbnailUrl, UriKind.Absolute);
            using (var b = await _client.GetStreamAsync(tUri))
            {
                // Remember to dispose of this image once you are finished.
                try
                {
                    Image<Rgba32> image = await Image.LoadAsync<Rgba32>(Configuration.Default, b);

                    MoeLogger.Log(this, "[Loaded]: " + model.ThumbnailUrl);

                    model.SetDataThumbnail(image);
                }
                catch (Exception ex)
                {
                    MoeLogger.Log(this, "Task Cancelled for Image (likely due to timeout) " + ex.Message);
                }
            }
        }

        public async Task DownloadContentPreview(MoeContentModel model, long imageUpdateRate)
        {
            MoeLogger.Log(this, "[Attempting Load PREVIEW]: " + model.PreviewUrl);

            using var b = await _client.GetStreamAsync(model.PreviewUrl);
            try
            {
                model.BytesRead = 0;

                ICollection<byte> bytes = new List<byte>();
                int bt = b.ReadByte();

                void updatePreview()
                {
                    try
                    {
                        byte[] byteArray = bytes.ToArray();
                        Image<Rgba32> a = Image.Load<Rgba32>(byteArray);
                        model.SetDataPreview(a);
                    }
                    catch (Exception ex)
                    {
                        MoeLogger.Log(this, ex.Message);
                    }
                }

                bool video = false;
                if (model.PreviewUrl.EndsWith(".mp4") ||
                    model.PreviewUrl.EndsWith(".webm") ||
                    model.PreviewUrl.EndsWith(".zip") ||
                    model.PreviewUrl.EndsWith(".gif"))
                {
                    video = true;
                }

                Task? updatingPreview = null;
                DateTime start = DateTime.Now;
                while (bt != -1)
                {
                    bytes.Add((byte)bt);
                    bt = b.ReadByte();
                    model.BytesRead += 1;
                    if (!video)
                    {
                        TimeSpan timeItTook = DateTime.Now - start;
                        if (timeItTook > TimeSpan.FromMilliseconds(imageUpdateRate))
                        {
                            if (updatingPreview == null || updatingPreview.IsCompleted)
                            {
                                MoeLogger.Log(this, "Generating new preview");
                                updatePreview();
                            }
                            else
                            {
                                MoeLogger.Log(this, "Overlap, Can't!");
                            }
                            start = DateTime.Now;
                        }
                    }
                }
                if (!video)
                {
                    updatePreview();
                }
                else
                {
                    string p = Path.GetTempPath() + Guid.NewGuid().ToString() + Path.GetExtension(model.PreviewUrl);
                    await File.WriteAllBytesAsync(p, bytes.ToArray());
                    model.SetDataPreviewVideo(p);
                }
            }
            catch (Exception ex)
            {
                MoeLogger.Log(this, "Task Cancelled for Image PREVIEW (likely due to timeout) " + ex.Message);
            }

        }

        public void Dispose()
        {
            _httpClientHandler.Dispose();
            _client.Dispose();
        }
    }
}
