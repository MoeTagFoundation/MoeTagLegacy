using MoeTag.Debug;
using MoeTag.Extern;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MoeTag.Graphics
{
    enum MoeContentType
    {
        UNKNOWN,
        VIDEO,
        IMAGE
    }

    enum MoeContentState
    {
        IDLE,
        DATA_PREVIEW,
        DATA_THUMBNAIL,
        DATA_VIDEO,
    }

    class MoeContentModel : IDisposable
    {
        public static readonly MoeContentModel EmptyModel = new("", "");


        public long BytesRead;
        public float MBRead
        {
            get
            {
                return ((BytesRead / 1024f) / 1024f);
            }
        }

        /**
         * Only used for clipboard. TODO: More elegant solution? less memory?
         */
        private Image<Rgba32>? _dataPreview;

        /**
         * Thumbnail and Preview Texture
         */
        private MoeTexture? _textureThumbnail = null;
        public string ThumbnailUrl;
        private MoeTexture? _texturePreview = null;
        public string PreviewUrl;

        /**
         * Video Properties
         */
        public string? VideoPath = null;
        public MPV mpv;

        /**
         * Video Texture Handle
         */
        private int _textureVideo = 0;
        public int TextureVideo { 
            get { return _textureVideo; }
        }

        /**
         * Frame Buffer Object Handle for FBO
         */
        private int _textureFbo = 0;
        public int TextureFbo
        {
            get { return _textureFbo; }
        }

        public string Tags; // TODO: Tag object / list
        public string Tags_Copyright; // TODO: Tag object / list
        public string Tags_Character; // TODO: Tag object / list
        public string Tags_Artist; // TODO: Tag object / list
        public string Tags_Meta; // TODO: Tag object / list

        private MoeContentState _state;
        private MoeContentType _type;

        public MoeTexture? GetMoeTextureThumbnail()
        {
            return _textureThumbnail;
        }

        public MoeTexture? GetMoeTexturePreview()
        {
            return _texturePreview;
        }

        public MoeContentModel(string thumbnailUrl, string previewUrl)
        {
            ThumbnailUrl = thumbnailUrl;
            PreviewUrl = previewUrl;

            _textureThumbnail = null;
            _texturePreview = null;
            _dataPreview = null;

            _state = MoeContentState.IDLE;
            _type = MoeContentType.UNKNOWN;
        }

        public void CheckForContentGeneration()
        {
            bool setIdle = false;

            // If we have thumbnail stuff to generate, generate it
            if (_textureThumbnail != null && _state == MoeContentState.DATA_THUMBNAIL)
            {
                _textureThumbnail.GenerateTexture();
                setIdle = true;
            }

            if (_type == MoeContentType.IMAGE)
            {
                // If we have preview data to generate, generate it
                if (_texturePreview != null && _state == MoeContentState.DATA_PREVIEW)
                {
                    _texturePreview.GenerateTexture();
                    setIdle = true;
                }
            }
            if (_type == MoeContentType.VIDEO)
            {
                // If we have video stuff to generate, generate it
                if (VideoPath != null && _state == MoeContentState.DATA_VIDEO)
                {
                    GenerateVideoGL();

                    mpv = new MPV();
                    mpv.LoadFile(VideoPath);

                    setIdle = true;
                }
            }

            if(setIdle)
            {
                _state = MoeContentState.IDLE;
            }
        }

        private void GenerateVideoGL()
        {
            MoeLogger.Log(this, "Generating VIDEO");
            GL.GenFramebuffers(1, out _textureFbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _textureFbo);
            Util.CheckGLError("BindFramebuffer");

            GL.GenTextures(1, out _textureVideo);
            GL.BindTexture(TextureTarget.Texture2D, _textureVideo);
            Util.CheckGLError("BindTexture");

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Util.CheckGLError("TextureSettingsVideo");

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureVideo, 0);
            Util.CheckGLError("FramebufferTexture2D");

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 100, 100, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            Util.CheckGLError("TexImage2DVideo");

            GL.BindTexture(TextureTarget.Texture2D, 0);
            Util.CheckGLError("VideoUnbindTexture2D");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Util.CheckGLError("VideoUnbindFramebuffer");
        }

        public Image<Rgba32>? GetPreviewData()
        {
            return _dataPreview;
        }

        public MoeContentType GetContentType()
        {
            return _type;
        }

        public MoeContentState GetContentState()
        {
            return _state;
        }

        public void SetDataThumbnail(Image<Rgba32> data)
        {
            _textureThumbnail = new MoeTexture(data);
            _state = MoeContentState.DATA_THUMBNAIL;
        }

        public void SetDataPreview(Image<Rgba32> data)
        {
            _type = MoeContentType.IMAGE;

            _dataPreview = data;
            _texturePreview = new MoeTexture(data);

            _state = MoeContentState.DATA_PREVIEW;
        }

        public void SetDataPreviewVideo(string path)
        {
            _type = MoeContentType.VIDEO;
         
            VideoPath = path;

            _state = MoeContentState.DATA_VIDEO;
        }

        public void Dispose()
        {
            _state = MoeContentState.IDLE;
            _type = MoeContentType.UNKNOWN;
            DisposeThumbnail();
            DisposePreview();
        }

        public void DisposePreview()
        {
            // Free Data Preview
            if(_dataPreview != null)
            {
                _dataPreview.Dispose();
                _dataPreview = null;
            }
            // Free Texture Preview
            if (_texturePreview != null)
            {
                _texturePreview?.Dispose();
                _texturePreview = null;
            }
            // Free MPV
            if (mpv != null)
            {
                mpv.Dispose();
                mpv = null;
            }
            // Free OpenGL Video Stuff
            if (_textureVideo != 0)
            {
                GL.DeleteTexture(_textureVideo);
                Util.CheckGLError("DeleteTextureVideo");
                _textureVideo = 0;
            }
            if (_textureFbo != 0)
            {
                GL.DeleteFramebuffer(_textureFbo);
                Util.CheckGLError("DeleteFboVideo");
                _textureFbo = 0;
            }
            // Delete any Video Temp Files
            if (VideoPath != null && !String.IsNullOrEmpty(VideoPath))
            {
                try
                {
                    File.Delete(VideoPath);
                } catch (Exception ex)
                {
                    MoeLogger.Log(this, "Failed to delete " + VideoPath + " : " + ex.Message);
                }
                VideoPath = null;
            }
        }

        public void DisposeThumbnail()
        {
            if (_textureThumbnail != null)
            {
                _textureThumbnail?.Dispose();
                _textureThumbnail = null;
            }
        }

        internal void SaveTo(string path)
        {
            if (_dataPreview != null)
            {
                try
                {
                    string extension = Path.GetExtension(path);
                    if (String.IsNullOrWhiteSpace(extension))
                    {
                        path += Path.GetExtension(PreviewUrl);
                    }

                    _dataPreview.Save(path);
                } catch (IOException ex) {
                    MoeLogger.Log(this, "IO Exception, Failed: " + ex.Message, LogType.ERROR);
                }
            }
        }
    }
}
