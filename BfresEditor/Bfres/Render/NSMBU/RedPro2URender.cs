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

namespace BfresEditor
{
    public class RedPro2URender : SharcFBRenderer
    {
        public override bool UseSRGB => false;

        //Todo find a better way to detect materials
        //These uniforms are usually used in the file, even if unused.
        //However some games may use the same ones.
        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return true;

            return material.ShaderParams.ContainsKey("mat_color0") &&
                   material.ShaderParams.ContainsKey("tev_color0") &&
                   material.ShaderParams.ContainsKey("amb_color0") &&
                   material.ShaderParams.ContainsKey("konst0") ||
                   archive == "nw4f_cs_shader" ||
                   archive == "nw4f_dv_shader" ||
                   archive == "nw4f_shader";
        }

        public RedPro2URender() { }

        public RedPro2URender(SHARCFB.ShaderProgram shaderModel) : base(shaderModel) { }

        public override void LoadMesh(BfresMeshAsset mesh)
        {
            base.LoadMesh(mesh);

            var mat = mesh.Shape.Material;
            if (mat.ShaderArchive == "nw4f_cs_shader" || mat.ShaderArchive == "nw4f_shader")
            {
                mesh.Shape.UpdateVertexBuffer(true);
                mesh.UpdateVertexBuffer();
            }
        }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            //Set custom priority value NSMBU uses
            if (mat.Material.RenderInfos.ContainsKey("priority"))
            {
                int sharc_priority = (int)mat.GetRenderInfo("priority");
                mesh.Priority = (int)(sharc_priority * 0x10000 + (uint)mesh.Shape.Shape.MaterialIndex);
            }

            string polygonOffset = mat.GetRenderInfo("polygon_offset");

            //Offset polygons
            if (polygonOffset != null && polygonOffset.Contains("yes"))
                mesh.IsSealPass = true;

            if (mat.GetRenderInfo("shadow_cast") == "shadow-only")
                mesh.IsDepthShadow = true;

            if (mat.GetRenderInfo("reflection") == "reflection-only")
                mesh.Shape.IsVisible = false;
        }

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

        static GLTexture2D ShadowMapTexture;

        private void InitTextures()
        {
            ShadowMapTexture = GLTexture2D.FromBitmap(BfresEditor.Properties.Resources.white);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (ShadowMapTexture == null)
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
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 64);
                    break;
            }
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = camera.ModelMatrix;
            var viewMatrix = camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewProjMatrix = mdlMat * viewMatrix * projMatrix;

            //cView used for matcaps. Invert and transpose it
            viewMatrix.Invert();
            viewMatrix.Transpose();

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
            Vector3[] cLightDiffDir = new Vector3[8];
            Vector4[] cLightDiffColor = new Vector4[8];
            Vector4[] cAmbColor = new Vector4[2];
            Vector3[] cFogColor = new Vector3[8];
            float[] cFogStart = new float[8];
            float[] cFogStartEndInv = new float[8];

            for (int i = 0; i < 2; i++)
            {
                cAmbColor[i] = new Vector4(1);
            }
            for (int i = 0; i < 8; i++)
            {
                cLightDiffDir[i] = new Vector3(0.1f, -0.5f, -0.5f);
                cLightDiffColor[i] = new Vector4(1);
            }
            for (int i = 0; i < 8; i++)
            {
                cFogColor[i] = new Vector3(1);
                cFogStart[i] = 100000;
                cFogStartEndInv[i] = 20000;
            }

            block.Add(cView);
            block.Add(cViewProj);
            block.Add(cLightDiffDir);
            block.Add(cLightDiffColor);
            block.Add(cAmbColor);
            block.Add(cFogColor);
            block.Add(cFogStart);
            block.Add(cFogStartEndInv);
        }

        public override void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            List<string> shaderSamplers = new List<string>();
            foreach (var sampler in ShaderModel.SamplerVariables.symbols)
            {
                if (bfresMaterial.Samplers.Contains(sampler.SymbolName))
                    shaderSamplers.Add(sampler.SymbolName);
            }

            int id = 1;
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

                GL.ActiveTexture(TextureUnit.Texture0 + id);
                var tex = BindTexture(shader, GetTextures(), mat.TextureMaps[i], name, id);
                shader.SetInt(ConvertSamplerName(location), id++);

                GL.ActiveTexture(TextureUnit.Texture0 + id);
                tex.Bind();
                shader.SetInt(ConvertSamplerFetchName(location), id++);
            }

            var pixelShader = ShaderModel.GetGX2PixelShader(this.BinaryIndex);
            foreach (var sampler in pixelShader.Samplers)
            {
                if (sampler.Name == "cShadowMap")
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + id);
                    ShadowMapTexture.Bind();
                    shader.SetInt(ConvertSamplerName((int)sampler.Location), id++);
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}
