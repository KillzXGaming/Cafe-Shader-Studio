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
    public class ACNHNXRender : BfshaRenderer
    {
        public override bool UseRenderer(FMAT material, string archive, string model) {
            return archive == "Park_UBER";
        }

        public ACNHNXRender() { }

        public ACNHNXRender(BfshaLibrary.ShaderModel shaderModel) : base(shaderModel)
        {

        }

        public override void LoadMesh(BfresMeshAsset mesh)
        {

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
            options.Add("gsys_index_stream_format", "0");
            options.Add("system_id", "0");

            int programIndex = ShaderModel.GetProgramIndex(options);
            if (programIndex == -1)
                return;

            this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
        }

        static GLTexture2D CubemapTexture;
        static GLTexture2D ProjectionTexture;
        static GLTexture2D User0Texture;
        static GLTexture2D User1Texture;
        static GLTexture2D DepthShadowTexture;
        static GLTexture2D SSAOTexture;
        static GLTexture2D AOTexture;

        static void InitTextures()
        {
            //Cube maps
            CubemapTexture = GLTexture2D.FromGeneric(new DDS($"Resources\\CubemapEquat.dds"), null);

            CubemapTexture.Bind();
            CubemapTexture.MagFilter = TextureMagFilter.Linear;
            CubemapTexture.MinFilter = TextureMinFilter.LinearMipmapLinear;
            CubemapTexture.UpdateParameters();
            CubemapTexture.Unbind();

            ProjectionTexture = GLTexture2D.FromBitmap(Properties.Resources.black);
            User0Texture = GLTexture2D.FromBitmap(Properties.Resources.white);
            User1Texture = GLTexture2D.FromBitmap(Properties.Resources.white);
            DepthShadowTexture = GLTexture2D.FromBitmap(Properties.Resources.white);
            SSAOTexture = GLTexture2D.FromBitmap(Properties.Resources.white);
            AOTexture = GLTexture2D.FromBitmap(Properties.Resources.white);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (CubemapTexture == null)
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
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block);
                    break;
                case "gsys_environment":
                    break;
                case "gsys_scene_material":
                    break;
                case "gsys_res_material":
                    break;
                case "gsys_user0":
                    SetUser0Uniforms(block);
                    break;
            }
        }

        private void SetUser0Uniforms(UniformBlock block)
        {
            float drumAngle = 35.0f;
            float drumOffset = 0.0f;
            Vector2 drumIntensity = new Vector2(0);
            Vector4 pLightPos = new Vector4(160, 28, 160, 1.0f);
            Vector4[] pLightColor = new Vector4[39];
            pLightColor[0] = new Vector4();
            pLightColor[1] = new Vector4(10, 0.35f, 1.5f, 1.0f);
            pLightColor[2] = new Vector4(2, 0.5f, 0.05f, 1.0f);
            pLightColor[3] = new Vector4(7.2f, 1.05f, 1f, 1.0f);
            pLightColor[4] = new Vector4(0.2f, 0, 0, 0);
            pLightColor[5] = new Vector4();
            pLightColor[6] = new Vector4();

            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.Write(Properties.Resources.ACUser1);

                writer.SeekBegin(736);
                writer.Write(drumAngle);
                writer.Write(drumOffset);
                writer.Write(drumIntensity);
                writer.Write(pLightPos);
              /*  for (int i = 0; i < pLightColor.Length; i++)
                    writer.Write(pLightColor[i]);

                writer.SeekBegin(1168);
                writer.Write(new Vector4(1));
                */
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = ParentRenderer.Transform.TransformMatrix;
            var viewMatrix = mdlMat * camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewInverted = viewMatrix.Inverted();
            var viewProjMatrix = viewMatrix * projMatrix;

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

                //Inverse view matrix (necessary for reflections)
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

                //Visualizer flags (pass 5)
                writer.SeekBegin(1984); //vec4[124]
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

            LoadLightingTextures(shader, id);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        static void LoadLightingTextures(ShaderProgram shader, int id)
        {
            id++;

            BindTextureVertex(shader, User0Texture, 0, id++);
            BindTexture(shader, AOTexture, 5, id++);
            BindTexture(shader, ProjectionTexture, 10, id++);
            BindTexture(shader, SSAOTexture, 12, id++);  
            BindTexture(shader, User1Texture, 14, id++);
            BindTexture(shader, CubemapTexture, 15, id++);
            BindTexture(shader, DepthShadowTexture, 16, id++);
        }

        static void BindTextureVertex(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot, true)}", id);
        }

        static void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot)}", id);
        }
    }
}
