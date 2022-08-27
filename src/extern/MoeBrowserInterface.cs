using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MoeTag.Extern
{
    internal class MoeBrowserInterface
    {
        public static bool OpenUrl(string url)
        {
            if(String.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                    return true;
                }
                else
                {
                    throw;
                }
            }

            return false;
        }
    }
}
