using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    [Serializable]
    public class FolderAsset : AssetBase
    {
        public string Directory { get; set; }

        public FolderAsset(string directory)
        {
            Directory = directory;
            Name = new DirectoryInfo(directory).Name;
        }
    }
}
