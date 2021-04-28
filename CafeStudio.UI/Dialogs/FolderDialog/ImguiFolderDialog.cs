using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CafeStudio.UI
{
    public class ImguiFolderDialog
    {
        public string Title { get; set; } = "Folder Select";

        public string SelectedPath { get; set; } = "";

        public bool ShowDialog()
        {
            string ofd = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FolderBrowserEx.FolderBrowserDialog dialog = new FolderBrowserEx.FolderBrowserDialog() { Title = Title, InitialFolder = SelectedPath };
                dialog.ShowDialog();
                ofd = dialog.SelectedFolder;
            }
            else
            {
                ofd = TinyFileDialog.SelectFolderDialog(Title, SelectedPath);
            }

            if (!string.IsNullOrEmpty(ofd))
            {
                this.SelectedPath = ofd;
                return true;
            }

            return false;
        }
    }
}
