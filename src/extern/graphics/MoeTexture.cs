using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace MoeTag.Graphics
{
    class MoeTexture : IDisposable
    {
        public const SizedInternalFormat Srgb8Alpha8 = (SizedInternalFormat)All.Srgb8Alpha8; // Not using SRGB Profile

        // Internal Formats
        public const SizedInternalFormat RGB32F = (SizedInternalFormat)All.Rgb32f;
        public const PixelFormat RGBA = PixelFormat.Rgba;

        // Internal Pointer
        private int GLTexture;
        // External Fetch
        public IntPtr GLTexturePtr { get { return (IntPtr)GLTexture; } }

        // Data Buffer
        private int[]? _dataBuffer;

        private readonly int _width;
        private readonly int _height;
        private Vector2 _size;
        public Vector2 Size { 
            get {
                return _size;
            }
        }

        public MoeTexture(IntPtr data)
        {
            GLTexture = (int)data;
        }

        public MoeTexture(Image<Rgba32> data)
        {
            _width = data.Width;
            _height = data.Height;
            _size = new Vector2(_width, _height);
            _dataBuffer = null;

            GenerateDataBuffer(data);
        }

        // ASNC
        private void GenerateDataBuffer(Image<Rgba32> data)
        {
            if (data != null)
            {
                _dataBuffer = new int[_width * _height];
                for (var x = 0; x < _width; x++)
                {
                    for (var y = 0; y < _height; y++)
                    {
                        var color = data[x, y];
                        _dataBuffer[y * _width + x] = (color.A << 24) | (color.B << 16) | (color.G << 8) | (color.R << 0);
                    }
                }
            }
        }

        // SYNC
        public void GenerateTexture()
        {     
            GL.CreateTextures(TextureTarget.Texture2D, 1, out GLTexture);
            Util.CheckGLError("CreateTexture");

            string Name = "MoeTexture";
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, GLTexture, Name.Length, Name);

            GL.TextureStorage2D(GLTexture, 1, RGB32F, _width, _height);
            Util.CheckGLError("Storage2d");

            ApplyDataBuffer();

            GL.TextureParameter(GLTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            Util.CheckGLError("MagFilter");
            GL.TextureParameter(GLTexture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Util.CheckGLError("MinFilter");
        }

        // SYNC
        private void ApplyDataBuffer()
        {
            if (_dataBuffer != null)
            {
                unsafe
                {
                    fixed (int* dataptr = _dataBuffer)
                    {
                        GL.TextureSubImage2D(GLTexture, 0, 0, 0, _width, _height, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)dataptr);
                        Util.CheckGLError("SubImage");
                    }
                }
                _dataBuffer = null;
            }
        }

        public void Dispose()
        {
            GL.DeleteTexture(GLTexture);
            Util.CheckGLError("DeleteTexture");

            if(_dataBuffer != null)
            {
                _dataBuffer = null;
            }
        }
    }
}