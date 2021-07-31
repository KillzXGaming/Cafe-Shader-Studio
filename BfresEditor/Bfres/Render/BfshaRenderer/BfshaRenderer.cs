using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.IO;
using GLFrameworkEngine;

namespace BfresEditor
{
    //Note to developers
    //To create a custom renderer, it needs a few things.

    //First an override to ReloadProgram(). This determines what shader program to use in the shader.
    //These are determined by the static and dynamic option choices in a shader model (ShaderModel).
    //Generally you can handle most of these options automatically from loading the shader options from the bfres material.
    //Dynamic options you can directly add them all from the shader model and adjust what is necessary
    //One very important option value you may need to adjust is skin count which varies between games on what type of option it is and what is called.

    //The next step is to optionally do a ReloadRenderState() function. Here you set render state information onto the mesh that may be used for the shader.
    //Currently used for things like setting draw priority values or polygon offset handling (all of this is a per game thing)

    //Another optional step is the LoadMesh() function. Here you can add addtional hardcoded attributes from mesh.Attributes
    //Check KSA/KSARender for a look at how it is used.

    //Next is the Render loop. If you plan to override this, you must include a base.Render() to draw the main bfsha render loop
    //Currently used to check for initialized textures hardcoded in engine, but that can be done elsewhere where the textures are loaded.

    //Lastly there is the uniform block loading from LoadUniformBlock(). 
    //Here you switch between the block names and fill the block with actual data.
    //Keep in mind the data is cleared each loop to be filled back up.
    //However there are plans to keep it cached and update only when necessary.

    /// <summary>
    /// A bfsha render class to help render shader binaries from a shader archive.
    /// This includes methods to help load some block data automatically.
    /// </summary>
    [Serializable]
    public class BfshaRenderer : ShaderRenderBase
    {
        private ShaderProgram shaderProgram;

        /// <summary>
        /// A list of programs used for multiple passes.
        /// This value is generally for dynamic options with mutliple pass programs
        /// </summary>
        public List<BfshaLibrary.ResShaderProgram> ProgramPasses = new List<BfshaLibrary.ResShaderProgram>();

        /// <summary>
        /// Determines to enable SRGB or not when drawn to the final framebuffer.
        /// </summary>
        public override bool UseSRGB { get; } = true;

        /// <summary>
        /// Determines if the current material has a valid program.
        /// If the model fails to find a program in ReloadProgram, it will fail to load the shader.
        /// </summary>
        public override bool HasValidProgram => ProgramPasses.Count > 0;

        /// <summary>
        /// Determines to reload the glsl shader file or not.
        /// </summary>
        private bool UpdateShader = false;

        /// <summary>
        /// The opengl shader used to render.
        /// </summary>
        public override ShaderProgram Shader => shaderProgram;

        /// <summary>
        /// Determines when to use this renderer for the given material.
        /// This is typically done from the shader archive or shader model name.
        /// The material can also be used for shader specific render information to check.
        /// </summary>
        /// <returns></returns>
        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return false;
        }

        /// <summary>
        /// A list of blocks which are cached to not update after the next frame.
        /// </summary>
        public List<string> BlocksToCache = new List<string>();

        /// <summary>
        /// The active shader model used for shader information.
        /// </summary>
        public BfshaLibrary.ShaderModel ShaderModel { get; set; }

        public bool IsSwitch => ShaderModel.BnshFileStream != null;

        public BfshaRenderer() { }

        public BfshaRenderer(BfshaLibrary.ShaderModel shaderModel)
        {
            ShaderModel = shaderModel;
        }

        /// <summary>
        /// Loads the material renderer for the first time.
        /// </summary>
        /// <returns></returns>
        public override void TryLoadShader(BFRES bfres, FMDL fmdl, FSHP mesh, BfresMeshAsset meshAsset)
        {
            var bfsha = TryLoadShaderArchive(bfres,
                mesh.Material.ShaderArchive,
                mesh.Material.ShaderModel);

            if (bfsha == null)
                return;

            var shaderModel = bfsha.ShaderModels.Values.FirstOrDefault(x => x.Name == mesh.Material.ShaderModel);
            if (shaderModel != null)
                OnLoad(shaderModel, fmdl, mesh, meshAsset);
        }

        /// <summary>
        /// Called once when the renderer can be loaded from a given shader model and mesh.
        /// </summary>
        public void OnLoad(BfshaLibrary.ShaderModel shaderModel, FMDL model, FSHP mesh, BfresMeshAsset meshAsset)
        {
            var shapeBlock = shaderModel.UniformBlocks.Values.FirstOrDefault(x =>
            x.Type == BfshaLibrary.UniformBlock.BlockType.Shape);

            //Models may update the shape block outside the shader if the shape block is unused so update mesh matrix manually
            if (shapeBlock.Size == 0 && mesh.VertexSkinCount == 0)
            {
                mesh.UpdateVertexBuffer(true);
                meshAsset.UpdateVertexBuffer();
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

            ShaderModel = shaderModel;
            MaterialData = mesh.Material;
            ParentModel = model;
            //Load mesh function for loading the custom shader for the first time
            LoadMesh(meshAsset);
            ReloadRenderState(meshAsset);
            ReloadProgram(meshAsset);

            var bfresMaterial = (FMAT)this.MaterialData;

            //Remap the vertex layouts from shader model attributes
            if (!IsSwitch)
            {
                //GX2 shaders can be directly mapped via string and location searches
                Dictionary<string, string> attributeLocations = new Dictionary<string, string>();
                for (int i = 0; i < shaderModel.Attributes.Count; i++)
                {
                    string key = shaderModel.Attributes.GetKey(i);
                    attributeLocations.Add(key, $"{key}_0_0");
                }
                meshAsset.UpdateVaoAttributes(attributeLocations);
            }
            else
            {
                Dictionary<string, int> attributeLocations = new Dictionary<string, int>();
                for (int i = 0; i < shaderModel.Attributes.Count; i++)
                {
                    string key = shaderModel.Attributes.GetKey(i);
                    int location = shaderModel.Attributes[i].Location;
                    attributeLocations.Add(key, location);
                }
                meshAsset.UpdateVaoAttributes(attributeLocations);
            }
        }

        /// <summary>
        /// Reloads the program passes to render onto.
        /// If the program pass list is empty, the material will not load and display a red error handling material.
        /// </summary>
        public override void ReloadProgram(BfresMeshAsset mesh)
        {

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

            //Set constants saved from shader code to the first uniform block of each stage
            if (IsSwitch)
            {
                LoadVertexShaderConstantBlock(programID);
                LoadPixelShaderConstantBlock(programID);
            }

            if (!IsSwitch)
                CafeShaderDecoder.SetShaderConstants(shader, programID, bfresMaterial);

            //Set in tool selection coloring
            shader.SetVector4("extraBlock.selectionColor", new Vector4(0));
            if (bfresMesh.IsSelected)
                shader.SetVector4("extraBlock.selectionColor", new Vector4(1, 1, 0.5f, 0.010f));

            //Set material raster state and texture samplers
            SetBlendState(bfresMaterial);
            SetTextureUniforms(control, shader, MaterialData);
            SetRenderState(bfresMaterial);

            int binding = IsSwitch ? 2 : 0;
            for (int i = 0; i < ShaderModel.UniformBlocks.Count; i++)
            {
                string name = ShaderModel.UniformBlocks.GetKey(i);
                var uniformBlock = ShaderModel.UniformBlocks[i];

                var locationInfo = ProgramPasses[this.ShaderIndex].UniformBlockLocations[i];
                int fragLocation = locationInfo.FragmentLocation;
                int vertLocation = locationInfo.VertexLocation;

                //Block unused for this program so skip it
                if (fragLocation == -1 && vertLocation == -1)
                    continue;

                var shaderBlock = GetBlock(name + "vs", false);

                //If a block is not cached, update it in the render loop.
                if (!BlocksToCache.Contains(name)) {
                    shaderBlock.Buffer.Clear();
                    LoadUniformBlock(control, shader, i, shaderBlock, name, mesh);
                }

                RenderBlock(shaderBlock, programID, vertLocation, fragLocation, binding++);
            }
        }

        /// <summary>
        /// Loads a given uniform block. Switch between the name to determine what type of block data to load.
        /// Fill the UniformBlock type with data.
        /// </summary>
        public virtual void LoadUniformBlock(GLContext control, ShaderProgram shader, int index,
            UniformBlock block, string name, GenericPickableMesh mesh)
        {
         
        }   

        /// <summary>
        /// A helper method to auto map commonly used render info settings to options.
        /// Not all games use the same render info settings so this only works for certain games!
        /// </summary>
        public virtual void LoadRenderStateOptions(Dictionary<string, string> options, FMAT mat) {
            ShaderOptionHelper.LoadRenderStateOptions(options, mat);
        }

        /// <summary>
        /// Fills the first constant block with constants from the shader code.
        /// This method must be called during render if the shader requires constants.
        /// </summary>
        public void LoadVertexShaderConstantBlock(int programID)
        {
            if (GLShaderInfo.VertexConstants == null)
                return;

            var firstBlock = GetBlock("vp_c1");
            firstBlock.Add(GLShaderInfo.VertexConstants);
            firstBlock.RenderBuffer(programID, "vp_c1", 0);
        }

        /// <summary>
        /// Fills the first constant block with constants from the shader code.
        /// This method must be called during render if the shader requires constants.
        /// </summary>
        public void LoadPixelShaderConstantBlock(int programID)
        {
            if (GLShaderInfo.PixelConstants == null)
                return;

            var firstBlock = GetBlock("fp_c1");
            firstBlock.Add(GLShaderInfo.PixelConstants);
            firstBlock.RenderBuffer(programID, "fp_c1", 1);
        }

        /// <summary>
        /// Searches for the shader archive file in external files, parent archive, and the global shader cache.
        /// </summary>
        /// <returns></returns>
        public virtual BfshaLibrary.BfshaFile TryLoadShaderArchive(BFRES bfres, string shaderFile, string shaderModel)
        {
            //Check external files.
            bfres.UpdateExternalShaderFiles();
            foreach (var file in bfres.ShaderFiles) {
                if (file is BfshaLibrary.BfshaFile && ((BfshaLibrary.BfshaFile)file).Name.Contains(shaderFile)) {
                    return (BfshaLibrary.BfshaFile)file;
                }
            }

            //Check global shader cache
            foreach (var file in GlobalShaderCache.ShaderFiles.Values)
            {
                if (file is BfshaLibrary.BfshaFile) {
                    if (((BfshaLibrary.BfshaFile)file).Name.Contains(shaderFile)) {
                        return (BfshaLibrary.BfshaFile)file;
                    }
                }
            }

            //Check external archives parenting the file.
            var archiveFile = bfres.FileInfo.ParentArchive;
            if (archiveFile == null)
                return null;

            foreach (var file in archiveFile.Files) {
                if (file.FileName.Contains(shaderFile)) {
                    if (file.FileFormat == null)
                        file.FileFormat = file.OpenFile();

                    return ((BFSHA)file.FileFormat).BfshaFile;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the program needs to be reloaded from a change in shader pass.
        /// </summary>
        public override void CheckProgram(GLContext control, BfresMeshAsset mesh, int pass = 0)
        {
            if (ProgramPasses.Count == 0) {
                return;
            }

            ShaderIndex = pass;
            if (GLShaders[pass] == null || UpdateShader) {
                ReloadGLSLShaderFile(ProgramPasses[pass]);
            }
            shaderProgram = GLShaders[pass].Program;
        }

        /// <summary>
        /// Reloads the glsl shader file from the shader cache or saves a translated one if does not exist.
        /// </summary>
        public void ReloadGLSLShaderFile(BfshaLibrary.ResShaderProgram program) {
            if (IsSwitch)
                DecodeSwitchBinary(program);
            else
                DecodeWiiUBinary(program);

            UpdateShader = false;

            var matBlock = ShaderModel.UniformBlocks.Values.FirstOrDefault(x => x.Type == BfshaLibrary.UniformBlock.BlockType.Material);
            if (matBlock != null && GLShaderInfo != null)
            {
                var locationInfo = ProgramPasses[this.ShaderIndex].UniformBlockLocations[matBlock.Index];
                string blockNameFSH = IsSwitch ? $"fp_c{locationInfo.FragmentLocation + 3}_data" : $"CBUFFER_{locationInfo.FragmentLocation}.values";
                string blockNameVSH = IsSwitch ? $"vp_c{locationInfo.VertexLocation + 3}_data" : $"CBUFFER_{locationInfo.VertexLocation}.values";

                GLShaderInfo.CreateUsedUniformListVertex(matBlock, blockNameVSH);
                GLShaderInfo.CreateUsedUniformListPixel(matBlock, blockNameFSH);
            }
        }

        private void DecodeSwitchBinary(BfshaLibrary.ResShaderProgram program)
        {
            GLShaders[ShaderIndex] = TegraShaderDecoder.LoadShaderProgram(ShaderModel, ShaderModel.GetShaderVariation(program));
            shaderProgram = GLShaderInfo.Program;
        }

        private void DecodeWiiUBinary(BfshaLibrary.ResShaderProgram program)
        {
            var vertexShader = BfshaGX2ShaderHelper.CreateVertexShader(ShaderModel, program);
            var pixelShader = BfshaGX2ShaderHelper.CreatePixelShader(ShaderModel, program);

            GLShaders[ShaderIndex] = CafeShaderDecoder.LoadShaderProgram(vertexShader, pixelShader);
            shaderProgram = GLShaderInfo.Program;
        }

        /// <summary>
        /// A helper method to set a common shape block layout.
        /// Note not all games use the same shape block data!
        /// </summary>
        public virtual void SetShapeBlock(BfresMeshAsset mesh, Matrix4 transform, UniformBlock block)
        {
            int numSkinning = (int)mesh.SkinCount;

            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.AddInt(numSkinning);
        }

        /// <summary>
        /// A helper method to set a common skeleton bone block layout.
        /// Note not all games use the same skeleton bone block data!
        /// </summary>
        public virtual void SetBoneMatrixBlock(STSkeleton skeleton, bool useInverse, UniformBlock block, int maxTransforms = 170)
        {
            block.Buffer.Clear();

            //Fixed buffer of max amount of transform values
            for (int i = 0; i < maxTransforms; i++)
            {
                Matrix4 value = Matrix4.Zero;

                //Set the inverse matrix and load the matrix data into 3 vec4s
                if (i < skeleton.Bones.Count)
                {
                    //Check if the bone is smooth skinning aswell for accuracy purposes.
                    if (useInverse || ((BfresBone)skeleton.Bones[i]).UseSmoothMatrix) //Use inverse transforms for smooth skinning
                        value = skeleton.Bones[i].Inverse * skeleton.Bones[i].Transform;
                    else
                        value = skeleton.Bones[i].Transform;
                }

                block.Add(value.Column0);
                block.Add(value.Column1);
                block.Add(value.Column2);
            }
        }

        /// <summary>
        /// A helper method to set a material option block layout.
        /// </summary>
        public virtual void SetMaterialOptionsBlock(FMAT mat, UniformBlock block)
        {
            var uniformBlock = ShaderModel.UniformBlocks.Values.FirstOrDefault(
                x => x.Type == (BfshaLibrary.UniformBlock.BlockType)4);

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);

                int index = 0;
                foreach (var param in uniformBlock.Uniforms.Values)
                {
                    var uniformName = uniformBlock.Uniforms.GetKey(index++);

                    writer.SeekBegin(param.Offset - 1);
                    if (mat.ShaderOptions.ContainsKey(uniformName))
                    {
                        var option = mat.ShaderOptions[uniformName];
                        int value = int.Parse(option);
                        writer.Write(value);
                    }
                }
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        /// <summary>
        /// A helper method to set a material parameter block layout.
        /// </summary>
        public virtual void SetMaterialBlock(FMAT mat, UniformBlock block)
        {
            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                var matBlock = ShaderModel.UniformBlocks.Values.FirstOrDefault(x =>
                    x.Type == BfshaLibrary.UniformBlock.BlockType.Material);

                int index = 0;
                foreach (var param in matBlock.Uniforms.Values)
                {
                    var uniformName = matBlock.Uniforms.GetKey(index++);

                    writer.SeekBegin(param.Offset - 1);
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

        public override void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            int id = 1;

            //Load bindless textures first (textures can be binded without locations set in the program)
            LoadBindlessTextures(control, shader, ref id);

            //Go through all the shader samplers
            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                var locationInfo = ProgramPasses[ShaderIndex].SamplerLocations[i];
                //Currently only using the vertex and fragment stages
                if (locationInfo.VertexLocation == -1 && locationInfo.FragmentLocation == -1)
                    continue;

                string sampler = ShaderModel.Samplers.GetKey(i);
                var textureIndex = -1;
                //Sampler assign has a key list of fragment shader samplers, value list of bfres material samplers
                if (bfresMaterial.Material.ShaderAssign.SamplerAssigns.ContainsKey(sampler))
                {
                    //Get the resource sampler
                    //Important to note, fragment samplers are unique while material samplers can be the same
                    //So we need to lookup which material sampler the current fragment sampler uses.
                    string resSampler = bfresMaterial.Material.ShaderAssign.SamplerAssigns[sampler].String;
                    //Find a texture using the sampler
                    textureIndex = bfresMaterial.TextureMaps.FindIndex(x => x.Sampler == resSampler);
                }

                //Cannot find the texture so try loading it from an external source
                if (textureIndex == -1)
                {
                    //Get external textures (ie shadow maps, cubemaps, etc)
                    var texture = GetExternalTexture(control, sampler);
                    if (texture != null)
                    {
                        GL.ActiveTexture(TextureUnit.Texture0 + id);
                        texture.Bind();
                        SetTexture(shader, locationInfo.VertexLocation, locationInfo.FragmentLocation, ref id);
                    }
                    continue;
                }

                //Get the current material texture map
                var texMap = bfresMaterial.TextureMaps[textureIndex];

                var name = texMap.Name;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(texMap.Sampler))
                    name = bfresMaterial.AnimatedSamplers[texMap.Sampler];

                BindTexture(shader, GetTextures(), texMap, name, id);
                SetTexture(shader, locationInfo.VertexLocation, locationInfo.FragmentLocation, ref id);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public virtual GLTexture GetExternalTexture(GLContext control, string sampler)
        {
            return null;
        }

        public virtual void LoadBindlessTextures(GLContext control, ShaderProgram shader, ref int id)
        {

        }

        //Sets the texture uniform from the locations given.
        private void SetTexture(ShaderProgram shader, int vertexLocation, int fragmentLocation, ref int id)
        {
            if (vertexLocation != -1)
                shader.SetInt(ConvertSamplerID(vertexLocation, true), id);
            if (fragmentLocation != -1)
                shader.SetInt(ConvertSamplerID(fragmentLocation, false), id);

            //Only increase the slot once as each stage share slots.
            id++;
        }

        private void RenderBlock(UniformBlock block, int programID, int vertexLocation, int fragmentLocation, int binding)
        {
            if (vertexLocation != -1)
                block.RenderBuffer(programID, IsSwitch ? $"vp_c{vertexLocation + 3}" : $"vp_{vertexLocation}", binding);

            if (fragmentLocation != -1)
                block.RenderBuffer(programID, IsSwitch ? $"fp_c{fragmentLocation + 3}" : $"fp_{fragmentLocation}", binding);
        }

        private UniformBlock GetBlock(string name, bool reset = true)
        {
            if (!UniformBlocks.ContainsKey(name))  {
                UniformBlocks.Add(name, new UniformBlock());
            }

            if (reset)
                UniformBlocks[name].Buffer.Clear();
            return UniformBlocks[name];
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

        public string ConvertSamplerID(int id, bool vertexShader = false)
        {
            if (IsSwitch)
            {
                if (vertexShader)
                    return "vp_tex_tcb_" + ((id * 2) + 8).ToString("X1");
                else
                    return "fp_tex_tcb_" + ((id * 2) + 8).ToString("X1");
            }
            else
            {
                return $"SPIRV_Cross_CombinedTEXTURE_{id}SAMPLER_{id}";
            }
        }

        public override void Dispose()
        {
            foreach (var block in UniformBlocks.Values)
                block.Dispose();

            UniformBlocks.Clear();
        }
    }
}
