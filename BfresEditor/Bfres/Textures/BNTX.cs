using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.IO;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresEditor
{
    public class BNTX : IFileFormat, IPropertyDisplay, ITextureContainer, IRenamableNode
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "BNTX" };
        public string[] Extension { get; set; } = new string[] { "*.bntx" };

        public File_Info FileInfo { get; set; }

        //Determines how to open the file format when a file is loaded in STFileLoader
        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "BNTX");
            }
        }

        public object PropertyDisplay => BntxFile;

        public BntxFile BntxFile { get; set; }

        //A list of textures
        public List<STGenericTexture> Textures = new List<STGenericTexture>();

        public IEnumerable<STGenericTexture> TextureList => Textures;

        public void Load(Stream stream)
        {
            BntxFile = new BntxFile(stream);
            //Load the textures into generic textures
            foreach (var tex in BntxFile.Textures)
                Textures.Add(new BntxTexture(BntxFile, tex));
        }

        public void Save(Stream stream) {
            BntxFile.Save(stream);
        }

        public bool DisplayIcons => true;

        public string GetRenameText() => BntxFile.Name;

        public void Renamed(string text)
        {
            BntxFile.Name = text;
        }

        public EventHandler OnRenamed => (s, e) => {
            BntxFile.Name = (string)s;
        };

    }
}
