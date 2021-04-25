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
    public class TurboNXRender : BfshaRenderer
    {
        public static GLTextureCubeArray ReflectionCubemap = null;
        public static GLTextureCube StaticLightmapTexture = null;

        public bool UpdateProbeMap = true;

        public int AreaIndex { get; set; } = -1;

        /// <summary>
        /// Gets the param area from a bgenv lighting file.
        /// The engine uses a collect model which as boundings for areas to set various things like fog or lights.
        /// This renderer uses it to determine what fog to render and cubemap (if a map object that dynamically changes areas)
        /// </summary>
        /// <returns></returns>
        public int GetAreaIndex()
        {
            var mat = (FMAT)MaterialData;
            var areaParam = mat.ShaderParams["gsys_area_env_index_diffuse"];

            var position = ParentRenderer.Transform.Position;
            if (position != Vector3.Zero)
            {
                var area = LightingEngine.LightSettings.CollectResource.GetArea(position.X, position.Y, position.Z);
                return area.AreaIndex;
            }

            float index = (float)areaParam.DataValue;
            return (int)index;
        }

        //A somewhat hacky implimentation to quickly adjust existing params in the block for dynamic objects
        private void UpdateParam(string name, object value)
        {
            var mat = (FMAT)MaterialData;
            var areaParam = mat.ShaderParams[name];

            if (mat.AnimatedParams.ContainsKey((name)))
                mat.AnimatedParams[name].DataValue = value;
            else
                mat.AnimatedParams.Add(name, new BfresLibrary.ShaderParam()
                {
                    Name = areaParam.Name,
                    Type = areaParam.Type,
                    DataValue = value,
                });
        }

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
            if (mat.Material.ShaderAssign.ShaderOptions["enable_color_buffer"] == "1")
                mesh.UseColorBufferPass = true;

            if (mat.Name.StartsWith("CausticsArea"))
                mesh.Shape.IsVisible = false;
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

            Console.WriteLine($"Reloading Shader Program {mesh.Name}");

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
        }

        public override bool UseRenderer(FMAT material, string archive, string model) {
            //Todo users may want to use multiple shader archives in a custom map.
            //May need to find a better way to detect this.
            return archive == "Turbo_UBER"; 
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

        public static GLTextureCube DiffuseLightmapTextureID;
        static GLTexture2D ProjectionTextureID;
        static GLTexture2D DepthShadowCascadeTextureID;
        static GLTexture2D NormalizedLinearDepth;

        static void InitTextures()
        {
            //Reflective cubemap
           var cubemap = GLTextureCubeArray.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapHDR)));

            CubemapManager.InitDefault(cubemap);

            /*    CubeMapTextureID = GLTextureCubeArray.CreateEmptyCubemap(
                    4, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
                CubeMapTextureID.GenerateMipmaps();*/

            //  CubeMapTextureID = GLTextureCubeArray.FromDDS(
            // new DDS(new MemoryStream(Resources.ReflectionCubemap)));

            //Diffuse cubemap lighting
            //Map gets updated when an object moves using probe lighting.
            DiffuseLightmapTextureID = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapLightmap)),
                new DDS(new MemoryStream(Resources.CubemapLightmapShadow)));

            //Used for dynamic lights. Ie spot, point, kart lights
            //Dynamic lights are setup using the g buffer pass (normals) and depth information before material pass is drawn
            //Additional slices may be used for bloom intensity
            LightingEngine.LightSettings.LightPrepassTexture = GLTexture2DArray.FromBitmap(Resources.black);

            //Shadows
            //Channel usage:
            //Red - Dynamic shadows
            //Green - Static shadows (course, for casting onto objects)
            //Blue - Soft shading (under kart, dynamic AO?)
            //Alpha - Usually gray
            LightingEngine.LightSettings.ShadowPrepassTexture = GLTexture2D.FromBitmap(Resources.white);

            DepthShadowCascadeTextureID = GLTexture2D.FromBitmap(Resources.white);

            //Tire marks
            ProjectionTextureID = GLTexture2D.FromBitmap(Resources.white);

            //Depth information. Likely for shadows
            NormalizedLinearDepth = GLTexture2D.FromBitmap(Resources.black);

            //Adjust mip levels

            cubemap.Bind();
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureMaxLevel, 13);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            cubemap.Unbind();

            DiffuseLightmapTextureID.Bind();
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(cubemap.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(DiffuseLightmapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseLightmapTextureID.Target, TextureParameterName.TextureMaxLevel, 2);
            DiffuseLightmapTextureID.Unbind();

            LightingEngine.LightSettings.InitTextures();
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (DiffuseLightmapTextureID == null)
                InitTextures();

            control.UseSRBFrameBuffer = true;
            if (AreaIndex == -1)
                AreaIndex = GetAreaIndex();

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
            }
        }

        public override void SetShapeBlock(BfresMeshAsset mesh, Matrix4 transform, UniformBlock block)
        {
            int numSkinning = (int)mesh.SkinCount;

            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.Add(new Vector4(numSkinning, 0, 0, 0));
            block.Add(new Vector4(0));
            block.Add(AreaIndex);
            block.Add(AreaIndex);
            block.Add(AreaIndex);
            block.Add(AreaIndex);
        }

        private void SetShadowTexture(ShaderProgram shader, GLContext control)
        {
        }

        private void SetScreenTextureBuffer(ShaderProgram shader, GLContext control)
        {
            int id = 50;

            var screenBuffer = ScreenBufferTexture.GetColorBuffer(control);
            if (screenBuffer == null)
                return;

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

            if (AreaIndex != -1 && LightingEngine.LightSettings.DisplayFog) {
                var areaFog = LightingEngine.LightSettings.CourseArea.GetAreaFog(false, AreaIndex);

                if (areaFog != null && areaFog.Enable) {
                    var color = areaFog.Color.ToColorF();
                    fog[0] = new Fog()
                    {
                        Start = areaFog.Start,
                        End = areaFog.End,
                        Color = new Vector4(color.X, color.Y, color.Z, color.W),
                        Direciton = new Vector3(
                           areaFog.Direction.X,
                           areaFog.Direction.Y,
                           areaFog.Direction.Z),
                    };
                }
            }

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

                //Visualizer flags (pass 5)
                writer.SeekBegin(1984); //vec4[124]
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

            id++;

            var lightSettings = LightingEngine.LightSettings;

            GL.ActiveTexture(TextureUnit.Texture0 + id);
            if (ParentRenderer.DiffuseProbeTexture != null)
                LoadTexture(shader, ParentRenderer.DiffuseProbeTexture, 11, id++);
            else if (lightSettings.Lightmaps.ContainsKey(AreaIndex))
                LoadTexture(shader, lightSettings.Lightmaps[AreaIndex], 11, id++);
            else
                LoadTexture(shader, DiffuseLightmapTextureID, 11, id++);

            LoadTexture(shader, CubemapManager.CubeMapTexture, 12, id++);
            LoadTexture(shader, LightingEngine.LightSettings.ShadowPrepassTexture, 13, id++);
            LoadTexture(shader, DepthShadowCascadeTextureID, 14, id++);
            LoadTexture(shader, ProjectionTextureID, 15, id++);
            LoadTexture(shader, lightSettings.LightPrepassTexture, 16, id++);
            LoadTexture(shader, NormalizedLinearDepth, 18, id++);
        }

        static void LoadTexture(ShaderProgram shader, GLTexture texture, int location, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(location)}", id);
        }
    }
}
