using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace CafeStudio.UI
{
    public class BrowserHelper
    {
        public static void OpenDonation() {
            OpenURLEncoded("aHR0cHM6Ly93d3cucGF5cGFsLmNvbS9kb25hdGU/YnVzaW5lc3M9VENHN1A2UEg2VjNQVSZjdXJyZW5jeV9jb2RlPVVTRA==");
        }

        public static void OpenURLEncoded(string encodedString)
        {
            byte[] data = Convert.FromBase64String(encodedString);
            OpenURL(Encoding.UTF8.GetString(data));
        }

        public static void OpenURL(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); // Not tested
            }
        }
    }
}
