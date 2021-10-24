using BfresLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using GLFrameworkEngine;
using CafeStudio.UI;
using Toolbox.Core.ViewModels;

namespace BfresEditor
{
    public class FMAT : STGenericMaterial, IExportReplaceNode, IPropertyUI, IDragDropNode,
        IRenamableNode, IDeletableNode
    {
        /// <summary>
        /// The material section of the bfres.
        /// </summary>
        public Material Material { get; set; }

        public NodeBase ParentNode { get; set; }

        /// <summary>
        /// Thee model in which the data in this section is parented to.
        /// </summary>
        public Model ParentModel { get; set; }

        /// <summary>
        /// The file in which the data in this section is parented to.
        /// </summary>
        public BFRES ParentFile { get; set; }

        private string _icon = "/Images/Material.png";
        public string Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
            }
        }

        public bool IsVisible
        {
            get { return Material.Visible; }
            set { Material.Visible = value; }
        }

        public BfresMaterialAsset MaterialAsset { get; set; }

        public FMDL ParentBfresModel { get; set; }

        public string ShaderArchive { get; set; }
        public string ShaderModel { get; set; }

        public BfshaLibrary.ShaderModel GetShaderModel()
        {
            if (MaterialAsset is BfshaRenderer) {
                var bfshaAsset = (BfshaRenderer)MaterialAsset;
                return bfshaAsset.ShaderModel;
            }
            return null;
        }

        public BfshaLibrary.ResShaderProgram GetShaderProgram()
        {
            if (MaterialAsset is BfshaRenderer)
            {
                var model = GetShaderModel();

                var bfshaAsset = (BfshaRenderer)MaterialAsset;
                return bfshaAsset.ProgramPasses[bfshaAsset.ShaderIndex];
            }
            return null;
        }

        public override string Name
        {
            get { return Material.Name; }
            set { Material.Name = value; }
        }

        public CullMode CullState
        {
            get
            {
                if (CullFront && CullBack) return CullMode.Both;
                else if (CullBack) return CullMode.Back;
                else if (CullFront) return CullMode.Front;
                else
                    return CullMode.None;
            }
            set
            {
                CullBack = false;
                CullFront = false;

                if (value == CullMode.Both)
                {
                    CullBack = true;
                    CullFront = true;
                }
                else if (value == CullMode.Front)
                    CullFront = true;
                else if (value == CullMode.Back)
                    CullBack = true;
            }
        }

        public enum CullMode
        {
            None,
            Front,
            Back,
            Both,
        }

        public bool CullFront = false;
        public bool CullBack = true;

        public List<string> Samplers { get; set; }
        public Dictionary<string, string> AnimatedSamplers { get; set; }

        public Dictionary<string, ShaderParam> ShaderParams { get; set; }
        public Dictionary<string, ShaderParam> AnimatedParams { get; set; }
        public Dictionary<string, string> ShaderOptions { get; set; }

        public GLMaterialBlendState BlendState { get; set; } = new GLMaterialBlendState();

        public bool IsTransparent { get; set; }

        public void Renamed(string text) {
            this.Name = text;
        }

        public string GetRenameText() => this.Name;

        public void Deleted() {
            ParentBfresModel.Materials.Remove(this);
        }

        public Type GetTypeUI() => typeof(BfresMaterialEditor);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (BfresMaterialEditor)uiInstance;
            editor.LoadEditor(this);
        }

        private void Init()
        {
            Samplers = new List<string>();
            ShaderParams = new Dictionary<string, ShaderParam>();
            ShaderOptions = new Dictionary<string, string>();
            AnimatedSamplers = new Dictionary<string, string>();
            AnimatedParams = new Dictionary<string, ShaderParam>();
        }

        public FMAT(BFRES bfres, FMDL fmdl, Model model, Material material)
        {
            Init();
            ParentFile = bfres;
            Material = material;
            ParentModel = model;
            ParentBfresModel = fmdl;
            Reload(material);
        }

        public void Reload(Material material)
        {
            ShaderArchive = "";
            ShaderModel = "";
            ShaderParams.Clear();
            ShaderOptions.Clear();
            Samplers.Clear();
            TextureMaps.Clear();

            UpdateRenderState();

            if (material.ShaderAssign != null)
            {
                ShaderArchive = material.ShaderAssign.ShaderArchiveName;
                ShaderModel = material.ShaderAssign.ShadingModelName;
            }

            foreach (var param in material.ShaderParams)
                ShaderParams.Add(param.Key, param.Value);
            foreach (var option in material.ShaderAssign.ShaderOptions)
                ShaderOptions.Add(option.Key, option.Value);

            // if (ShaderParams.ContainsKey("gsys_i_color_ratio0"))
            //   ShaderParams["gsys_i_color_ratio0"].DataValue = 0.1f;

            for (int i = 0; i < material.TextureRefs.Count; i++)
            {
                string name = material.TextureRefs[i].Name;
                Sampler sampler = material.Samplers[i];
                var texSampler = material.Samplers[i].TexSampler;
                string samplerName = sampler.Name;
                string fragSampler = "";

                //Force frag shader sampler to be used 
                if (material.ShaderAssign.SamplerAssigns.ContainsValue(samplerName))
                    material.ShaderAssign.SamplerAssigns.TryGetKey(samplerName, out fragSampler);

                Samplers.Add(fragSampler);

                this.TextureMaps.Add(new TextureMap()
                {
                    Name = name,
                    Sampler = samplerName,
                    MagFilter = GXConverter.ConvertMagFilter(texSampler.MagFilter),
                    MinFilter = GXConverter.ConvertMinFilter(
                        texSampler.MipFilter,
                        texSampler.MinFilter),
                    Type = GetTextureType(fragSampler),
                    WrapU = GXConverter.ConvertWrapMode(texSampler.ClampX),
                    WrapV = GXConverter.ConvertWrapMode(texSampler.ClampY),
                    LODBias = texSampler.LodBias,
                    MaxLOD = texSampler.MaxLod,
                    MinLOD = texSampler.MinLod,
                });
            }
        }

        public void ReloadTextureMap(int index)
        {
            var texSampler = Material.Samplers[index].TexSampler;
            var texMap = this.TextureMaps[index];

            texMap.MagFilter = GXConverter.ConvertMagFilter(texSampler.MagFilter);
            texMap.MinFilter = GXConverter.ConvertMinFilter(
                       texSampler.MipFilter,
                       texSampler.MinFilter);
            texMap.WrapU = GXConverter.ConvertWrapMode(texSampler.ClampX);
            texMap.WrapV = GXConverter.ConvertWrapMode(texSampler.ClampY);
            texMap.LODBias = texSampler.LodBias;
            texMap.MaxLOD = texSampler.MaxLod;
            texMap.MinLOD = texSampler.MinLod;
        }

        public void UpdateRenderState()
        {
            GXConverter.ConvertRenderState(this, Material.RenderState);
            GXConverter.ConvertPolygonState(this, Material);

            if (this.BlendState.State == GLMaterialBlendState.BlendState.Translucent ||
                this.BlendState.State == GLMaterialBlendState.BlendState.Custom)
                Icon = "/Images/MaterialTrans.tif";
            if (this.BlendState.State == GLMaterialBlendState.BlendState.Mask)
                Icon = "/Images/MaterialMask.tif";
        }

        private STTextureType GetTextureType(string sampler)
        {
            switch (sampler)
            {
                case "_a0": return STTextureType.Diffuse;
                case "_n0": return STTextureType.Normal;
                case "_s0": return STTextureType.Specular;
                case "_e0": return STTextureType.Emission;
                default:
                    return STTextureType.None;
            }
        }

        public dynamic GetRenderInfo(string name, int index = 0)
        {
            if (Material.RenderInfos.ContainsKey(name))
            {
                if (Material.RenderInfos[name].Data == null)
                    return null;

                switch (Material.RenderInfos[name].Type)
                {
                    case RenderInfoType.Int32: return Material.RenderInfos[name].GetValueInt32s()[index];
                    case RenderInfoType.String:
                        if (Material.RenderInfos[name].GetValueStrings().Length > index)
                            return Material.RenderInfos[name].GetValueStrings()[index];
                        else
                            return null;
                    case RenderInfoType.Single: return Material.RenderInfos[name].GetValueSingles()[index];
                }
            }

            return null;
        }

        #region events

        public FileFilter[] ReplaceFilter => new FileFilter[]
        {
          new FileFilter(".bfmat", "Raw Binary Material"), 
          new FileFilter(".json", "Json (Readable Text)"),
        };

        public FileFilter[] ExportFilter => new FileFilter[]
        {
          new FileFilter(".bfmat", "Raw Binary Material"),
          new FileFilter(".json", "Json (Readable Text)"),
        };

        public void Replace(string fileName) {
            Material.Import(fileName, ParentFile.ResFile);
            Reload(Material);

            foreach (FSHP mesh in GetMappedMeshes())
                mesh.ReloadShader();
        }

        public void Export(string fileName) {
            Material.Export(fileName, ParentFile.ResFile);
        }

        #endregion

        public override List<STGenericMesh> GetMappedMeshes()
        {
            List<STGenericMesh> meshes = new List<STGenericMesh>();
            for (int i = 0; i < ParentBfresModel.Meshes.Count; i++)
            {
                if (((FSHP)ParentBfresModel.Meshes[i]).Material == this)
                    meshes.Add(ParentBfresModel.Meshes[i]);
            }
            return meshes;
        }

        public virtual void ResetAnimations() {
            AnimatedSamplers.Clear();
            AnimatedParams.Clear();
        }
    }
}
