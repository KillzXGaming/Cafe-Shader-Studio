using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using MapStudio.Rendering;
using Toolbox.Core;
using GLFrameworkEngine;
using System.IO;
using BfresEditor.Properties;

namespace BfresEditor
{
    public class RedCarpetNXRender : BfshaRenderer
    {
        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return archive == "RenderMaterial" || 
                archive == "RenderMaterialAlphaMask" ||
                archive == "RenderMaterialGrass" ||
                archive == "RenderMaterialCloud" ||
                archive == "RenderMaterialCrystal" ||
                archive == "RenderMaterialEcho" ||
                archive == "RenderMaterialEmission" ||
                archive == "RenderMaterialGodRay" ||
                archive == "RenderMaterialIndirect" ||
                archive == "RenderMaterialMirror" ||
                archive == "RenderMaterialNoiseSpecular" ||
                archive == "RenderMaterialMultiTex";
        }

        public RedCarpetNXRender() { }

        public RedCarpetNXRender(BfshaLibrary.ShaderModel shaderModel) : base(shaderModel)
        {

        }

        public override BfshaLibrary.BfshaFile TryLoadShaderArchive(BFRES bfres, string shaderFile, string shaderModel)
        {
            return SM3DWShaderLoader.LoadShader(shaderFile);
        }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            if (mesh.Shape.Material.ShaderArchive == "RenderMaterialAlphaMask") {
                mesh.Shape.Material.BlendState.State = GLMaterialBlendState.BlendState.Mask;
            }
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            ProgramPasses.Clear();

            var mat = mesh.Shape.Material;

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in mat.Material.ShaderAssign.ShaderOptions)
                options.Add(op.Key, op.Value);

            //Update option from render state
            this.LoadRenderStateOptions(options, mat);

            //Dynamic options.
            options["cSkinWeightNum"] = mesh.Shape.VertexSkinCount.ToString();
            options.Add("system_id", "0");

            int programIndex = ShaderModel.GetProgramIndex(options);
            if (programIndex == -1)
                return;

            this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
        }

        static GLTexture NoiseTexture;
        static GLTexture HeightTexture;
        static GLTexture AlphaProjTexture;
        static GLTexture MultiTextureMask;
        static GLTexture IndirectTexture;
        static GLTexture AlbedoLayerTexture;

        static void InitTextures()
        {
            SM3DWCubemapLoader.LoadCubemap();

            NoiseTexture = GLTexture2D.FromBitmap(Resources.white);
            AlbedoLayerTexture = GLTexture2D.FromBitmap(Resources.white);
            HeightTexture = GLTexture2D.FromBitmap(Resources.black);
            AlphaProjTexture = GLTexture2D.FromBitmap(Resources.black);
            MultiTextureMask = GLTexture2D.FromBitmap(Resources.black);
            IndirectTexture = GLTexture2D.FromBitmap(Resources.black);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (IndirectTexture == null)
                InitTextures();

            control.ScreenBuffer.SetDrawBuffers(
             DrawBuffersEnum.None, DrawBuffersEnum.None, DrawBuffersEnum.None, DrawBuffersEnum.ColorAttachment0);

            base.Render(control, shader, mesh);
        }

        public override void LoadUniformBlock(GLContext control, ShaderProgram shader, UniformBlock block, string name, GenericPickableMesh mesh)
        {
            var bfresMaterial = (FMAT)this.MaterialData;
            var bfresMesh = (BfresMeshAsset)mesh;
            var meshBone = ParentModel.Skeleton.Bones[bfresMesh.BoneIndex];

            switch (name)
            {
                case "cMdlEnvView":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "cEchoBlockUniform":
                    break;
                case "cMiiFaceUniform":
                    break;
                case "cModelAdditionalInfo":
                    SetModelUniforms(block);
                    break;
                case "cMat":
                    SetMaterialBlock(bfresMaterial, block);
                    break;
                case "cWater":
                    break;
                case "cShaderOption":
                    SetMaterialOptionsBlock(bfresMaterial, block);
                    break;
                case "skel":
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 170);
                    break;
                case "shape":
                    SetShapeUniforms(bfresMesh, meshBone.Transform, block);
                    break;
            }
        }

        private void SetModelUniforms(UniformBlock block)
        {
            block.Add(new Vector4(4, 4, 4, 1));
            block.Add(new Vector4(1, 1, 0, 0));
        }

        private void SetShapeUniforms(BfresMeshAsset mesh, Matrix4 mat, UniformBlock block)
        {
            block.Add(mat.Column0);
            block.Add(mat.Column1);
            block.Add(mat.Column2);
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            var viewMatrix = camera.ViewMatrix;
            var viewInvMatrix = camera.ViewMatrix.Inverted();
            var projMatrix = camera.ProjectionMatrix;
            var projViewMatrix = viewMatrix * projMatrix;
            var projViewInvMatrix = projViewMatrix.Inverted();

            Vector4[] cView = new Vector4[3]
            {
                viewMatrix.Column0,
                viewMatrix.Column1,
                viewMatrix.Column2,
            };
            Vector4[] cViewInv = new Vector4[3]
            {
                viewInvMatrix.Column0,
                viewInvMatrix.Column1,
                viewInvMatrix.Column2,
            };
            Vector4[] cProjView = new Vector4[4]
           {
                projViewMatrix.Column0,
                projViewMatrix.Column1,
                projViewMatrix.Column2,
                projViewMatrix.Column3,
           };
            Vector4[] cProjViewInv = new Vector4[3]
            {
                projViewInvMatrix.Column0,
                projViewInvMatrix.Column1,
                projViewInvMatrix.Column2,
            };

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);

                writer.Write(cView[0]);
                writer.Write(cView[1]);
                writer.Write(cView[2]);

                writer.Write(cViewInv[0]);
                writer.Write(cViewInv[1]);
                writer.Write(cViewInv[2]);

                writer.Write(cProjView[0]);
                writer.Write(cProjView[1]);
                writer.Write(cProjView[2]);
                writer.Write(cProjView[3]);

                writer.Write(cProjViewInv[0]);
                writer.Write(cProjViewInv[1]);
                writer.Write(cProjViewInv[2]);
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        public override void SetTextureUniforms(ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            List<string> shaderSamplers = new List<string>();
            foreach (var sampler in ShaderModel.SamplersDict.GetKeys())
                if (!string.IsNullOrEmpty(sampler))
                    shaderSamplers.Add(sampler);

            int id = 1;
            foreach (var sampler in bfresMaterial.Material.ShaderAssign.SamplerAssigns)
            {
                var fragOutput = sampler.Key;
                var bfresInput = sampler.Value;

                var textureIndex = bfresMaterial.TextureMaps.FindIndex(x => x.Sampler == bfresInput);
                if (textureIndex == -1)
                    continue;

                var texMap = mat.TextureMaps[textureIndex];

                var name = texMap.Name;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(bfresInput))
                    name = bfresMaterial.AnimatedSamplers[bfresInput];

                int index = shaderSamplers.IndexOf(fragOutput);
                var uniformName = $"{ConvertSamplerID(index)}";

                var binded = BindTexture(shader, GetTextures(), texMap, name, id);
                shader.SetInt(uniformName, id++);
            }

            LoadEngineTextures(shader, id);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void LoadEngineTextures(ShaderProgram shader, int id)
        {
            var mat = MaterialData as FMAT;

            id++;

            //BindTexture(shader, NoiseTexture, 5, id++);
            BindTexture(shader, SM3DWCubemapLoader.GetCubemap(mat), 6, id++);
            BindTextureVertex(shader, SM3DWCubemapLoader.GetIrradianceCubemap(), 7, id++);

            BindTexture(shader, HeightTexture, 8, id++);

            BindTexture(shader, AlbedoLayerTexture, GetSamplerID("_a1"), id++);
            BindTexture(shader, HeightTexture, GetSamplerID("_h0"), id++);
            BindTexture(shader, NoiseTexture, GetSamplerID("_noise"), id++);
            BindTexture(shader, MultiTextureMask, GetSamplerID("_mt0"), id++);
            BindTexture(shader, IndirectTexture, GetSamplerID("_r0"), id++);
            BindTexture(shader, IndirectTexture, 25, id++);
        }

        int GetSamplerID(string name)
        {
            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                if (ShaderModel.SamplersDict.GetKey(i) == name)
                    return ShaderModel.Samplers[i].Index;
            }
            return -1;
        }

        static void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            if (texture is GLTextureCube)
            {
                GL.TexParameter(texture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(texture.Target, TextureParameterName.TextureBaseLevel, 0);
                GL.TexParameter(texture.Target, TextureParameterName.TextureMaxLevel, 13);
            }

            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot)}", id);
        }

        static void BindTextureVertex(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot, true)}", id);
        }
    }
}
