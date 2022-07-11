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
using GLFrameworkEngine;

namespace BfresEditor
{
    public class BlitzNXRender : BfshaRenderer
    {
        public enum ShaderPass
        {
            Color = 0,
            Depth,
            LightSpec,
            Normal,
            Unknown,
            Unknown2,
        }

        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return archive == "Blitz_UBER";
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
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            //Find the variation index by material options
            var mat = mesh.Shape.Material;

            ProgramPasses.Clear();

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in mat.ShaderOptions)
                options.Add(op.Key, op.Value);

            //Update option from render state
            this.LoadRenderStateOptions(options, mat);

            //Dynamic options.
            options.Add("gsys_weight", mesh.Shape.VertexSkinCount.ToString());
            options.Add("gsys_assign_type", "gsys_assign_material");
            options.Add("system_id", "0");
            options.Add("blitz_ink_type", "0");

            //The assign type has it's own set of programs for different passes
            foreach (var option in ShaderModel.DynamiOptions[1].ChoiceDict.GetKeys())
            {
                if (string.IsNullOrEmpty(option))
                    continue;

                options["gsys_assign_type"] = option;
                int programIndex = ShaderModel.GetProgramIndex(options);
                if (programIndex == -1)
                    continue;

                this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
            }
            base.LoadMesh(mesh);
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


        static GLTexture2D ProjectionTexture;
        static GLTexture2D StaticShadowTexture;
        static GLTexture2D DepthShadowCascadeTexture;

        static GLTextureCube DiffuseLightmapTexture;

        static GLTexture2D UserData0Texture; 
        static GLTextureCubeArray PrefilterCubeArrayTexture;
        static GLTexture2D BrdfTexture;
        static GLTexture2D UserData3Texture;
        static GLTexture2DArray TableTexture;
        static GLTexture2D UserData5Texture; //Height or paint map (sampler2 in yuzu)

        static GLTexture2D ColorBufferTexture;
        static GLTexture2D DepthBufferTexture;
        static GLTexture2D LightPPTexture;
        static GLTexture2DArray SSAOBufferTexture;

        static GLTexture2D DepthShadowTexture;
        static GLTextureCubeArray DynamicReflectionTexture;

        static void InitTextures()
        {
            //Cube maps
            PrefilterCubeArrayTexture = GLTextureCubeArray.FromDDS(new DDS($"Resources{Path.DirectorySeparatorChar}CubemapPrefilter.dds"));
            DynamicReflectionTexture = GLTextureCubeArray.FromDDS(new DDS($"Resources{Path.DirectorySeparatorChar}CubemapHDR.dds"));

            DiffuseLightmapTexture = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapLightmap)));

            //Shadows
            DepthShadowTexture = GLTexture2D.FromBitmap(Resources.white);
            DepthShadowCascadeTexture = GLTexture2D.FromBitmap(Resources.white);
            StaticShadowTexture = GLTexture2D.FromBitmap(Resources.white);

            //Extra
            ProjectionTexture = GLTexture2D.FromBitmap(Resources.white);
            LightPPTexture = GLTexture2D.FromBitmap(Resources.black);

            UserData0Texture = GLTexture2D.FromBitmap(Resources.black);
            BrdfTexture = GLTexture2D.FromGeneric(
                new DDS(new MemoryStream(Resources.brdf)), new ImageParameters());
            UserData3Texture = GLTexture2D.FromBitmap(Resources.black);
            TableTexture = GLTexture2DArray.FromBitmap(Resources.white);
            UserData5Texture = GLTexture2D.FromBitmap(Resources.black);

            ColorBufferTexture = GLTexture2D.FromBitmap(Resources.black);
            DepthBufferTexture = GLTexture2D.FromBitmap(Resources.black);
            SSAOBufferTexture = GLTexture2DArray.FromBitmap(Resources.white);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (PrefilterCubeArrayTexture == null)
                InitTextures();

            base.Render(control, shader, mesh);
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
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 150);
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
                    SetUser0Uniforms(block);
                    break;
                case "gsys_user1":
                    SetUser1Uniforms(block);
                    break;
                case "gsys_user2":
                    SetUser2Uniforms(block);
                    break;
            }
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

                writer.Write(new Vector4(1, 0, 0, 0.5522344f));
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

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = camera.ModelMatrix;
            var viewMatrix = camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewInverted = viewMatrix.Inverted();
            var viewProjMatrix = mdlMat * viewMatrix * projMatrix;

            float znear = camera.ZNear;
            float zfar = camera.ZFar;
            float zDistance = zfar - znear;
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

                writer.Write(cViewInv[0]);
                writer.Write(cViewInv[1]);
                writer.Write(cViewInv[2]);
                writer.Write(new Vector4(znear, zfar, zfar / znear, 1.0f - znear / zfar));
                //Near/far calculations necessary for models to display properly
                //Default game z values are 0.1 and 20000.0
                writer.Write(new Vector4(1.0f / zDistance, znear / zDistance, camera.AspectRatio, 1.0f / camera.AspectRatio));
                writer.Write(new Vector4(zDistance, 0, 0, 0));

                writer.SeekBegin(656);
                writer.Write(0.55f);
                writer.Write(1);

                writer.SeekBegin(1024);
                //Cubemap params
                writer.Write(1024.0f); //Range
                writer.Write(4.0f); //Scale
                writer.Write(1.0f);
                writer.Write(1.0f);
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        private void SetUser0Uniforms(UniformBlock block)
        {
            //Todo figure out what values do what.

            Vector4[] blockData = new Vector4[40];
            blockData[0] = new Vector4(0.5539238f, 0.0001027618f, 0.1277139f, 0);
            blockData[1] = new Vector4(0.5539238f, 0.0001027618f, 0.1277139f, 0);
            blockData[2] = new Vector4(0.6320431f, 0.0001071832f, 0.1332085f, 0);
            blockData[3] = new Vector4(0.6589496f, 0.01626004f, 0.002031374f, 0);
            blockData[4] = new Vector4(0.5524251f, 0.002031374f, 0.7208676f, 0);
            blockData[5] = new Vector4(0.004354607f, 0.3782967f, 0.001357751f, 0);

            blockData[32] = new Vector4(-0.1398054f, 0.4021495f, -0.2326191f, 0.6173416f);
            blockData[33] = new Vector4(-0.1931684f, 0.5908378f, -0.3368035f, 0.7598742f);
            blockData[34] = new Vector4(-0.2408963f, 0.8209172f, -0.3954142f, 0.8875099f);
            blockData[35] = new Vector4(-0.1189116f, -0.1235714f, -0.003013233f, 0.1132319f);
            blockData[36] = new Vector4(-0.1978887f, -0.2433449f, 0.004212617f, 0.1701293f);
            blockData[37] = new Vector4(-0.2578259f, -0.3457301f, -0.01130502f, 0.2127025f);
            blockData[38] = new Vector4(-0.1228433f, -0.168933f, -0.2239204f, 1f);

        //    block.Add(Properties.Resources.User0);

            block.Add(blockData);

          /*  block.Add(new Vector4(0.5539238f, 0.0001027618f, 0.1277139f, 0));
            block.Add(new Vector4(0.5539238f, 0.0001027618f, 0.1277139f, 0));
            block.Add(new Vector4(0.6320431f, 0.0001071832f, 0.1332085f, 0));
            block.Add(new Vector4(0.6589496f, 0.01626004f, 0.002031374f, 0));
            block.Add(new Vector4(0.5524251f, 0.002031374f, 0.7208676f, 0));
            block.Add(new Vector4(0.004354607f, 0.3782967f, 0.001357751f, 0));
            block.Add(new Vector4(0.004354607f, 0.3782967f, 0.001357751f, 0));
            block.Add(new Vector4(0.005028193f, 0.399294f, 0.00143311f, 0));
            block.Add(new Vector4(0.2000978f, 0.4690126f, 0.005039762f, 0));
            block.Add(new Vector4(0.004606879f, 0.4690126f, 0.149474f, 0));
            block.Add(new Vector4(0.3544904f, 0.4615507f, 0, 0));
            block.Add(new Vector4(0.5075025f, 0.6489585f, 0.004094653f, 0));

            block.Add(new Vector4(0.025f, 1.2f, 0.2f, 0.3f));
            block.Add(new Vector4(0.991f, 0.8f, 1.88f, 1.0f));
            block.Add(new Vector4(1, 0.7071068f, -0.7071068f, 0.3f));

            block.Add(new Vector4(0));
            block.Add(new Vector4(0));
            block.Add(new Vector4(0));
            block.Add(new Vector4(0));
            block.Add(new Vector4(0));
            block.Add(new Vector4(0));

            block.Add(new Vector4(1, 0, 0, 0));
            block.Add(new Vector4(0, 1, 0, 0));
            block.Add(new Vector4(0, 0, 1, 0));

            block.Add(new Vector4(1, 0, 0, 0));
            block.Add(new Vector4(0, 1, 0, 0));
            block.Add(new Vector4(0, 0, 1, 0));
            block.Add(new Vector4(0, 0, 0, 1));

            block.Add(new Vector4(0));

            block.Add(new Vector4(0.5f, 2.5f, 1000f, 0.7f));
            block.Add(new Vector4(0.5f, 0.25f, 0.9f, 0.0f));*/



        }

        private void SetUser1Uniforms(UniformBlock block)
        {

        }

        private void SetUser2Uniforms(UniformBlock block)
        {
        }

        private void SetResMatUniforms(UniformBlock block)
        {
            block.Buffer.Clear();
            block.Add(new Vector4(2.007874f, 0.4f, 0.586611f, 0.114478f));
            block.Add(new Vector4(1.666667f, 0, 0, 0));
        }

        public override void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            List<string> shaderSamplers = new List<string>();
            foreach (var sampler in ShaderModel.Samplers.GetKeys())
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
            id++;

            BindTexture(shader, ProjectionTexture, 16, id++);
            BindTexture(shader, StaticShadowTexture, 17, id++);
            BindTexture(shader, DepthShadowCascadeTexture, 18, id++);
            BindTexture(shader, PrefilterCubeArrayTexture, 19, id++);
            BindTexture(shader, BrdfTexture, 20, id++);
            BindTexture(shader, DiffuseLightmapTexture, 21, id++);
            BindTexture(shader, UserData0Texture, 22, id++);
            BindTexture(shader, UserData3Texture, 23, id++);
            BindTexture(shader, TableTexture, 24, id++);
            BindTexture(shader, UserData5Texture, 25, id++);
            BindTexture(shader, ColorBufferTexture, 26, id++);
            BindTexture(shader, DepthBufferTexture, 27, id++);
            BindTexture(shader, LightPPTexture, 28, id++);
            BindTexture(shader, SSAOBufferTexture, 29, id++);
            BindTexture(shader, DepthShadowTexture, 30, id++);
           // BindTexture(shader, DynamicReflectionTexture, 31, id++);
        }

        void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot)}", id);
        }
    }
}
