using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CafeStudio.UI
{
    public class ImguiFileDialog
    {
        static Dictionary<string, int> SelectedFilters = new Dictionary<string, int>();

        public bool FilterAll { get; set; } = true;

        public bool SaveDialog { get; set; }

        public string[] FilePaths = new string[0];
        public string FilePath => FilePaths.FirstOrDefault();
        public string FileName { get; set; }

        public bool MultiSelect = false;

        string dialogKey;

        readonly List<FileFilter> filters = new List<FileFilter>();

        public void AddFilter(string filter, string description)
        {
            filters.Add(new FileFilter(filter, description));
        }

        public void AddFilter(FileFilter filter)
        {
            filters.Add(filter);
        }

        public bool ShowDialog(string key)
        {
            dialogKey = key;

            if (!SelectedFilters.ContainsKey(key))
                SelectedFilters.Add(key, 0);

            if (SaveDialog)
            {
                var ofd = TinyFileDialog.SaveFileDialog(filters, FileName);
                if (!string.IsNullOrEmpty(ofd))
                {
                    this.FilePaths = new string[] { ofd };
                    return true;
                }
            }
            else
            {
                var ofd = TinyFileDialog.OpenFileDialog(filters, FileName, MultiSelect);
                if (!string.IsNullOrEmpty(ofd))
                {

                    this.FilePaths = ofd.Split('|');
                    return true;
                }
            }

            return false;
        }
    }
}
