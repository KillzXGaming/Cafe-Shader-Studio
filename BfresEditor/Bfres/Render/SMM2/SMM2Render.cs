using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using MapStudio.Rendering;
using Toolbox.Core;
using System.IO;
using BfresEditor.Properties;
using AGraphicsLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class SMM2Render : BfshaRenderer
    {
        public override bool UseSRGB => true;

        public static GLTextureCubeArray ReflectionCubemap = null;
        public static GLTextureCube StaticLightmapTexture = null;

        //Render passes as labeled in bfsha binary 
        public enum ShaderPass
        {
            MATERIAL = 0,
            ZONLY,
            GBUFFER = 2,
            XLU_ZPREPASS,
            VISUALIZE,
        }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            if (mat.GetRenderInfo("gsys_static_depth_shadow_only") == "1")
                mesh.IsDepthShadow = true;
            if (mat.GetRenderInfo("gsys_pass") == "seal")
                mesh.IsSealPass = true;
            if (mat.GetRenderInfo("gsys_cube_map_only") == "1")
                mesh.IsCubeMap = true;
            if (mat.GetRenderInfo("gsys_cube_map") == "1")
                mesh.RenderInCubeMap = true;
            if (mat.Material.ShaderAssign.ShaderOptions["gsys_enable_color_buffer"] == "1")
                mesh.UseColorBufferPass = true;

            mesh.Priority = mat.GetRenderInfo("gsys_priority");
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            ProgramPasses.Clear();

            var mat = mesh.Shape.Material;

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in mat.ShaderOptions)
                options.Add(op.Key, op.Value);

            if (!options.ContainsKey("gsys_alpha_test_enable")) options.Add("gsys_alpha_test_enable", "0");
            if (!options.ContainsKey("gsys_alpha_test_func")) options.Add("gsys_alpha_test_func", "6");

            //Update option from render state
            this.LoadRenderStateOptions(options, mat);

            //Dynamic options.
            options.Add("gsys_weight", mesh.Shape.VertexSkinCount.ToString());
            options.Add("gsys_assign_type", "gsys_assign_material");
            options.Add("system_id", "0");
            options.Add("gsys_index_stream_format", "0");

            Console.WriteLine($"Reloading Shader Program {mesh.Name}");

            //The assign type has it's own set of programs for different passes
            foreach (var option in ShaderModel.DynamiOptions[2].ChoiceDict.GetKeys())
            {
                if (string.IsNullOrEmpty(option))
                    continue;

                options["gsys_assign_type"] = option;
                int programIndex = ShaderModel.GetProgramIndex(options);
                if (programIndex == -1)
                {
                    Console.WriteLine($"Failed to find program! {mesh.Name}");
                    continue;
                }

                this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
            }
        }

        public override bool UseRenderer(FMAT material, string archive, string model) {
            return archive == "Block_UBER"; 
        }

        struct ResMaterial
        {
            public Vector4 Param1 { get; set; }
            public Vector4 Param2 { get; set; }
        }

        class Fog
        {
            public float End = 100000;
            public float Start = 1000;

            public float StartC => -Start / (End - Start);
            public float EndC => 1.0f / (End - Start);

            public Vector3 Direciton = new Vector3(0, 0, 0);
            public Vector4 Color = new Vector4(0);
        }

        public static GLTextureCube DiffuseCubeTextureID;
        public static GLTextureCube SpecularCubeTextureID;
        public static GLTextureCubeArray CubeMapTextureID;

        static GLTexture2D User1Texture;
        static GLTexture2D ProjectionTextureID;
        static GLTexture2D DepthShadowCascadeTextureID;

        static void InitTextures()
        {
           CubeMapTextureID = GLTextureCubeArray.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapHDR)));

            DiffuseCubeTextureID = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapLightmap)),
                new DDS(new MemoryStream(Resources.CubemapLightmapShadow)));

            SpecularCubeTextureID = GLTextureCube.CreateEmptyCubemap(32);

            LightingEngine.LightSettings.LightPrepassTexture = GLTexture2DArray.FromBitmap(Resources.black);
            LightingEngine.LightSettings.ShadowPrepassTexture = GLTexture2D.FromBitmap(Resources.white);
            DepthShadowCascadeTextureID = GLTexture2D.FromBitmap(Resources.white);
            ProjectionTextureID = GLTexture2D.FromBitmap(Resources.white);
            User1Texture = GLTexture2D.FromBitmap(Resources.white);

            //Adjust mip levels
            CubeMapTextureID.Bind();
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMaxLevel, 13);
            CubeMapTextureID.Unbind();

            DiffuseCubeTextureID.Bind();
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(DiffuseCubeTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseCubeTextureID.Target, TextureParameterName.TextureMaxLevel, 2);
            DiffuseCubeTextureID.Unbind();

            LightingEngine.LightSettings.InitTextures();
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (CubeMapTextureID == null)
                InitTextures();

            control.UseSRBFrameBuffer = true;

            base.Render(control, shader, mesh);

            if (((BfresMeshAsset)mesh).UseColorBufferPass)
                SetScreenTextureBuffer(shader, control);
        }

        public override void LoadUniformBlock(GLContext control, ShaderProgram shader, int index, UniformBlock block, string name, GenericPickableMesh mesh)
        {
            var bfresMaterial = (FMAT)this.MaterialData;
            var bfresMesh = (BfresMeshAsset)mesh;
            var meshBone = ParentModel.Skeleton.Bones[bfresMesh.BoneIndex];

            switch (name)
            {
                case "gsys_context":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "gsys_material":
                    SetMaterialBlock(bfresMaterial, block);
                    break;
                case "gsys_shape":
                    SetShapeBlock(bfresMesh, meshBone.Transform, block);
                    break;
                case "gsys_skeleton":
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block);
                    break;
                case "gsys_environment":
                    SetEnvUniforms(block);
                    break;
                case "gsys_scene_material":
                    SetSceneMatUniforms(block);
                    break;
                case "gsys_res_material":
                    SetResMatUniforms(block);
                    break;
                case "gsys_shader_option":
                    SetMaterialOptionsBlock(bfresMaterial, block);
                    break;
                case "gsys_user0":
                    SetUser0(block);
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
            shader.SetInt($"{ConvertSamplerID(17)}", id);
        }

        private void SetUser0(UniformBlock block)
        {
        }

        private void SetEnvUniforms(UniformBlock block)
        {
            Fog[] fog = new Fog[4];
            for (int i = 0; i < 4; i++)
                fog[i] = new Fog();

            fog[0] = new Fog()
            {
                Start = 1000000,
                End = 1000000,
                Color = new Vector4(1, 0, 0, 1),
                Direciton = new Vector3(0, 1, 0),
            };

            var mem = new MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(10 * 16);
                for (int i = 0; i < 1; i++)
                {
                    writer.Write(fog[i].Color);
                    writer.Write(fog[i].Direciton);
                    writer.Write(fog[i].StartC);
                    writer.Write(fog[i].EndC);
                }

                float amount = 0.1f;

                writer.Write(new Vector4(1, 0, 0, amount));
                writer.Write(new Vector3(0.6771527f, -0.4863142f, 131.3442f));
            }

            block.Buffer.Clear();
            block.Add(mem.ToArray());
        }

        private void SetSceneMatUniforms(UniformBlock block)
        {
            float light_intensity = 1.0f; //Multipled by light scale param (fp_c7_data[2].y)

            //Note alpha for colors also are intensity factors
            Vector4 shadow_color = new Vector4(0, 0, 0, 1);
            Vector4 ao_color = new Vector4(0, 0, 0, 1);

            Vector4 lighting = new Vector4(1.0f, light_intensity, 1.0f, 1.0f);
            Vector4 lighting_specular = new Vector4(1, 0.9f, 1, 1);
            //Z value controls shadow intensity when a light source hits shadows
            Vector4 light_prepass_param = new Vector4(1, 1, 0, 1);
            Vector4 exposure = new Vector4(1);
                
            block.Buffer.Clear();
            block.Add(shadow_color);
            block.Add(ao_color);
            block.Add(lighting);
            block.Add(lighting_specular);
            block.Add(light_prepass_param);
            block.Add(exposure);

            if (block.Buffer.Count != 96)
                throw new Exception("Invalid gsys_scene_material size");
        }

        private void SetViewportUniforms(Camera camera,  UniformBlock block)
        {
            Matrix4 mdlMat = ParentRenderer.Transform.TransformMatrix;
            var viewMatrix = mdlMat * camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewInverted = viewMatrix.Inverted();
            var viewProjMatrix = viewMatrix * projMatrix;

            float znear = 1.0f;
            float zfar = 100000.00f;
            Vector4[] cView = new Vector4[3]
            {
                viewMatrix.Column0,
                viewMatrix.Column1,
                viewMatrix.Column2,
            };
            Vector4[] cViewProj = new Vector4[4]
            {
                viewProjMatrix.Column0,
                viewProjMatrix.Column1,
                viewProjMatrix.Column2,
                viewProjMatrix.Column3,
            };
            Vector4[] cProj = new Vector4[4]
            {
                projMatrix.Column0,
                projMatrix.Column1,
                projMatrix.Column2,
                projMatrix.Column3,
            };
            Vector4[] cViewInv = new Vector4[3]
             {
                viewInverted.Column0,
                viewInverted.Column1,
                viewInverted.Column2,
             };

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                writer.Write(cView[0]);
                writer.Write(cView[1]);
                writer.Write(cView[2]);

                writer.Write(cViewProj[0]);
                writer.Write(cViewProj[1]);
                writer.Write(cViewProj[2]);
                writer.Write(cViewProj[3]);

                writer.Write(cProj[0]);
                writer.Write(cProj[1]);
                writer.Write(cProj[2]);
                writer.Write(cProj[3]);

                //Inverse view matrix (necessary for reflections)
                writer.Write(cViewInv[0]);
                writer.Write(cViewInv[1]);
                writer.Write(cViewInv[2]);
                writer.Write(new Vector4(znear, zfar, zfar / znear, 1.0f - znear / zfar));

                writer.SeekBegin(656);
                writer.Write(0.55f);
                writer.Write(1);

                for (int i = 0; i < 200; i++)
                    writer.Write(new Vector4(1));

                writer.SeekBegin(1024);
                //Cubemap params
                writer.Write(1024.0f);
                writer.Write(4.0f);
                writer.Write(1.0f);
                writer.Write(1.0f);
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        private void SetResMatUniforms(UniformBlock block)
        {
            block.Buffer.Clear();
            block.Add(new Vector4(2.007874f, 0.4f, 0.586611f, 0.114478f));
            block.Add(new Vector4(1.666667f, 0, 0, 0));
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

            LoadLightingTextures(shader, id);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        void LoadLightingTextures(ShaderProgram shader, int id)
        {
            if (DiffuseCubeTextureID == null)
                return;

            id++;

            var lightSettings = LightingEngine.LightSettings;

            GL.ActiveTexture(TextureUnit.Texture0 + id);
            LoadTexture(shader, DiffuseCubeTextureID, 9, id++);
            LoadTexture(shader, SpecularCubeTextureID, 10, id++);
            LoadTexture(shader, CubeMapTextureID, 11, id++);

            LoadTexture(shader, LightingEngine.LightSettings.ShadowPrepassTexture, 12, id++);
            LoadTexture(shader, LightingEngine.LightSettings.ShadowPrepassTexture, 13, id++);
            LoadTexture(shader, User1Texture, 14, id++);
            LoadTexture(shader, ProjectionTextureID, 16, id++);
        }

        static void LoadTexture(ShaderProgram shader, GLTexture texture, int location, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(location)}", id);
        }
    }
}
