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
    public class UKingNXRender : BfshaRenderer
    {
        //BOTW has a deferred rendering setup.
        //To set this game up, it relies mostly on the gbuffer pass
        //The gbuffer pass consists of
        //Color0 - Material IDs. These determine what deferred rendering shader to use.
        //Material IDs determine what shader may be cell shaded or not.
        //Color1 - Albedo for rgb, Alpha for AO
        //Color3 - Normals for rgb, Alpha for specular

        //Render passes as labeled in bfsha binary 
        public enum ShaderPass
        {
            MATERIAL = 0,
            ZONLY,
            GBUFFER = 2,
            XLU_ZPREPASS,
            VISUALIZE,
        }

        public override void LoadMesh(BfresMeshAsset mesh)
        {
            base.LoadMesh(mesh);
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
            ProgramPasses.Clear();

            //Find the variation index by material options
            var mat = mesh.Shape.Material;

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in mat.Material.ShaderAssign.ShaderOptions)
                options.Add(op.Key, op.Value);

            //Update option from render state
            this.LoadRenderStateOptions(options, mat);

            //Dynamic options.
            options.Add("gsys_weight", mesh.Shape.VertexSkinCount.ToString());
            options.Add("gsys_assign_type", "gsys_assign_material");
            options.Add("system_id", "0");

            //The assign type has it's own set of programs for different passes
            foreach (var option in ShaderModel.DynamiOptions[1].ChoiceDict.GetKeys())
            {
                if (string.IsNullOrEmpty(option))
                    continue;

                options["gsys_assign_type"] = option;
              //  if (options["gsys_assign_type"] != "gsys_assign_gbuffer")
               //     continue;

                int programIndex = ShaderModel.GetProgramIndex(options);
                if (programIndex == -1)
                    continue;

                this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
            }
        }

        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return archive == "uking_mat";
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

        static GLTextureCubeArray CubeMapTextureID;
        public static GLTextureCube DiffuseLightmapTextureID;
        static GLTexture2D DepthShadowTextureID;
        static GLTexture2D ProjectionTextureID;
        static GLTexture2DArray LightPPTextureID;
        static GLTexture2D DepthShadowCascadeTextureID;
        static GLTexture2D NormalizedLinearDepth;

        static void InitTextures()
        {
            //Reflective cubemap
            CubeMapTextureID = GLTextureCubeArray.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapHDR)));

            CubemapManager.InitDefault(CubeMapTextureID);

            //Diffuse cubemap lighting
            //Map gets updated when an object moves using probe lighting.
            DiffuseLightmapTextureID = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapLightmap)),
                new DDS(new MemoryStream(Resources.CubemapLightmapShadow)));

            //Shadows
            //Channel usage:
            //Red - Dynamic shadows
            //Green - Static shadows (course)
            //Blue - Soft shading (under kart, dynamic AO?)
            //Alpha - Usually gray
            DepthShadowTextureID = GLTexture2D.FromBitmap(Resources.white);

            DepthShadowCascadeTextureID = GLTexture2D.FromBitmap(Resources.white);

            //Tire marks
            ProjectionTextureID = GLTexture2D.FromBitmap(Resources.white);

            //Used for dynamic lights. Ie spot, point, kart lights
            //Dynamic lights are setup using the g buffer pass (normals) and depth information before material pass is drawn
            //Additional slices may be used for bloom intensity
            LightPPTextureID = GLTexture2DArray.FromBitmap(Resources.black);

            //Depth information. Likely for shadows
            NormalizedLinearDepth = GLTexture2D.FromBitmap(Resources.black);

            //Adjust mip levels

            CubeMapTextureID.Bind();
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(CubeMapTextureID.Target, TextureParameterName.TextureMaxLevel, 13);
            CubeMapTextureID.Unbind();

            DiffuseLightmapTextureID.Bind();
            GL.TexParameter(DiffuseLightmapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseLightmapTextureID.Target, TextureParameterName.TextureMaxLevel, 2);
            DiffuseLightmapTextureID.Unbind();
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (CubeMapTextureID == null)
                InitTextures();

         //   control.ScreenBuffer.SetDrawBuffers(
          //  DrawBuffersEnum.None, DrawBuffersEnum.None, DrawBuffersEnum.None, DrawBuffersEnum.ColorAttachment0);

            base.Render(control, shader, mesh);

            if (((BfresMeshAsset)mesh).UseColorBufferPass)
                SetScreenTextureBuffer(shader, control);
            SetShadowTexture(shader, control);
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
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 200);
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
            }
        }

        private void SetShadowTexture(ShaderProgram shader, GLContext control)
        {
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

        private void SetEnvUniforms(UniformBlock block)
        {
     
        }

        private void SetSceneMatUniforms(UniformBlock block)
        {
            Vector4 toon_light_adjust = new Vector4(1);
            float cloud_ratio = 1.0f;
            float depth_shadow_offset = 0;
            float proj_shadow_offset = 0;
            float rain_ratio = 0;
            float rainfall = 0;
            float exposure = 0;
            float world_shadow_offset = 0;
            float base_light_ratio = 0;
            float main_light = 1;
            float sky_occ_offset = 0;
            float depth_shadow_scale = 1;
            float item_filter_alpha = 0;
            float ui_highlight = 0;
            float proj_discard_scale1 = 0.12f;
            float proj_discard_scale2 = 0.85f;
            float proj_discard_scale3 = 2.5f;
            float debug0 = 0.8f;
            float debug1 = 0;
            float debug2 = 0.4f;
            float debug3 = 1.0f;

            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                writer.Write(toon_light_adjust);
                writer.Write(cloud_ratio);
                writer.Write(depth_shadow_offset);
                writer.Write(proj_shadow_offset);
                writer.Write(rain_ratio);

                writer.Write(rainfall);
                writer.Write(exposure);
                writer.Write(world_shadow_offset);
                writer.Write(base_light_ratio);

                writer.Write(main_light);
                writer.Write(sky_occ_offset);
                writer.Write(depth_shadow_scale);
                writer.Write(item_filter_alpha);

                writer.Write(ui_highlight);
                writer.Write(proj_discard_scale1);
                writer.Write(proj_discard_scale2);
                writer.Write(proj_discard_scale3);

                writer.Write(debug0);
                writer.Write(debug1);
                writer.Write(debug2);
                writer.Write(debug3);
            }

            block.Buffer.Clear();
            block.Add(mem.ToArray());

            if (block.Buffer.Count != 96)
                throw new Exception("Invalid gsys_scene_material size");
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = camera.ModelMatrix;
            var viewMatrix = ParentTransform * camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewInverted = viewMatrix.Inverted();
            var viewProjMatrix = mdlMat * viewMatrix * projMatrix;

            float znear = 1.0f;
            float zfar = 100000.00f;
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
                writer.Write(new Vector4(1.0f / zDistance, znear / zDistance, camera.AspectRatio, 1.0f / camera.AspectRatio));
                writer.Write(new Vector4(zDistance, 0, 0, 0));

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
            if (DiffuseLightmapTextureID == null)
                return;

            BindTexture(shader, DiffuseLightmapTextureID, 11, id++);
            BindTexture(shader, CubemapManager.CubeMapTexture, 12, id++);
            BindTexture(shader, DepthShadowTextureID, 13, id++);
            BindTexture(shader, DepthShadowCascadeTextureID, 14, id++);
            BindTexture(shader, ProjectionTextureID, 15, id++);
            BindTexture(shader, LightPPTextureID, 16, id++);
            BindTexture(shader, NormalizedLinearDepth, 18, id++);
        }

        static void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot)}", id);
        }
    }
}
