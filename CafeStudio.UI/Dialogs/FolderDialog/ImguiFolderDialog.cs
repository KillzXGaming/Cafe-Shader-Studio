using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            var ofd = TinyFileDialog.SelectFolderDialog(Title, SelectedPath);
            if (!string.IsNullOrEmpty(ofd))
            {
                this.SelectedPath = ofd;
                return true;
            }

            return false;
        }
    }
}
