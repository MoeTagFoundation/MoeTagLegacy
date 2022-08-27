using Mochi.DearImGui;
using MoeTag.Debug;
using MoeTag.Extern.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace MoeTag.Extern
{
    internal class MoeClipboardInterface
    {
        public static unsafe bool SetTextClipboard(string text)
        {
            if(text == null)
            {
                return false;
            }

            ImGui.SetClipboardText(text);

            return true;
        }

        public static unsafe bool SetImageClipboard(Image<Rgba32>? image)
        {
            if(image == null)
            {
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsClipboard.CopyImageToClipboard(image);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // TODO: Linux Clipboard (xclip? bash?)
                MoeLogger.Log("Clipboard", "error: Linux Clipboard Not Implemented Yet");
                throw new NotImplementedException();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: OSX Clipboard (Lower Priority)
                MoeLogger.Log("Clipboard", "error: OSX Clipboard Not Implemented Yet");
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidOperationException("Unsupported Platform for Image Clipboard");
            }
        }
    }
}
