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
    public class SMORenderer : BfshaRenderer
    {
        public override bool UseRenderer(FMAT material, string archive, string model) {
            return archive == "alRenderMaterial";
        }

        public SMORenderer() { }

        public SMORenderer(BfshaLibrary.ShaderModel shaderModel) : base(shaderModel)
        {

        }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            if (mat.Material.ShaderAssign.ShaderOptions["cRenderType"] == "3") {
                mesh.UseColorBufferPass = true;
            }

            if (mat.Material.ShaderAssign.ShaderOptions["cRenderType"] != "0") {
                mat.BlendState.State = GLMaterialBlendState.BlendState.Translucent;
                mat.BlendState.BlendColor = true;
                mat.IsTransparent = true;
                mesh.Pass = Pass.TRANSPARENT;
            }

            mesh.IsSealPass = mat.GetRenderInfo("enable_polygon_offset") == "true";
            mat.BlendState.DepthTest = mat.GetRenderInfo("enable_depth_test") == "true";
            mat.BlendState.DepthWrite = mat.GetRenderInfo("enable_depth_write") == "true";
            string displayFace = mat.GetRenderInfo("display_face");

            switch (displayFace)
            {
                case "front": mat.CullState = FMAT.CullMode.Back; break;
                case "back": mat.CullState = FMAT.CullMode.Front; break;
                case "both": mat.CullState = FMAT.CullMode.None; break;
                case "none": mat.CullState = FMAT.CullMode.Both; break;
            }
        }


        public override BfshaLibrary.BfshaFile TryLoadShaderArchive(BFRES bfres, string shaderFile, string shaderModel)
        {
            return SMOShaderLoader.LoadShader(shaderFile);
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            ProgramPasses.Clear();

            var mat = mesh.Shape.Material;

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in mat.ShaderOptions)
                options.Add(op.Key, op.Value);

            //Update option from render state
            this.LoadRenderStateOptions(options, mat);

            //Dynamic options.
            options["cSkinWeightNum"] = mesh.Shape.VertexSkinCount.ToString();
            options.Add("enable_compose_footprint", "0");
            options.Add("enable_compose_capture", "0");
            options.Add("enable_add_stain_proc_texture_3d", "0");
            options.Add("compose_prog_texture0", "0");
            options.Add("enable_parallax_cubemap", "0");
            options.Add("is_output_motion_vec", "0");
            options.Add("material_lod_level", "0");
            options.Add("system_id", "0");

            int programIndex = ShaderModel.GetProgramIndex(options);
            if (programIndex == -1)
                return;

            this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
        }

        static GLTexture MaterialLightCube;
        static GLTexture MaterialLightSphere;
        static GLTexture DirectionalLightTexture;
        static GLTexture ColorBufferTexture;
        static GLTexture DepthShadowTexture;
        static GLTexture LinearDepthTexture;
        static GLTexture RoughnessCubemapTexture;
        static GLTexture ExposureTexture;
        static GLTexture Uniform0Texture;

        static void InitTextures()
        {
           SMOCubemapLoader.LoadCubemap();

            MaterialLightCube = GLTextureCube.FromDDS(new DDS($"Resources\\CubemapHDR2.dds"), true);

            MaterialLightSphere = GLTexture2D.FromBitmap(Resources.black);
            DirectionalLightTexture = GLTexture2D.FromGeneric(
                 new DDS(new MemoryStream(Resources.HalfMap)), new ImageParameters());

            MaterialLightCube.Bind();
            GL.TexParameter(MaterialLightCube.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(MaterialLightCube.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(MaterialLightCube.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(MaterialLightCube.Target, TextureParameterName.TextureMaxLevel, 5);
            MaterialLightCube.Unbind();

            ColorBufferTexture = GLTexture2D.FromBitmap(Resources.black);
            DepthShadowTexture = GLTexture2D.FromBitmap(Resources.white);
            LinearDepthTexture = GLTexture2D.FromBitmap(Resources.black);
            ExposureTexture = GLTexture2D.FromBitmap(Resources.white);
            RoughnessCubemapTexture = MaterialLightCube;
            Uniform0Texture = GLTexture2D.FromBitmap(Resources.white);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (MaterialLightCube == null)
                InitTextures();

            base.Render(control, shader, mesh);

            if (((BfresMeshAsset)mesh).UseColorBufferPass)
                SetScreenTextureBuffer(shader, control);
        }

        public override void LoadUniformBlock(GLContext control, ShaderProgram shader, int index,UniformBlock block, string name, GenericPickableMesh mesh)
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
                case "cHdrTranslate":
                    SetHdrUniforms(block);
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

        private void SetScreenTextureBuffer(ShaderProgram shader, GLContext control)
        {
            int id = 50;

            var screenBuffer = ScreenBufferTexture.GetColorBuffer(control);

            GL.ActiveTexture(TextureUnit.Texture0 + id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            screenBuffer.Bind();
            shader.SetInt($"{ConvertSamplerID(12)}", id);
        }

        private void SetHdrUniforms(UniformBlock block)
        {
            float pow = 4.0f;
            float range = 8192.0f;

            //The current cubemap used uses 1024 but in game it's usually 8192
            range = 1024.0f;    

            block.Add(new Vector4(pow, range, 0.0f, 0.0f));
        }

        private void SetModelUniforms(UniformBlock block)
        {
            var matrix = Matrix4.Identity;

            block.Add(new Vector4(1, 1, 0.5f, 0.5f));
            for (int i = 0; i < 7; i++)
            {
                block.Add(matrix.Column0);
                block.Add(matrix.Column1);
                block.Add(matrix.Column2);
                block.Add(matrix.Column3);
            }
        }

        private void SetShapeUniforms(BfresMeshAsset mesh, Matrix4 mat, UniformBlock block)
        {
            block.Add(mat.Column0);
            block.Add(mat.Column1);
            block.Add(mat.Column2);
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            var modelMatrix = this.ParentRenderer.Transform.TransformMatrix;

            var viewMatrix = camera.ViewMatrix;
            var viewInvMatrix = camera.ViewMatrix.Inverted();
            var projMatrix = camera.ProjectionMatrix;
            var projViewMatrix = modelMatrix * viewMatrix * projMatrix;
            var projViewInvMatrix = projViewMatrix.Inverted();
            var projMatrixInv = projMatrix.Inverted();

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
            Vector4[] cProjInv = new Vector4[4]
             {
                projMatrixInv.Column0,
                projMatrixInv.Column1,
                projMatrixInv.Column2,
                projMatrixInv.Column3,
           };

            projViewInvMatrix.ClearTranslation();

            Vector4[] cProjViewInvNoPos = new Vector4[3]
            {
                projViewInvMatrix.Column0,
                projViewInvMatrix.Column1,
                projViewInvMatrix.Column2,
            };

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

                writer.Write(cProjInv[0]);
                writer.Write(cProjInv[1]);
                writer.Write(cProjInv[2]);
                writer.Write(cProjInv[3]);

                writer.Write(cProjViewInvNoPos[0]);
                writer.Write(cProjViewInvNoPos[1]);
                writer.Write(cProjViewInvNoPos[2]);

                writer.Write(16.66667f); // vec4[20].x
                writer.Write(1.0f); //Exposure used for the half texture
                writer.Write(0);
                writer.Write(0);

                writer.Write(0.3811009f);
                writer.Write(0.5090417f);
                writer.Write(0.7717764f);
                writer.Write(1.0f);
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

            LoadEngineTextures(shader, 20);

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

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void LoadEngineTextures(ShaderProgram shader, int id)
        {
            BindTextureVertex(shader, RoughnessCubemapTexture, 6, id++);
            BindTextureVertex(shader, DirectionalLightTexture, 15, id++);
            BindTexture(shader, Uniform0Texture, 2, id++);
            BindTexture(shader, ColorBufferTexture, 12, id++);
            BindTexture(shader, DepthShadowTexture, 13, id++);
            BindTexture(shader, RoughnessCubemapTexture, 6, id++);
            BindTexture(shader, ExposureTexture, 23, id++);

            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                var locationInfo = ProgramPasses[this.ProgramIndex].SamplerLocations[i];
                string sampler = ShaderModel.SamplersDict.GetKey(i);

                GLTexture texture = GetTextureFromSampler(sampler);
                if (texture == null)
                    continue;

                if (locationInfo.FragmentLocation != -1)
                    BindTexture(shader, texture, locationInfo.FragmentLocation, id++);

                if (locationInfo.VertexLocation != -1)
                    BindTextureVertex(shader, texture, locationInfo.VertexLocation, id++);
            }
        }

        GLTexture GetTextureFromSampler(string sampler)
        {
            switch (sampler)
            {
                case "_m0": return MaterialLightCube;
                case "_m1": return MaterialLightSphere;
                case "linear_depth": return LinearDepthTexture;
                case "_d0": return DepthShadowTexture;
                case "_d1": return DepthShadowTexture;
                case "_gem0": return MaterialLightCube;
                case "_gem1": return MaterialLightCube;
                case "_stcol0": return ColorBufferTexture;
            }
            return null;
        }

        static void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
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
