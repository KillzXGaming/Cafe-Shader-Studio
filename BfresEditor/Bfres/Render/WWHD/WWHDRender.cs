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
using BfresEditor.Properties;

namespace BfresEditor
{
    public class WWHDRender : SharcFBRenderer
    {
        public override bool UseSRGB => true;

        public override bool UseRenderer(FMAT material, string archive, string model) {
            return material.ShaderParams.ContainsKey("texpivot0") &&
                   material.Material.RenderInfos.ContainsKey("draw_buffer_type");
        }

        public WWHDRender() { }

        public WWHDRender(SHARCFB.ShaderProgram shaderModel) : base(shaderModel) { }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            //Set custom priority value
            if (mat.Material.RenderInfos.ContainsKey("sort_priority"))
            {
                int priority = (int)mat.GetRenderInfo("sort_priority");
                mesh.Priority = -priority;
            }

            if (mat.Material.RenderInfos.ContainsKey("draw_buffer_type"))
            {
                int type = (int)mat.GetRenderInfo("draw_buffer_type");
               switch (type)
                {
                    case 4:
                        mat.BlendState.BlendColor = true;
                        mat.IsTransparent = true;
                        mat.BlendState.State = GLMaterialBlendState.BlendState.Mask;
                        mesh.Pass = Pass.TRANSPARENT;
                        break;
                }
            }
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            Console.WriteLine($"{mesh.Shape.ParentModel.Name} mesh {mesh.Name} VertexSkinCount {mesh.Shape.VertexSkinCount}");

            Dictionary<string, string> options = new Dictionary<string, string>();
            options.Add("Shader_Path", "2"); //0 = Depth, 1 = gbuffer, 2 = material, 3 = ice

            //Most macros are usually shader options. Convert using the symbol names that match
            foreach (var macro in ShaderModel.VariationMacroData.symbols)
            {
                if (macro.Name == "vtx_skin_count") {
                   options.Add(macro.Name, macro.Values[0]);
                }
                if (mat.ShaderOptions.ContainsKey(macro.SymbolName))
                    options.Add(macro.Name, mat.ShaderOptions[macro.SymbolName]);
            }
            VariationBaseIndex = ShaderModel.GetVariationIndex(options);
        }

        static GLTexture2DArray LightPPTexture;
        static GLTexture2D ProjectionTexture;
        static GLTexture2D ShadowTexture;
        static GLTexture2D LinearDepthTexture;
        static GLTexture2D IceToonTexture;
        static GLTexture2D ToonTexture;
        static GLTextureCube IceCubeTexture;

        private void InitTextures() {
            IceToonTexture = GLTexture2D.FromBitmap(Resources.black);
            LightPPTexture = GLTexture2DArray.FromBitmap(Resources.white);
            LinearDepthTexture = GLTexture2D.FromBitmap(Resources.black);
            ShadowTexture = GLTexture2D.FromBitmap(Resources.white);
            ProjectionTexture = GLTexture2D.FromBitmap(Resources.white);
            ToonTexture = GLTexture2D.FromGeneric(new DDS(new System.IO.MemoryStream(Resources.toon)));
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh) {
            if (LightPPTexture == null)
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
                case "MdlEnvView":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "Mat":
                    SetMaterialBlock(bfresMaterial, block);
                    break;
                case "Shp":
                    SetShapeBlock(bfresMesh, meshBone.Transform, block);
                    break;
                case "MdlMtx":
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 100);
                    break;
                case "LightInfo":
                    SetLightUniforms(block);
                    break;
            }
        }

        private void SetLightUniforms(UniformBlock block)
        {
            Matrix4 lightMatrix = Matrix4.Identity;
            Matrix4 cloudProjMatrix = Matrix4.Identity;

            for (int i = 0; i < 4; i++)
            {
                block.Add(lightMatrix.Column0);
                block.Add(lightMatrix.Column1);
                block.Add(lightMatrix.Column2);
                block.Add(lightMatrix.Column3);
            }
            block.Add(new Vector4(1));
            //Cloud proj mtx
            block.Add(cloudProjMatrix.Column0);
            block.Add(cloudProjMatrix.Column1);
            block.Add(cloudProjMatrix.Column2);
            block.Add(cloudProjMatrix.Column3);
            //Depth shadow pow
            block.Add(1);
            block.Add(1);
            block.Add(1);
            block.Add(1);
            block.Add(new Vector4(1));
            //light pre pass param
            block.Add(new Vector4(1));
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = camera.ModelMatrix;
            var viewMatrix = camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewProjMatrix = mdlMat * viewMatrix * projMatrix;

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
            Vector4[] cLightDiffDir = new Vector4[8];
            Vector4[] cLightDiffColor = new Vector4[8];
            Vector4[] cAmbColor = new Vector4[2];
            Vector3 cFogColor = new Vector3(1);
            float cFogStart = 0;
            float cFogStartEndInv = 0;

            for (int i = 0; i < 2; i++)
            {
                cAmbColor[i] = new Vector4(1);
            }
            for (int i = 0; i < 8; i++)
            {
                cLightDiffDir[i] = new Vector4(0.1f, -0.5f, -0.5f, 0);
                cLightDiffColor[i] = new Vector4(1);
            }

            block.Buffer.Clear();
            block.Add(cView);
            block.Add(cViewProj);
            block.Add(cLightDiffDir);
            block.Add(cLightDiffColor);
            block.Add(cAmbColor);
            block.Add(cFogColor);
            block.Add(cFogStart);
            block.Add(cFogStartEndInv);
            block.Add(0);
            block.Add(0);
            block.Add(cProj);
        }

        public override void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            int id = 1;

            var pixelShader = ShaderModel.GetGX2PixelShader(BinaryIndex);
            foreach (var sampler in pixelShader.Samplers)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + id);

                switch (sampler.Name)
                {
                    case "ice_cube": IceCubeTexture.Bind(); break;
                    case "ice_toon": IceToonTexture.Bind(); break;
                    case "light_pre_pass": LightPPTexture.Bind(); break;
                    case "linear_depth": LinearDepthTexture.Bind(); break;
                    case "shadow_pre_pass": ShadowTexture.Bind(); break;
                    case "projection_map": ProjectionTexture.Bind(); break;
                    default:
                        ProjectionTexture.Bind(); break;
                }
                this.SetSampler(shader, (int)sampler.Location, ref id);
            }

            for (int i = 0; i < bfresMaterial.TextureMaps?.Count; i++)
            {
                var name = mat.TextureMaps[i].Name;
                var sampler = mat.TextureMaps[i].Sampler;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(sampler))
                    name = bfresMaterial.AnimatedSamplers[sampler];

                var location = this.GetSamplerLocation(bfresMaterial.Samplers[i]);
                if (location == -1)
                    continue;

                if (name == "ZBtoonEX")
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + id);
                    ToonTexture.Bind();
                    this.SetSampler(shader, (int)location, ref id);
                    continue;
                }

                var binded = BindTexture(shader, GetTextures(), mat.TextureMaps[i], name, id);
                this.SetSampler(shader, (int)location, ref id);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
