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

        SceneMaterial sceneMaterial = new SceneMaterial();

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
            foreach (var option in ShaderModel.DynamiOptions["gsys_assign_type"].choices)
            {
                options["gsys_assign_type"] = option;
                int programIndex = ShaderModel.GetProgramIndex(options);
                if (programIndex == -1)
                    continue;

                Console.WriteLine($"program {programIndex}");

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

        class SceneMaterial
        {
            const float light_intensity = 1.0f; //Multipled by light scale param (fp_c7_data[2].y)

            //Note alpha for colors also are intensity factors
            public Vector4 shadow_color = new Vector4(0, 0, 0, 1);
            public Vector4 ao_color = new Vector4(0, 0, 0, 1);

            public Vector4 lighting = new Vector4(1.0f, light_intensity, 1.0f, 1.0f);
            public Vector4 lighting_specular = new Vector4(1, 0.9f, 1, 1);
            //Z value controls shadow intensity when a light source hits shadows
            public Vector4 light_prepass_param = new Vector4(1, 1, 0, 1);
            public Vector4 exposure = new Vector4(1);

            public void AddBlock(UniformBlock block)
            {
                block.Add(shadow_color);
                block.Add(ao_color);
                block.Add(lighting);
                block.Add(lighting_specular);
                block.Add(light_prepass_param);
                block.Add(exposure);
            }

            public void Write(Toolbox.Core.IO.FileWriter writer)
            {
                writer.Write(shadow_color);
                writer.Write(ao_color);
                writer.Write(lighting);
                writer.Write(lighting_specular);
                writer.Write(light_prepass_param);
                writer.Write(exposure);
            }
        }

        public static GLTexture DiffuseLightmapTexture;

        //WiiU Specific
        public static GLTexture DiffuseLightmapTextureArray;

        static GLTexture2D ProjectionTexture;
        static GLTexture2D DepthShadowCascadeTexture;
        static GLTexture2D NormalizedLinearDepth;

        static void InitTextures()
        {
            //Reflective cubemap
           var cubemap = GLTextureCubeArray.FromDDS(
                new DDS($"Resources\\CubemapHDR.dds"));

            CubemapManager.InitDefault(cubemap);

            CubemapManager.CubeMapTextureArray = GLTexture2DArray.FromDDS(new DDS($"Resources\\CubemapHDR.dds"));
            DiffuseLightmapTextureArray = GLTexture2DArray.FromDDS(new DDS[2]
            {
                new DDS(new MemoryStream(Resources.CubemapLightmap)),
                new DDS(new MemoryStream(Resources.CubemapLightmapShadow)) 
            });

            //Diffuse cubemap lighting
            //Map gets updated when an object moves using probe lighting.
            DiffuseLightmapTexture = GLTextureCube.FromDDS(
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

            DepthShadowCascadeTexture = GLTexture2D.FromBitmap(Resources.white);

            //Tire marks
            ProjectionTexture = GLTexture2D.FromBitmap(Resources.white);

            //Depth information. Likely for shadows
            NormalizedLinearDepth = GLTexture2D.FromBitmap(Resources.black);

            //Adjust mip levels

            CubemapManager.CubeMapTexture.Bind();
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureMaxLevel, 13);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(CubemapManager.CubeMapTexture.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            CubemapManager.CubeMapTexture.Unbind();

            DiffuseLightmapTexture.Bind();
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureMaxLevel, 2);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(DiffuseLightmapTexture.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            DiffuseLightmapTexture.Unbind();

            LightingEngine.LightSettings.InitTextures();
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (ProjectionTexture == null)
                InitTextures();

            control.UseSRBFrameBuffer = true;
            if (AreaIndex == -1)
                AreaIndex = GetAreaIndex();

            base.Render(control, shader, mesh);
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

        private GLTexture SetScreenTextureBuffer(GLContext control)
        {
            var screenBuffer = ScreenBufferTexture.GetColorBuffer(control);
            if (screenBuffer == null)
                return null;

            return screenBuffer;
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
            sceneMaterial.AddBlock(block);
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
                writer.Write(new byte[128 * 16]);

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

                if (!IsSwitch) {

                    writer.SeekBegin(26 * 16);
                    writer.Write(new Vector4(1));

                    //Wii U saves scene params in view block
                    writer.SeekBegin(53 * 16);
                    writer.Write(sceneMaterial.shadow_color);
                    writer.Write(sceneMaterial.ao_color);

                    writer.SeekBegin(70 * 16);
                    writer.Write(sceneMaterial.light_prepass_param);
                    writer.Write(new Vector4(1));
                    writer.Write(sceneMaterial.exposure);
                    writer.Write(sceneMaterial.lighting);
                    writer.Write(new Vector4(1));

                    //Cubemap params
                    writer.Write(1024.0f);
                    writer.Write(4.0f);
                    writer.Write(1.0f);
                    writer.Write(1.0f);
                    writer.Write(new Vector4(1280, 720, 0.0078f, 0.00139f));
                    writer.Write(new Vector4(1, 0, 0, 0)); //Set to 1 for usable ratio lighting
                    writer.Write(sceneMaterial.lighting_specular);
                }
                else
                {
                    writer.SeekBegin(53 * 16);
                    writer.Write(new Vector4(1));

                    writer.SeekBegin(656);
                    writer.Write(0.55f);
                    writer.Write(1);

                    writer.SeekBegin(64 * 16);
                    //Cubemap params
                    writer.Write(1024.0f);
                    writer.Write(4.0f);
                    writer.Write(1.0f);
                    writer.Write(1.0f);
                }

                writer.SeekBegin(78 * 16);
                writer.Write(0.0f);
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

        public override GLTexture GetExternalTexture(GLContext control, string sampler)
        {
            var lightSettings = LightingEngine.LightSettings;
            switch (sampler)
            {
                case "gsys_lightmap_diffuse":
                    {
                        if (!IsSwitch)
                            return DiffuseLightmapTextureArray;

                        if (ParentRenderer.DiffuseProbeTexture != null)
                            return ParentRenderer.DiffuseProbeTexture;
                        else if (lightSettings.Lightmaps.ContainsKey(AreaIndex))
                            return lightSettings.Lightmaps[AreaIndex];
                        else
                            return DiffuseLightmapTexture;
                    }
                case "gsys_cube_map":
                    if (IsSwitch)
                        return CubemapManager.CubeMapTexture;
                    else
                        return CubemapManager.CubeMapTextureArray;
                case "gsys_static_depth_shadow":
                    return LightingEngine.LightSettings.ShadowPrepassTexture;
                case "gsys_depth_shadow_cascade":
                    return DepthShadowCascadeTexture;
                case "gsys_projection0":
                    return ProjectionTexture;
                case "gsys_light_prepass":
                    return lightSettings.LightPrepassTexture;
                case "gsys_normalized_linear_depth":
                    return NormalizedLinearDepth;
                case "gsys_color_buffer":
                    return SetScreenTextureBuffer(control);
                default:
                    return null;
            }
        }
    }
}
