using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using System.IO;
using Toolbox.Core.ViewModels;
using Toolbox.Core;
using Syroot.NintenTools.NSW.Bntx;
using Toolbox.Core.IO;
using MapStudio;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BFRES : BfresNodeBase, IFileFormat, IPropertyDisplay, IRenderableFile
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "BFRES" };
        public string[] Extension { get; set; } = new string[] { "*.bfres" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream) {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "FRES");
            }
        }

        public object PropertyDisplay => ResFile;

        public ResFile ResFile { get; set; }

        public List<FMDL> Models = new List<FMDL>();
        public List<STGenericTexture> Textures = new List<STGenericTexture>();
        public List<BfresSkeletalAnim> SkeletalAnimations = new List<BfresSkeletalAnim>();
        public List<BfresMaterialAnim> MaterialAnimations = new List<BfresMaterialAnim>();
        public List<BfresShapeAnim> ShapeAnimations = new List<BfresShapeAnim>();
        public List<BfresVisibilityAnim> VisibilityAnimations = new List<BfresVisibilityAnim>();
        public List<BfshaLibrary.BfshaFile> ShaderFiles = new List<BfshaLibrary.BfshaFile>();

        public GenericRenderer Renderer { get; set; }

        public override string Header
        {
            get { return ResFile.Name; }
            set { ResFile.Name = value; }
        }

        public EventHandler OnRenamed => (s, e) => {
            this.Header = (string)s;
        };

        public void Load(Stream stream) {
            Icon = "/Images/Bfres.png";
            ResFile = new ResFile(stream);
            ReloadWrappers(ResFile);

            Renderer = new BfresRender();
            ((BfresRender)Renderer).Load(this);

            Tag = this;
        }

        /// <summary>
        /// Updates the shader list stored in the file as embedded.
        /// </summary>
        public void UpdateExternalShaderFiles()
        {
            if (ShaderFiles.Count > 0)
                return;

            //Find and load any external shader binaries
            for (int i = 0; i < ResFile.ExternalFiles.Count; i++)
            {
                string fileName = ResFile.ExternalFiles.Keys.ToList()[i];
                if (fileName.EndsWith(".bfsha") && IsSwitchBinary(ResFile.ExternalFiles[i].Data))
                {
                    ShaderFiles.Add(new BfshaLibrary.BfshaFile(new MemoryStream(ResFile.ExternalFiles[i].Data)));
                }
            }
        }

        //Check if the file is a switch binary file
        private bool IsSwitchBinary(byte[] data)
        {
            using (var reader = new FileReader(data))
            {
                reader.ReadUInt32();
                return reader.ReadUInt32() == 0x20202020;
            }
        }

        public void Save(Stream stream) {
            SaveWrappers();
            ResFile.Save(stream);
        }

        //Apply wrapper linked data to the resfile instance
        private void SaveWrappers()
        {
            ResFile.Models.Clear();

            foreach (var model in Models)
            {
                var modelData = model.Model;
                ResFile.Models.Add(model.Name, modelData);

                modelData.Shapes.Clear();
                modelData.VertexBuffers.Clear();
                modelData.Materials.Clear();

                for (int i = 0; i < model.Meshes.Count; i++)
                {
                    modelData.Shapes.Add(model.Meshes[i].Name, ((FSHP)model.Meshes[i]).Shape);
                    modelData.VertexBuffers.Add(((FSHP)model.Meshes[i]).VertexBuffer);
                }
                for (int i = 0; i < model.Materials.Count; i++)
                    modelData.Materials.Add(model.Materials[i].Name, ((FMAT)model.Materials[i]).Material);
            }
        }

        private void ReloadWrappers(ResFile resFile) {
            var bntxFile = resFile.ExternalFiles.Values.FirstOrDefault(x => x.LoadedFileData as BntxFile != null) ;

            Children.Clear();
            if (resFile.Models.Count > 0)
                AddChild(new ModelFolder(this, resFile, resFile.Models));
            if (resFile.Textures.Count > 0)
                AddChild(new TextureFolder(this, resFile, resFile.Textures, bntxFile));
            if (resFile.SkeletalAnims.Count > 0)
                AddChild(new SkeletalAnimFolder(this, resFile, resFile.SkeletalAnims));
            if (resFile.TexSrtAnims.Count > 0)
                AddChild(new TextureSRTAnimFolder(this, resFile, resFile.TexSrtAnims));
            if (resFile.ColorAnims.Count > 0)
                AddChild(new ColorAnimFolder(this, resFile, resFile.ColorAnims));
            if (resFile.ShaderParamAnims.Count > 0)
                AddChild(new ShaderParamAnimFolder(this, resFile, resFile.ShaderParamAnims));
            if (resFile.TexPatternAnims.Count > 0)
                AddChild(new TexturePatternAnimFolder(this, resFile, resFile.TexPatternAnims));
            if (resFile.ShapeAnims.Count > 0)
                AddChild(new ShapeAnimFolder(this, resFile, resFile.ShapeAnims));
            if (resFile.BoneVisibilityAnims.Count > 0)
                AddChild(new BoneVisibilityAnimFolder(this, resFile, resFile.BoneVisibilityAnims));
            if (resFile.ExternalFiles.Count > 0)
                AddChild(new EmbeddedFolder(this, resFile, resFile.ExternalFiles));
        }
    }
}
