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
    public class RedPro2NXRender : BfshaRenderer
    {
        public override bool UseSRGB => false;

        public override bool UseRenderer(FMAT material, string archive, string model) {
            return archive == "Wii_UBER";
        }

        public RedPro2NXRender() { }

        public RedPro2NXRender(BfshaLibrary.ShaderModel shaderModel) : base(shaderModel) { }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Shape.Material;

            //Set custom priority value NSMBU uses
            if (mat.Material.RenderInfos.ContainsKey("priority")) {
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
            ProgramPasses.Clear();
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

            int programIndex = ShaderModel.GetProgramIndex(options);
            if (programIndex == -1)
                return;

            this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
        }

        static GLTexture2D ShadowMapTexture;

        private void InitTextures() {
            ShadowMapTexture = GLTexture2D.FromBitmap(BfresEditor.Properties.Resources.white);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh) {
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
                case "env":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "material":
                    SetMaterialBlock(bfresMaterial, block);
                    break;
                case "shape":
                    SetShapeBlock(bfresMesh, meshBone.Transform, block);
                    break;
                case "skeleton":
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 100);
                    break;
                case "gsys_shader_option":
                    SetMaterialOptionsBlock(bfresMaterial, block);
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

            Vector4[] unkExtra = new Vector4[8];
            Vector4[] unkExtra2 = new Vector4[8];
            for (int i = 0; i < 8; i++)
            {
                unkExtra[i] = new Vector4(1);
                unkExtra2[i] = new Vector4(1);
            }

            block.Add(cView);
            block.Add(cViewProj);
            block.Add(cLightDiffDir);
            block.Add(cLightDiffColor);
            block.Add(cAmbColor);
            block.Add(cFogColor);
            block.Add(cFogStart);
            block.Add(cFogStartEndInv);
            block.Add(unkExtra);
            block.Add(unkExtra2);

            if (block.Buffer.Count != 1040)
                throw new Exception("Invalid MdlEnvView size");
        }

        public override void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            List<string> shaderSamplers = new List<string>();
            foreach (var sampler in ShaderModel.Samplers.GetKeys())
            {
                if (bfresMaterial.Samplers.Contains(sampler))
                    shaderSamplers.Add(sampler);
            }

            int id = 1;
            for (int i = 0; i < bfresMaterial.TextureMaps?.Count; i++)
            {
                var name = mat.TextureMaps[i].Name;
                var sampler = bfresMaterial.Samplers[i];
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(sampler))
                    name = bfresMaterial.AnimatedSamplers[sampler];

                int index = shaderSamplers.IndexOf(sampler);

                var uniformName = ConvertSamplerID(index);
                var binded = BindTexture(shader, GetTextures(), mat.TextureMaps[i], name, id);
                shader.SetInt(uniformName, id++);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                string sampler = ShaderModel.Samplers.GetKey(i);
                if (sampler == "_sm0")
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + id);
                    ShadowMapTexture.Bind();
                    shader.SetInt(ConvertSamplerID((int)ProgramPasses[ProgramIndex].SamplerLocations[i].FragmentLocation), id++);
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
        }
    }
}
