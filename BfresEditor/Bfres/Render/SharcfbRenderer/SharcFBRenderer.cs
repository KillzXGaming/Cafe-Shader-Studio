using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapStudio;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.IO;
using GLFrameworkEngine;

namespace BfresEditor
{
    /// <summary>
    /// A sharcfb render class to help render shader binaries from a shader archive.
    /// This includes methods to help load some block data automatically.
    /// </summary>
    [Serializable]
    public class SharcFBRenderer : ShaderRenderBase
    {
        private ShaderProgram shaderProgram;

        /// <summary>
        /// Determines if the current material has a valid program.
        /// If the model fails to find a program in ReloadProgram, it will fail to load the shader.
        /// </summary>
        public override bool HasValidProgram => ShaderModel != null;

        /// <summary>
        /// Shader information from the decoded shader.
        /// This is used to store constants and source information.
        /// </summary>
        public override ShaderInfo GLShaderInfo { get; set; }

        public int VariationBaseIndex { get; set; } = -1;
        public int BinaryIndex => ShaderModel.GetBinaryIndex(VariationBaseIndex);

        public bool UpdateShader { get; set; }

        public override ShaderProgram Shader => shaderProgram;

        public virtual bool UseRenderer(string archive, string model, SHARCFB sharcFB)
        {
            return false;
        }

        public SHARCFB.ShaderProgram ShaderModel { get; set; }

        public SharcFBRenderer() { }

        public SharcFBRenderer(SHARCFB.ShaderProgram shaderModel) {
            ShaderModel = shaderModel;
        }

        /// <summary>
        /// Loads the material renderer for the first time.
        /// </summary>
        /// <returns></returns>
        public override void TryLoadShader(BFRES bfres, FMDL fmdl, FSHP mesh, BfresMeshAsset meshAsset)
        {
            var sharcfb = TryLoadShaderArchive(bfres,
                mesh.Material.ShaderArchive,
                mesh.Material.ShaderModel);

            if (sharcfb == null)
            {
                Console.WriteLine($"Failed to sharcfb! {mesh.Material.ShaderArchive}");
                return;
            }

            OnLoad(sharcfb, fmdl, mesh, meshAsset);
        }

        /// <summary>
        /// Called once when the renderer can be loaded from a given shader model and mesh.
        /// </summary>
        public void OnLoad(SHARCFB sharcfb, FMDL model, FSHP mesh, BfresMeshAsset meshAsset)
        {
            ShaderModel = sharcfb.Programs.FirstOrDefault(x => x.Name == mesh.Material.ShaderModel);
            if (ShaderModel == null)
            {
                Console.WriteLine($"Failed to find program! {mesh.Material.ShaderModel}");
                return;
            }

            //Assign some necessary data
            meshAsset.MaterialAsset = this;

            //Force reload from material editing
            mesh.ShaderReload += delegate
            {
                Console.WriteLine($"Reloading shader program {meshAsset.Name}");
                this.ReloadRenderState(meshAsset);
                this.ReloadProgram(meshAsset);
                mesh.HasValidShader = this.HasValidProgram;

                Console.WriteLine($"Program Validation: {this.HasValidProgram}");
                this.UpdateShader = true;
            };

            MaterialData = mesh.Material;
            ParentModel = model;
            //Load mesh function for loading the custom shader for the first time
            LoadMesh(meshAsset);
            ReloadRenderState(meshAsset);
            ReloadProgram(meshAsset);

            var gx2ShaderVertex = (GX2VertexShader)ShaderModel.GetGX2VertexShader(BinaryIndex);
            var bfresMaterial = (FMAT)this.MaterialData;

            //Remap the vertex layouts from shader model attributes
            Dictionary<string, int> attributeLocations = new Dictionary<string, int>();

            int location = 0;
            for (int i = 0; i < gx2ShaderVertex.Attributes.Count; i++)
            {
                var symbol = ShaderModel.AttributeVariables.symbols.FirstOrDefault(
                     x => x.Name == gx2ShaderVertex.Attributes[i].Name);

                if (symbol == null || symbol.flags[this.VariationBaseIndex] == 0)
                    continue;

                var attribVar = gx2ShaderVertex.Attributes[i];
                var arrayCount = Math.Max(1, attribVar.Count);
                var streamCount = attribVar.GetStreamCount();

                if (arrayCount > 1 || streamCount > 1)
                    throw new Exception("Multiple attribute streams and variable counts not supported!");

                attributeLocations.Add(symbol.SymbolName, location++);
            }

            meshAsset.UpdateVaoAttributes(attributeLocations);
        }

        /// <summary>
        /// Reloads the program passes to render onto.
        /// If the program pass list is empty, the material will not load.
        /// </summary>
        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var macro in ShaderModel.VariationMacroData.symbols)
            {
                if (mat.ShaderOptions.ContainsKey(macro.SymbolName))
                    options.Add(macro.Name, mat.ShaderOptions[macro.SymbolName]);
            }
            VariationBaseIndex = ShaderModel.GetVariationIndex(options);
        }

        /// <summary>
        /// Mesh loading info for loading additional data like hardcoded vertex attributes.
        /// </summary>
        public override void LoadMesh(BfresMeshAsset mesh)
        {

        }

        /// <summary>
        /// Reloads render info and state settings into the materials blend state for rendering.
        /// </summary>
        public override void ReloadRenderState(BfresMeshAsset mesh)
        {

        }

        /// <summary>
        /// The render loop to draw the material
        /// </summary>
        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            var bfresMaterial = (FMAT)this.MaterialData;
            var bfresMesh = (BfresMeshAsset)mesh;

            //Set the SRGB setting
            control.UseSRBFrameBuffer = UseSRGB;

            var programID = shader.program;

            CafeShaderDecoder.SetShaderConstants(shader, programID, bfresMaterial);

            //Set material raster state and texture samplers
            SetBlendState(bfresMaterial);
            SetTextureUniforms(control, shader, MaterialData);
            SetRenderState(bfresMaterial);

            var pixelShader = ShaderModel.GetGX2PixelShader(this.BinaryIndex);
            var vertexShader = ShaderModel.GetGX2VertexShader(this.BinaryIndex);

            int bindings = 2;
            for (int i = 0; i < ShaderModel.UniformBlocks.symbols.Count; i++)
            {
                string name = ShaderModel.UniformBlocks.symbols[i].Name;

                //Get the gx2 uniform block and find it's offset info
                var gx2VertexUniformBlock = vertexShader.UniformBlocks.FirstOrDefault(x => x.Name == name);
                var gx2PixelUniformBlock = pixelShader.UniformBlocks.FirstOrDefault(x => x.Name == name);

                int vertLocation = -1;
                int fragLocation = -1;

                if (gx2VertexUniformBlock != null)
                    vertLocation = (int)gx2VertexUniformBlock.Offset;
                if (gx2PixelUniformBlock != null)
                    fragLocation = (int)gx2PixelUniformBlock.Offset;

                //Block unused for this program so skip it
                if (fragLocation == -1 && vertLocation == -1)
                    continue;

                var shaderBlock = GetBlock(name);
                LoadUniformBlock(control, shader, i, shaderBlock, name, mesh);
                RenderBlock(shaderBlock, programID, vertLocation, fragLocation, bindings++);
            }
        }

        /// <summary>
        /// Searches for the shader archive file in external files, parent archive, and the global shader cache.
        /// </summary>
        /// <returns></returns>
        public virtual SHARCFB TryLoadShaderArchive(BFRES bfres, string shaderFile, string shaderModel)
        {
            //Check external files.
            bfres.UpdateExternalShaderFiles();
            foreach (var file in bfres.ShaderFiles) {
                if (file is SHARCFB && ((SHARCFB)file).Name.Contains(shaderFile)) {
                    return (SHARCFB)file;
                }
            }

            //Check global shader cache
            foreach (var file in GlobalShaderCache.ShaderFiles.Values)
            {
                if (file is SHARCFB)
                {
                    if (((SHARCFB)file).Name.Contains(shaderFile)) {
                        return (SHARCFB)file;
                    }
                }
            }

            //Check external archives parenting the file.
            var archiveFile = bfres.FileInfo.ParentArchive;
            if (archiveFile == null)
                return null;

            foreach (var file in archiveFile.Files) {
                if (!file.FileName.EndsWith(".sharcfb"))
                    continue;

                if (file.FileName == shaderFile || HasFileName(file.FileData, shaderFile)) {
                    if (file.FileFormat == null)
                        file.FileFormat = file.OpenFile();

                    return (SHARCFB)file.FileFormat;
                }
            }
            return null;
        }

        private bool HasFileName(Stream stream, string fileName)
        {
            using (var reader = new Toolbox.Core.IO.FileReader(stream, true))
            {
                reader.ReadSignature(4, "BAHS");

                reader.SeekBegin(20);
                uint nameLength = reader.ReadUInt32();
                string name = reader.ReadString((int)nameLength, true);
                return name == fileName;
            }
        }

        /// <summary>
        /// Checks if the program needs to be reloaded from a change in shader pass.
        /// </summary>
        public override void CheckProgram(GLContext control, BfresMeshAsset mesh, int pass = 0)
        {
            if (ShaderModel == null) {
                return;
            }

            if (Shader == null || UpdateShader) {
                ReloadGLSLShaderFile();
            }
        }

        /// <summary>
        /// Reloads the glsl shader file from the shader cache or saves a translated one if does not exist.
        /// </summary>
        public void ReloadGLSLShaderFile()
        {
            var vertexData = ShaderModel.ParentFile.Binaries[BinaryIndex];
            var pixelData = ShaderModel.ParentFile.Binaries[BinaryIndex + 1];

            GLShaderInfo = CafeShaderDecoder.LoadShaderProgram(vertexData.DataBytes, pixelData.DataBytes);
            shaderProgram = GLShaderInfo.Program;

            UpdateShader = false;
        }

        public virtual void LoadUniformBlock(GLContext control, ShaderProgram shader, int index, UniformBlock block, string name, GenericPickableMesh mesh)
        {

        }

        public virtual void SetShapeBlock(BfresMeshAsset mesh, Matrix4 transform, UniformBlock block)
        {
            int numSkinning = (int)mesh.SkinCount;
            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.AddInt(numSkinning);
        }

        public void SetBoneMatrixBlock(STSkeleton skeleton, bool useInverse, UniformBlock block, int maxTransforms = 64)
        {
            block.Buffer.Clear();

            //Fixed buffer of max amount of transform values
            for (int i = 0; i < maxTransforms; i++)
            {
                Matrix4 value = Matrix4.Zero;

                //Set the inverse matrix and load the matrix data into 3 vec4s
                if (i < skeleton.Bones.Count)
                {
                    if (useInverse) //Use inverse transforms for smooth skinning
                        value = skeleton.Bones[i].Inverse * skeleton.Bones[i].Transform;
                    else
                        value = skeleton.Bones[i].Transform;
                }

                block.Add(value.Column0);
                block.Add(value.Column1);
                block.Add(value.Column2);
            }
        }

        public void SetMaterialBlock(FMAT mat, UniformBlock block)
        {
            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);

                var matBlock = ShaderModel.UniformBlocks.symbols.FirstOrDefault(
                         x => x.Name == "Mat");

                writer.Write(matBlock.DefaultValue);
                foreach (var param in ShaderModel.UniformVariables.symbols)
                {
                    var uniformName = param.SymbolName;

                    writer.SeekBegin(param.Offset);
                    if (mat.ShaderParams.ContainsKey(uniformName))
                    {
                        var matParam = mat.ShaderParams[uniformName];
                        if (mat.AnimatedParams.ContainsKey(uniformName))
                            matParam = mat.AnimatedParams[uniformName];

                        if (matParam.Type == BfresLibrary.ShaderParamType.TexSrtEx) //Texture matrix (texmtx)
                            writer.Write(CalculateSRT3x4((BfresLibrary.TexSrt)matParam.DataValue));
                        else if (matParam.Type == BfresLibrary.ShaderParamType.TexSrt)
                            writer.Write(CalculateSRT2x3((BfresLibrary.TexSrt)matParam.DataValue));
                        else if (matParam.DataValue is BfresLibrary.Srt2D) //Indirect SRT (ind_texmtx)
                            writer.Write(CalculateSRT((BfresLibrary.Srt2D)matParam.DataValue));
                        else if (matParam.DataValue is float)
                            writer.Write((float)matParam.DataValue);
                        else if (matParam.DataValue is float[])
                            writer.Write((float[])matParam.DataValue);
                        else if (matParam.DataValue is int[])
                            writer.Write((int[])matParam.DataValue);
                        else if (matParam.DataValue is uint[])
                            writer.Write((uint[])matParam.DataValue);
                        else if (matParam.DataValue is int)
                            writer.Write((int)matParam.DataValue);
                        else if (matParam.DataValue is uint)
                            writer.Write((uint)matParam.DataValue);
                        else
                            throw new Exception($"Unsupported render type! {matParam.Type}");
                    }
                }
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        public void RenderBlock(UniformBlock block, int programID, int vertexLocation, int fragmentLocation, int bindings)
        {
            if (vertexLocation != -1)
                block.RenderBuffer(programID, $"vp_{vertexLocation}", bindings);

            if (fragmentLocation != -1)
                block.RenderBuffer(programID, $"fp_{fragmentLocation}", bindings);
        }


        public int GetSamplerLocation(string fragSampler)
        {
            var sharcSymbol = ShaderModel.SamplerVariables.symbols.FirstOrDefault(x => x.SymbolName == fragSampler);
            if (sharcSymbol == null)
                return -1;

            var pixelShader = ShaderModel.GetGX2PixelShader(BinaryIndex);
            var gx2Sampler = pixelShader.Samplers.FirstOrDefault(x => x.Name == sharcSymbol.Name);
            if (gx2Sampler == null)
                return -1;

          return (int)gx2Sampler.Location;
        }

        public void SetSampler(ShaderProgram shader, int location, ref int slot)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            shader.SetInt(ConvertSamplerName(location), slot++);

            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            shader.SetInt(ConvertSamplerFetchName(location), slot++);
        }

        public static string ConvertSamplerName(int index) {
            return $"SPIRV_Cross_CombinedTEXTURE_{index}SAMPLER_{index}";
        }
        public static string ConvertSamplerFetchName(int index) {
            return $"SPIRV_Cross_CombinedTEXTURE_{index}SPIRV_Cross_DummySampler";
        }

        public UniformBlock GetBlock(string name)
        {
            if (!UniformBlocks.ContainsKey(name))
                UniformBlocks.Add(name, new UniformBlock());

            UniformBlocks[name].Buffer.Clear();
            return UniformBlocks[name];
        }

        private float[] CalculateSRT2x3(BfresLibrary.TexSrt texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);
            float scalingXC = scaling.X * cosR;
            float scalingXS = scaling.X * sinR;
            float scalingYC = scaling.Y * cosR;
            float scalingYS = scaling.Y * sinR;

            switch (texSrt.Mode)
            {
                default:
                case BfresLibrary.TexSrtMode.ModeMaya:
                    return new float[8]
                    {
                scalingXC, -scalingYS,
                scalingXS, scalingYC,
                -0.5f * (scalingXC + scalingXS - scaling.X) - scaling.X * translate.X, -0.5f * (scalingYC - scalingYS + scaling.Y) + scaling.Y * translate.Y + 1.0f,
                0.0f, 0.0f,
                    };
                case BfresLibrary.TexSrtMode.Mode3dsMax:
                    return new float[8]
                    {
                scalingXC, -scalingYS,
                scalingXS, scalingYC,
                -scalingXC * (translate.X + 0.5f) + scalingXS * (translate.Y - 0.5f) + 0.5f, scalingYS * (translate.X + 0.5f) + scalingYC * (translate.Y - 0.5f) + 0.5f,
                0.0f, 0.0f
                    };
                case BfresLibrary.TexSrtMode.ModeSoftimage:
                    return new float[8]
                    {
                scalingXC, scalingYS,
                -scalingXS, scalingYC,
                scalingXS - scalingXC * translate.X - scalingXS * translate.Y, -scalingYC - scalingYS * translate.X + scalingYC * translate.Y + 1.0f,
                0.0f, 0.0f,
                    };
            }
        }

        private float[] CalculateSRT3x4(BfresLibrary.TexSrt texSrt)
        {
            var m = CalculateSRT2x3(texSrt);
            return new float[12]
            {
                m[0], m[2], m[4], 0.0f,
                m[1], m[3], m[5], 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
            };
        }

        private float[] CalculateSRT(BfresLibrary.Srt2D texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);

            return new float[8]
            {
                scaling.X * cosR, scaling.X * sinR,
                -scaling.Y * sinR, scaling.Y * cosR,
                translate.X, translate.Y,
                0.0f, 0.0f
            };
        }
    }
}
