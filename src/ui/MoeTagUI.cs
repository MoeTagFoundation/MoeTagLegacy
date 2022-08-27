using Mochi.DearImGui;
using MoeTag.Debug;
using MoeTag.Extern;
using MoeTag.Graphics;
using MoeTag.Lang;
using MoeTag.Network;
using MoeTag.Save;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using NativeFileDialogSharp;

using static MoeTag.UI.DefaultTags;
using System.Text;

namespace MoeTag.UI
{
    public class MoeTagUI : IDisposable
    {
        private MoeNetworkHandler? _moeNetworkHandler;
        private MoeContentManager? _moeContentManager;
        private MoeSaveData? _moeSaveData;
        private ILanguageProvider _moeLanguageProvider;

        private int _columnCount = 5; // Width of Columns
        private int _columnSize = 150;

        private int _imageUpdateRate = 2000;
        private int _page = 0; // Page being searched
        private SearchState _searchState = SearchState.FINISHED;

        private string _tagsOld = "_";
        private List<TagData> data = new List<TagData>();
        private int counter = 0;

        private bool _searchStateFinished
        {
            get { return _searchState == SearchState.FINISHED || _searchState == SearchState.NO_RESULTS; }
        }

        // Public Functions

        public MoeTagUI()
        {
            _moeNetworkHandler = new MoeNetworkHandler();
            _moeContentManager = new MoeContentManager();
            _moeLanguageProvider = new EnglishLanguageProvider();
            if(!File.Exists("moesavedata.json"))
            {
                using (StreamWriter stream = new StreamWriter(File.Create("moesavedata.json")))
                {
                    MoeSaveData saveData = new MoeSaveData();
                    saveData.SetProperty("DanbooruLogin", "");
                    saveData.SetProperty("DanbooruKey", "");
                    string json = JsonConvert.SerializeObject(saveData);
                    stream.Write(json);
                }
            }
            string saveDataStr = File.ReadAllText("moesavedata.json");
            _moeSaveData = JsonConvert.DeserializeObject<MoeSaveData>(saveDataStr);

            MoeUnmanagedHelper.AddUnmanagedString("Tags");
            MoeUnmanagedHelper.AddUnmanagedString("DanbooruLogin");
            MoeUnmanagedHelper.AddUnmanagedString("DanbooruKey");

            APILoginStorage.DanbooruAPILogin = _moeSaveData.GetPropertyString("DanbooruLogin");
            APILoginStorage.DanbooruAPIKey = _moeSaveData.GetPropertyString("DanbooruKey");
            MoeUnmanagedHelper.SetUnmanagedString("DanbooruLogin", APILoginStorage.DanbooruAPILogin);
            MoeUnmanagedHelper.SetUnmanagedString("DanbooruKey", APILoginStorage.DanbooruAPIKey);
        }

        private void DrawSearchUI()
        {
            ImGui.Dummy(new Vector2(20, 10));

            foreach (IContentEndpoint endpoint in _moeNetworkHandler.GetEndpoints().Keys)
            {
                bool active = _moeNetworkHandler.GetEndpoints()[endpoint];
                unsafe
                {
                    string suffix = endpoint.IsNSFW() ? " (NSFW)" : " (SFW)";
                    ImGui.Checkbox(endpoint.GetContentEndpointName() + suffix, &active);
                }
                if(ImGui.IsItemHovered())
                {
                    if (active)
                    {
                        ImGui.SetTooltip("Including results from " + endpoint.GetContentEndpointName());
                    } else
                    {
                        ImGui.SetTooltip("Excluding results from " + endpoint.GetContentEndpointName());
                    }
                }
                _moeNetworkHandler.GetEndpoints()[endpoint] = active;
            }

            ImGui.Dummy(new Vector2(20, 10));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(20, 10));

            float width = ImGui.GetContentRegionAvail().X;

            unsafe
            {
                fixed (byte* bufferPtr = MoeUnmanagedHelper.GetRawBuffer("Tags"))
                {
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.InputTextWithHint("", _moeLanguageProvider.GetLanguageNode(LanguageNodeType.SEARCH_INPUT_HINT),
                        bufferPtr, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        Search();
                    }
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SEARCH_INPUT_TOOLTIP));
            }

            if (!_searchStateFinished)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.0f, 0.0f, 1.0f));
            if (ImGui.Button(_moeNetworkHandler.GetSearchString(_searchState),
                new Vector2(width, 30)) && _searchStateFinished)
            {
                Search();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SEARCH_BUTTON_TOOLTIP));
            }

            if (_page <= 0)
            {
                ImGui.BeginDisabled();
            }
            if (ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PAGE_LABEL, "" + Math.Max(0, (_page - 1))), new Vector2(width / 2.0f, 30)))
            {
                _page -= 1;
                _page = Math.Max(_page, 0);
                Search(resetPage: false);
            }
            if (_page <= 0)
            {
                ImGui.EndDisabled();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Move to page " + (_page - 1));
            }
            ImGui.SameLine();

            if (ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PAGE_LABEL, "" + (_page + 1)), new Vector2(width / 2.0f, 30)))
            {
                _page += 1;
                Search(resetPage: false);
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Move to page " + (_page + 1));
            }

            if (!_searchStateFinished)
            {
                ImGui.EndDisabled();
            }

            ImGui.PopStyleColor();

            ImGui.Separator();

            string currentTags = MoeUnmanagedHelper.GetUnmanagedString("Tags");

            if (currentTags != _tagsOld)
            {
                data = DefaultTags.FuzzySearch(currentTags);          
                _tagsOld = currentTags;
            }
            foreach (TagData tag in data)
            {
                if (ImGui.Button(tag.Name, new Vector2(0, 0)))
                {
                    MoeUnmanagedHelper.SetUnmanagedString("Tags", tag.Name);
                    Search();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_SEARCH_TOOLTIP, tag.Name));
                }

                ImGui.SameLine();
                ImGui.Text("x " + tag.PostCount);
            }

            ImGui.Separator();
        }

        public void DrawUI(OpenTK.Mathematics.Vector2i clientSize)
        {
            unsafe
            {
                ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
            }
            bool drawUI = true;
            if (drawUI)
            {
                unsafe
                {
                    ImGui.Begin(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SECTION_SEARCHSETTINGS));
                }
    
                ImGui.Separator();

                DrawSearchUI();

                if (ImGui.CollapsingHeader(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.HEADER_SYSTEMINFO)))
                {
                    //ImGui.Text("FPS: " + Math.Round(1f / e.Time));
                    ImGui.Text("OpenGL Version: " + GL.GetString(StringName.Version));
                    ImGui.Text("GPU: " + GL.GetString(StringName.Renderer));
                    ImGui.Text("OS: " + RuntimeInformation.OSDescription);
                    ImGui.Text("Arch: " + RuntimeInformation.ProcessArchitecture);

                    var memory = 0.0;
                    using (Process proc = Process.GetCurrentProcess())
                    {
                        // The proc.PrivateMemorySize64 will returns the private memory usage in byte.
                        // Would like to Convert it to Megabyte? divide it by 2^20
                        memory = proc.PrivateMemorySize64 / (1024 * 1024);
                    }
                    ImGui.Text("PrivateMemorySize64: " + memory + "MB");
                    unsafe { ImGui.Text("IM Framerate: " + (int)Math.Round(ImGui.GetIO()->Framerate)); }
                }

                ImGui.Separator();

                if (ImGui.CollapsingHeader(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.HEADER_SETTINGS)))
                {
                    unsafe
                    {
                        int temp = _columnSize;
                        ImGui.SliderInt("Column Size", &temp, 10, 500, "%dpx");
                        _columnSize = temp;

                        fixed (int* imageUpdateRatePtr = &_imageUpdateRate)
                        {
                            ImGui.SliderInt("Image Update Rate", imageUpdateRatePtr, 200, 5000, "Every %dms");
                        }

                        fixed (byte* danbooruApiPtr = MoeUnmanagedHelper.GetRawBuffer("DanbooruKey"))
                        {
                            ImGui.InputText("Danbooru API Key", danbooruApiPtr, 128);
                        }
                        APILoginStorage.DanbooruAPIKey = MoeUnmanagedHelper.GetUnmanagedString("DanbooruKey");


                        fixed (byte* danbooruApiLogin = MoeUnmanagedHelper.GetRawBuffer("DanbooruLogin"))
                        {
                            ImGui.InputText("Danbooru API Login", danbooruApiLogin, 128);
                        }
                        APILoginStorage.DanbooruAPILogin = MoeUnmanagedHelper.GetUnmanagedString("DanbooruLogin");
                    }
                }


                ImGui.End();

                unsafe
                {
                    ImGui.Begin(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SECTION_PREVIEWINFO));
                }

                if (_moeContentManager.GetCurrentModelPreview() != null)
                {
                    if (_moeContentManager.GetCurrentModelPreview().Tags != null)
                    {

                        void displayTag(string text, string tag)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                if (ImGui.Button(tag, new Vector2(0, 0)))
                                {
                                    MoeUnmanagedHelper.SetUnmanagedString("Tags", tag);
                                    Search();
                                }
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_SEARCH_TOOLTIP, tag));
                                }
                                ImGui.SameLine();
                                ImGui.Text("(" + text + ")");
                            }
                        }

                        displayTag(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_ARTIST),
                            _moeContentManager.GetCurrentModelPreview().Tags_Artist);
                        displayTag(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_CHARACTER),
                            _moeContentManager.GetCurrentModelPreview().Tags_Artist);
                        displayTag(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_COPYRIGHT),
                            _moeContentManager.GetCurrentModelPreview().Tags_Copyright);

                        List<string> metaTags = _moeContentManager.GetCurrentModelPreview().Tags_Meta.Split(" ").ToList();
                        int tagMetaIndex = 0;
                        foreach (string metaTag in metaTags)
                        {
                            if (!string.IsNullOrWhiteSpace(metaTag))
                            {
                                tagMetaIndex++;
                                displayTag($"{_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_META)} {tagMetaIndex}", metaTag);
                            }
                        }

                        List<string> tags = _moeContentManager.GetCurrentModelPreview().Tags.Split(" ").ToList();
                        int tagIndex = 0;
                        foreach (string tag in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                tagIndex++;
                                displayTag($"{_moeLanguageProvider.GetLanguageNode(LanguageNodeType.TAG_TAG)} {tagIndex}", tag);
                            }
                        }
                    }
                    else
                    {
                        ImGui.Text(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.WARNING_NOTAGS));
                    }
                }

                ImGui.End();

                unsafe
                {
                    ImGui.Begin(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SECTION_CONTENTGRID));
                }

                if(_moeContentManager.UpdateDataBuffer() && _searchState == SearchState.TEXTURE_GENERATION)
                {
                    if (_moeNetworkHandler != null)
                    {
                        _moeNetworkHandler.EndTimer();
                    }
                    _searchState = SearchState.FINISHED;
                }

                RenderImageGrid();

                ImGui.End();

                unsafe
                {
                    ImGui.Begin(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.SECTION_PREVIEW),
                        flags: ImGuiWindowFlags.NoScrollbar);
                }

                bool _disposingTab = false;

                if (ImGui.BeginTabBar("PreviewTabs", flags: ImGuiTabBarFlags.Reorderable))
                {
                    if (_moeContentManager.GetModelPreviews().Count > 0)
                    {
                        foreach (MoeContentModel previewModel in _moeContentManager.GetModelPreviews())
                        {
                            bool clicked = false;
                            var flags = ImGuiTabItemFlags.Leading | ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;
                            flags |= (_moeContentManager.IsCurrentPreview(previewModel)) ? ImGuiTabItemFlags.SetSelected : ~ImGuiTabItemFlags.SetSelected;
                            unsafe
                            {
                                clicked = ImGui.BeginTabItem(previewModel.PreviewUrl, flags: flags);
                            }
                            if (clicked)
                            {
                                _moeContentManager.SetPreview(previewModel);

                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.3f, 0.3f, 1.0f));
                                if (string.IsNullOrEmpty(previewModel.PreviewUrl))
                                {
                                    ImGui.BeginDisabled();
                                    ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.BUTTON_CLOSETAB), new Vector2(0, 0));
                                    ImGui.EndDisabled();
                                    ImGui.PopStyleColor();
                                }
                                else
                                {
                                    if (ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.BUTTON_CLOSETAB), new Vector2(0, 0)))
                                    {
                                        _disposingTab = true;
                                        ImGui.PopStyleColor();
                                        break;
                                    }
                                    else
                                    {
                                        ImGui.PopStyleColor();
                                    }
                                }
                                ImGui.SameLine();

                                // Bytes Read Counter / Size Display
                                ImGui.Text(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.LABEL_BYTESREAD,
                                    "" + previewModel.BytesRead, "" + Math.Round(previewModel.MBRead, 2)));

                                if (string.IsNullOrEmpty(previewModel.PreviewUrl))
                                {
                                    ImGui.BeginDisabled();
                                    ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.WARNING_NOCONTENT), new Vector2(0, 0));
                                    ImGui.EndDisabled();
                                }

                                if (previewModel.GetContentType() == MoeContentType.VIDEO)
                                {
                                    if (previewModel.mpv != null)
                                    {
                                        int bw = 80;
                                        int w = (int)ImGui.GetContentRegionAvail().X;
                                        int h = (int)ImGui.GetContentRegionAvail().Y - bw;

                                        int videoUpdateRate = 2;
                                        counter++;
                                        if ((counter % videoUpdateRate) == 0 && previewModel.mpv != null)
                                        {
                                            bool update = _moeContentManager.GetCurrentModelPreview().mpv.Update();
                                            if (update && w > 0 && h > 0)
                                            {
                                                GL.BindFramebuffer(FramebufferTarget.Framebuffer, previewModel.TextureFbo);
                                                GL.BindTexture(TextureTarget.Texture2D, previewModel.TextureVideo);

                                                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, w, h, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);

                                                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                                                GL.BindTexture(TextureTarget.Texture2D, 0);

                                                previewModel.mpv.Render(w, h, previewModel.TextureFbo);

                                                GL.Viewport(0, 0, (int)clientSize.X, (int)clientSize.Y);
                                            }
                                        }

                                        unsafe
                                        {
                                            if (previewModel.TextureVideo != 0)
                                            {
                                                ImGui.Image(new IntPtr(previewModel.TextureVideo).ToPointer(), new Vector2(w, h),
                                                    new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                                            }
                                        }

                                        float seek = (float)previewModel.mpv.PlayPercent;
                                        ImGui.SetNextItemWidth(w);
                                        unsafe
                                        {
                                            if(ImGui.SliderFloat("", &seek, 0.0f, 100.0f, "%.2f / 100.0"))
                                            {
                                                previewModel.mpv.SetTime(seek);
                                            }
                                        }
                                        int sliderH = 20;

                                        if (!previewModel.mpv.Playing)
                                        {
                                            if (ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PLAY_BUTTON), new Vector2(w /2, bw - sliderH)))
                                            {
                                                previewModel.mpv.Play();
                                            }
                                            ImGui.SameLine();
                                            ImGui.BeginDisabled();
                                            ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PAUSE_BUTTON), new Vector2(w / 2, bw - sliderH));
                                            ImGui.EndDisabled();
                                        }
                                        else
                                        {
                                            ImGui.BeginDisabled();
                                            ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PLAY_BUTTON), new Vector2(w / 2, bw - sliderH));
                                            ImGui.EndDisabled();
                                            ImGui.SameLine();
                                            if (ImGui.Button(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.PAUSE_BUTTON), new Vector2(w / 2, bw - sliderH)))
                                            {
                                                previewModel.mpv.Pause();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    MoeTexture? previewTexture = previewModel.GetMoeTexturePreview();
                                    if (previewTexture != null &&
                                        previewTexture.Size != Vector2.Zero &&
                                        previewTexture.GLTexturePtr != IntPtr.Zero)
                                    {
                                        DrawTexture(previewTexture, ImGui.GetContentRegionAvail());
                                        if(ImGui.BeginPopupContextWindow("ContextImage"))
                                        {
                                            unsafe {
                                                if (ImGui.MenuItem("Copy Content to Clipboard", "c"))
                                                {
                                                    MoeClipboardInterface.SetImageClipboard(previewModel.GetPreviewData());
                                                }
                                                if (ImGui.MenuItem("Save Content as File", "s"))
                                                {
                                                    DialogResult result = Dialog.FileSave("png,jpg,webp", Path.Combine("", Path.GetFileName(previewModel.PreviewUrl)));
                                                    string path = result.Path;
                                                    if(result.IsOk)
                                                    {
                                                        previewModel.SaveTo(path);
                                                    }
                                                }
                                                if (ImGui.MenuItem("Open URL in Browser", "o"))
                                                {
                                                    MoeBrowserInterface.OpenUrl(previewModel.PreviewUrl);
                                                }
                                                if (ImGui.MenuItem("Copy URL to Clipboard", "u"))
                                                {
                                                    MoeClipboardInterface.SetTextClipboard(previewModel.PreviewUrl);
                                                }
                                            }

                                            ImGui.EndPopup();
                                        } else
                                        {
                                            if (ImGui.IsItemHovered())
                                            {
                                                ImGui.SetTooltip("Right-click for Options");
                                            }
                                        }
                                    }
                                }

                                ImGui.EndTabItem();
                            }
                        }
                    } else {
                        ImGui.Text(_moeLanguageProvider.GetLanguageNode(LanguageNodeType.EMPTY_PREVIEW_TEXT));
                    }
                    ImGui.EndTabBar();
                }

                if (_disposingTab)
                {
                    if (_moeContentManager.GetCurrentModelPreview() != null)
                    {
                        _moeContentManager.RemovePreview(_moeContentManager.GetCurrentModelPreview());
                    }
                    _moeContentManager.ShiftDownPreview();
                }

                ImGui.End();
            }
        }

        internal void SaveData()
        {
            _moeSaveData.SetProperty("DanbooruLogin", APILoginStorage.DanbooruAPILogin);
            _moeSaveData.SetProperty("DanbooruKey", APILoginStorage.DanbooruAPIKey);
            string json = JsonConvert.SerializeObject(_moeSaveData);
            using (StreamWriter stream = new StreamWriter(File.Create("moesavedata.json")))
            {
                stream.Write(json);
            }
        }

        private async void Search(bool resetPage = true)
        {
            // Return if already searching
            if (!_searchStateFinished) { return; }

            if (resetPage) { _page = 0; }

            _searchState = SearchState.PREPARING;
            _moeContentManager!.DisposeUnusedContent();

            void errorExit(string error)
            {
                Console.Error.WriteLine(error);
                _searchState = SearchState.NO_RESULTS;
            }

            _searchState = SearchState.API_FETCHING;

            int totalResults = 0;
            // Fetch
            await _moeNetworkHandler!.GetApiResults(_page, MoeUnmanagedHelper.GetUnmanagedString("Tags"), async (networkResponseModel) =>
            {
                // Download
                _searchState = SearchState.DATA_DOWNLOADING;

                if (networkResponseModel == null)
                {
                    MoeLogger.Log(this, "error: null response model");
                    return;
                }

                if (networkResponseModel.Nodes.Count > 0)
                {
                    ICollection<Task> tasks = new List<Task>();
                    foreach (NetworkResponseNode node in networkResponseModel.Nodes)
                    {
                        if (node != null)
                        {
                            MoeContentModel model = _moeContentManager.AddModel(node.ThumbnailUrl, node.PreviewUrl);
                            model.Tags = node.Tags;
                            model.Tags_Character = node.TagCharacter;
                            model.Tags_Copyright = node.TagCopyright;
                            model.Tags_Artist = node.TagArtist;
                            model.Tags_Meta = node.TagsMeta;

                            tasks.Add(_moeNetworkHandler.DownloadContentThumbnail(model));
                        }
                        totalResults++;
                    }
                    try
                    {
                        await Task.WhenAll(tasks);
                    } catch(UriFormatException exception)
                    {
                        MoeLogger.Log(this, "Error: Invalid URI Format : " + exception.Message);
                        _searchState = SearchState.NO_RESULTS;
                    } catch(ObjectDisposedException exception)
                    {
                        MoeLogger.Log(this, "Error: Disposed while downloading : " + exception.Message);
                        _searchState = SearchState.NO_RESULTS;
                    } catch(HttpRequestException exception)
                    {
                        MoeLogger.Log(this, "Error: HTTP request exception : " + exception.Message);
                        _searchState = SearchState.NO_RESULTS;
                    }
                }
            });

            if (totalResults == 0)
            {
                _searchState = SearchState.NO_RESULTS;
            }
            else
            {
                _searchState = SearchState.TEXTURE_GENERATION;
            }
        }

        private bool DrawTexture(MoeTexture texture, Vector2 fitSize, bool button = false, bool center = true)
        {
            Vector2 currentSize = texture.Size;
            Vector2 scaledSize = texture.Size;

            if (currentSize.X > 0 && currentSize.Y > 0)
            {
                float fitrate = 0.98f; // 98% fitrate
                float ratio = Math.Min(fitSize.X / currentSize.X, fitSize.Y / currentSize.Y);
                scaledSize.Y *= ratio * fitrate;
                scaledSize.X *= ratio * fitrate;
            }

            Vector2 previousPosition = Vector2.Zero;

            if (center)
            {
                previousPosition = ImGui.GetCursorPos();
                ImGui.SetCursorPos(previousPosition + ((fitSize - scaledSize) * 0.5f));
            }

            bool pressed = false;

            // Render
            if (button)
            {
                unsafe
                {
                    pressed = ImGui.ImageButton(texture.GLTexturePtr.ToPointer(), scaledSize, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), 0, new Vector4(1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                }
            }
            else
            {
                unsafe
                {
                    ImGui.Image(texture.GLTexturePtr.ToPointer(), scaledSize, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector4(1.0f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                    pressed = false;
                }
            }

            if(center)
            {
                ImGui.SetCursorPos(previousPosition);
            }

            return pressed;
        }

        private void LoadModel(MoeContentModel model)
        {
            // Clicked on already loaded
            if(_moeContentManager == null)
            {
                MoeLogger.Log(this, "error: content manager null, cannot load model");
                return;
            }
            if (_moeContentManager.GetModelPreviews().Contains(model))
            {
                _moeContentManager.SetPreview(model);
            }
            else
            {
                // New content, loading
                _moeContentManager.AddPreview(model);
                _moeContentManager.SetPreview(model);

                Task.Run(() =>
                {
                    _moeNetworkHandler!.DownloadContentPreview(model, _imageUpdateRate);
                });
            }
        }

        private void RenderImageGrid()
        {
            //ImGui.Columns(_columnWidth, null, false);
            Vector2 tableSize = ImGui.GetContentRegionAvail();
            _columnCount = Math.Max(1, (int)Math.Ceiling(tableSize.X / _columnSize));
            if (ImGui.BeginTable("ImageResults", _columnCount, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders, tableSize))
            {
                MoeContentModel[] modelArray = _moeContentManager!.GetModels().ToArray();
                int total = modelArray.Count();

                for (int i = 0; i < total; i++)
                {
                    // Get texture stream
                    MoeContentModel? model = modelArray[i];
                    if (model == null) {
                        ImGui.TableNextColumn();
                        continue;
                    }
                    // Get texture raw
                    MoeTexture? texture = model.GetMoeTextureThumbnail();
                    if (texture == null)
                    {
                        ImGui.TableNextColumn();
                        continue;
                    }

                    if (texture != null)
                    {
                        float size = tableSize.X;
                        if (_columnCount > 0)
                        {
                            size = tableSize.X / _columnCount;
                        }
                        if (DrawTexture(texture, new Vector2(size), true))
                        {
                            LoadModel(model);
                        }
                        unsafe
                        {
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(model.PreviewUrl);
                            }
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.MenuItem("Open URL in Browser", "o"))
                                {
                                    MoeBrowserInterface.OpenUrl(model.PreviewUrl);
                                }
                                if (ImGui.MenuItem("Copy URL to Clipboard", "u"))
                                {
                                    MoeClipboardInterface.SetTextClipboard(model.PreviewUrl);
                                }
                                if (ImGui.MenuItem("Open Content in MoeTag", "p"))
                                {
                                    LoadModel(model);
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                }
                ImGui.EndTable();
            }
        }

        public void Dispose()
        {
            // Cleanup Network Handler
            if (_moeNetworkHandler != null)
            {
                _moeNetworkHandler.Dispose();
                _moeNetworkHandler = null;
            }

            // Texture Cleanup
            if (_moeContentManager != null)
            {
                _moeContentManager.Dispose();
                _moeContentManager = null;
            }

            // Suppress Finalizer
            GC.SuppressFinalize(this);
        }


    }
}
