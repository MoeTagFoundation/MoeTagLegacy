using MoeTag.Debug;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoeTag.Extern.Windows
{
    static class WindowsClipboard
    {
        /**
         * DLL Imports
         * 
         * Required DLLS: user32.dll + kernel32.dll
         */

        [DllImport("kernel32.dll", EntryPoint = "GlobalAlloc", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal extern static IntPtr Win32GlobalAlloc(GAllocFlags Flags, int dwBytes);

        [DllImport("kernel32.dll", EntryPoint = "GlobalLock", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal extern static IntPtr Win32GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalUnlock", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal extern static IntPtr Win32GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", EntryPoint = "SetClipboardData", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal extern static IntPtr Win32SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll", EntryPoint = "OpenClipboard", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool Win32OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", EntryPoint = "CloseClipboard", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool Win32CloseClipboard();

        [DllImport("user32.dll", EntryPoint = "EmptyClipboard", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool Win32EmptyClipboard();

        /**
         * Enums
         */

        internal enum CFTypes : uint
        {
            CF_UNICODETEXT = 13,
            CF_BITMAP = 2,
            CF_DIB = 8
        }

        [Flags]
        internal enum GAllocFlags : uint
        {
            GMEM_FIXED = 0x0000,
            GMEM_MOVEABLE = 0x0002,
            GMEM_NOCOMPACT = 0x0010,
            GMEM_NODISCARD = 0x0020,
            GMEM_ZEROINIT = 0x0040,
            GMEM_MODIFY = 0x0080,
            GMEM_DISCARDABLE = 0x0100,
            GMEM_NOT_BANKED = 0x1000,
            GMEM_SHARE = 0x2000,
            GMEM_DDESHARE = 0x2000,
            GMEM_NOTIFY = 0x4000,
            GMEM_LOWER = GMEM_NOT_BANKED,
            GMEM_VALID_FLAGS = 0x7F72,
            GMEM_INVALID_HANDLE = 0x8000,
            GHND = (GMEM_MOVEABLE | GMEM_ZEROINIT),
            GPTR = (GMEM_FIXED | GMEM_ZEROINIT)
        }

        /**
         * Methods
         */

        // MONO
        internal static byte[] ImageToDib(Image<Rgba32> image)
        {
            byte[] buffer;
            byte[] retbuf;
            // save as bmp into ms
            using (MemoryStream ms = new MemoryStream())
            {
                image.SaveAsBmp(ms);
                buffer = ms.GetBuffer();
            }

            // filter our 14 fheader bytes
            retbuf = new byte[buffer.Length];
            int fileHeaderSize = 14;
            Array.Copy(buffer, fileHeaderSize, retbuf, 0, buffer.Length - fileHeaderSize);

            return retbuf;
        }

        // MONO
        internal static IntPtr CopyToMoveableMemory(byte[] data)
        {
            if (data == null || data.Length == 0)
                // detect this before GlobalAlloc does.
                throw new ArgumentException("Can't create a zero length memory block.");

            IntPtr hmem = Win32GlobalAlloc(GAllocFlags.GMEM_MOVEABLE | GAllocFlags.GMEM_DDESHARE, data.Length);
            if (hmem == IntPtr.Zero)
                throw new Win32Exception();
            IntPtr hmem_ptr = Win32GlobalLock(hmem);
            if (hmem_ptr == IntPtr.Zero) // If the allocation was valid this shouldn't occur.
                throw new Win32Exception();
            Marshal.Copy(data, 0, hmem_ptr, data.Length);
            Win32GlobalUnlock(hmem);
            return hmem;
        }

        public static void CopyImageToClipboard(Image<Rgba32> image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (Win32OpenClipboard(MoeApplication.HWND))
            {
                if (!MultiTryOpenClipboard())
                {
                    ThrowWin32();
                }

                if(!Win32EmptyClipboard())
                {
                    ThrowWin32();
                }

                byte[] data = ImageToDib(image);
                if (data.Length <= 0)
                {
                    throw new NullReferenceException();
                }

                IntPtr hmem = CopyToMoveableMemory(data);
                if (hmem == IntPtr.Zero)
                {
                    ThrowWin32();
                }

                if (Win32SetClipboardData((uint)CFTypes.CF_DIB, hmem) == IntPtr.Zero)
                {
                    ThrowWin32();
                }

                Win32CloseClipboard();
            }
            else
            {
                throw new Exception();
            }
        }
        
        private static bool MultiTryOpenClipboard(int attempts = 10)
        {
            while (true)
            {
                if (Win32OpenClipboard(MoeApplication.HWND))
                {
                    break;
                }

                if (--attempts == 0)
                {
                    ThrowWin32();
                    return false;
                }

                try
                {
                    Thread.Sleep(100);
                } catch (Exception e)
                {
                    MoeLogger.Log("Clipboard", "error: MultiTryOpenClipboard sleep failed: " + e.Message);
                    return false;
                }
            }
            return true;
        }

        private static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
