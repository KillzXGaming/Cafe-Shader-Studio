using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using GLFrameworkEngine;
using System.IO;
using BfresEditor.Properties;
using MapStudio.Rendering;

namespace BfresEditor
{
    public class KSANXRender : BfshaRenderer
    {
        public enum ShaderPass
        {
            Color = 0,
            Normals = 1,
            HalfTone = 3,
            Depth = 4,
        }

        public override void LoadMesh(BfresMeshAsset mesh)
        {
            int offset = mesh.Attributes.Sum(x => x.Stride);
            for (int i = 0; i < 3; i++)
            {
                if (mesh.Attributes.Any(x => x.name == $"inst{i}"))
                    continue;

                VaoAttribute vaoAtt = new VaoAttribute();
                vaoAtt.vertexAttributeName = $"inst{i}";
                vaoAtt.name = $"inst{i}";
                vaoAtt.ElementCount = 4;
                vaoAtt.Offset = offset;
                vaoAtt.Type = VertexAttribPointerType.Float;
                mesh.Attributes.Add(vaoAtt);

                offset += vaoAtt.Stride;
            }

            var tangentAttribute = mesh.Attributes.FirstOrDefault(x => x.name == "_t0");
            if (tangentAttribute != null)
                tangentAttribute.name = "tangent";

            //Tangents need to be assigned manually for some reason.
            if (!mesh.Attributes.Any(x => x.name == "tangent"))
            {
                VaoAttribute vaoAtt = new VaoAttribute();
                vaoAtt.vertexAttributeName = $"_t0";
                vaoAtt.name = $"tangent";
                vaoAtt.ElementCount = 4;
                vaoAtt.Offset = offset;
                vaoAtt.Type = VertexAttribPointerType.Float;
                mesh.Attributes.Add(vaoAtt);

                offset += vaoAtt.Stride;
            }

            mesh.UpdateVertexBuffer();
        }

        public override void ReloadRenderState(BfresMeshAsset mesh)
        {
            var mat = mesh.Material as FMAT;
            if (mat.GetRenderInfo("blendEnabled") == 1)
            {
                mat.BlendState.BlendColor = true;
                mat.IsTransparent = true;
                mat.BlendState.State = GLMaterialBlendState.BlendState.Translucent;
                mesh.Pass = Pass.TRANSPARENT;
            }
            mesh.Pass = Pass.OPAQUE;

            bool cullBoth = mat.GetRenderInfo("cullMode") == "both";
            mat.CullBack = mat.GetRenderInfo("cullMode") == "back" || cullBoth;
            mat.CullFront = mat.GetRenderInfo("cullMode") == "front" || cullBoth;
            mat.BlendState.DepthTest = mat.GetRenderInfo("depthTestEnabled") == 1;
            mat.BlendState.DepthWrite = mat.GetRenderInfo("depthWriteEnabled") == 1;

            if (mat.GetRenderInfo("depthComparisonFunction") == "always")
                mat.BlendState.DepthFunction = DepthFunction.Always;

            //Todo, stencil testing
            if (mat.GetRenderInfo("stencilTestEnabled") == 1)
            {

            }
        }

        public override void ReloadProgram(BfresMeshAsset mesh)
        {
            ProgramPasses.Clear();

            //Keep things simple and add all the programs (game does not rely on variations)
            for (int i = 0; i < ShaderModel.ProgramCount; i++)
                this.ProgramPasses.Add(ShaderModel.GetShaderProgram(i));
        }

        public override bool UseRenderer(FMAT material, string archive, string model)
        {
            return material.Material.RenderInfos.ContainsKey("shadingModel");
        }

        static GLTexture2D ShadowDepthNearTextureID;
        static GLTexture2D ShadowDepthFarTextureID;
        static GLTexture2D ShadowDepthCharTextureID;
        static GLTexture2D ColorBufferTextureID;
        static GLTexture2D DepthBufferTextureID;
        static GLTextureCube DiffuseCubemapTextureID;
        static GLTextureCube SpecularCubemapTextureID;
        static GLTexture2D HalfTone;

        static void InitTextures()
        {
            //Cube maps
            DiffuseCubemapTextureID = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapIrradianceDefault)));

            DiffuseCubemapTextureID.Bind();
            DiffuseCubemapTextureID.MagFilter = TextureMagFilter.Linear;
            DiffuseCubemapTextureID.MinFilter = TextureMinFilter.Linear;
            DiffuseCubemapTextureID.UpdateParameters();

            GL.TexParameter(DiffuseCubemapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseCubemapTextureID.Target, TextureParameterName.TextureMaxLevel, 0);
            DiffuseCubemapTextureID.Unbind();

            SpecularCubemapTextureID = GLTextureCube.FromDDS(
                new DDS(new MemoryStream(Resources.CubemapDefault)));

            SpecularCubemapTextureID.Bind();
            SpecularCubemapTextureID.MagFilter = TextureMagFilter.Linear;
            SpecularCubemapTextureID.MinFilter = TextureMinFilter.LinearMipmapLinear;
            SpecularCubemapTextureID.UpdateParameters();

            GL.TexParameter(SpecularCubemapTextureID.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(SpecularCubemapTextureID.Target, TextureParameterName.TextureMaxLevel, 7);
            SpecularCubemapTextureID.Unbind();

            //Shadows
            ShadowDepthNearTextureID = GLTexture2D.FromBitmap(Resources.white);
            ShadowDepthFarTextureID = GLTexture2D.FromBitmap(Resources.white);
            ShadowDepthCharTextureID = GLTexture2D.FromBitmap(Resources.white);

            //Extra
            ColorBufferTextureID = GLTexture2D.FromBitmap(Resources.black);
            DepthBufferTextureID = GLTexture2D.FromBitmap(Resources.black);
            HalfTone = GLTexture2D.FromBitmap(Resources.dot);
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (DiffuseCubemapTextureID == null)
                InitTextures();

            base.Render(control, shader, mesh);
        }

        public override void LoadUniformBlock(GLContext control, ShaderProgram shader,
            UniformBlock block, string name, GenericPickableMesh mesh)
        {
            var bfresMaterial = (FMAT)this.MaterialData;
            var bfresMesh = (BfresMeshAsset)mesh;
            var meshBone = ParentModel.Skeleton.Bones[bfresMesh.BoneIndex];

            switch (name)
            {
                case "shape":
                    SetShapeBlock(bfresMesh, meshBone.Transform, block);
                    break;
                case "bone":
                    SetBoneMatrixBlock(this.ParentModel.Skeleton, bfresMesh.SkinCount > 1, block, 64);
                    break;
                case "view":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "light":
                    SetLightUniforms(block);
                    break;
                case "material":
                    SetMaterialBlock(bfresMaterial, block);
                    break;
                case "fog":
                    SetFogUniforms(block);
                    break;

                case "model":
                    SetModelBlock(block);
                    break;
            }
        }

        public void SetModelBlock(UniformBlock block)
        {
            Matrix4 transform = Matrix4.Identity;

            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.Add(transform.Column3);
        }

        private void SetFogUniforms(UniformBlock block)
        {
            block.Buffer.Clear();
            block.Add(new Vector4(0));
        }

        private void SetLightUniforms(UniformBlock block)
        {
            block.Buffer.Clear();
            block.Add(Properties.Resources.LightingTable);
        }

        public override void SetShapeBlock(BfresMeshAsset mesh, Matrix4 transform, UniformBlock block)
        {
            int numSkinning = (int)mesh.SkinCount;

            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.AddInt(numSkinning);
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            var viewMatrix = camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewProjMatrix = camera.ViewProjectionMatrix;

            Vector4[] cView = new Vector4[3]
            {
                viewMatrix.Column0,
                viewMatrix.Column1,
                viewMatrix.Column2,
            };
            Vector4[] cProj = new Vector4[4]
            {
                projMatrix.Column0,
                projMatrix.Column1,
                projMatrix.Column2,
                projMatrix.Column3,
            };

            var direction = Vector3.TransformNormal(new Vector3(0f, 0f, -1f),
                viewProjMatrix.Inverted()).Normalized();

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
   
                writer.Write(cProj[0]);
                writer.Write(cProj[1]);
                writer.Write(cProj[2]);
                writer.Write(cProj[3]);

                writer.Write(cView[0]);
                writer.Write(cView[1]);
                writer.Write(cView[2]);

                writer.SeekBegin(112);
                writer.Write(direction);
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

            int id = 1;
            foreach (var sampler in bfresMaterial.Material.ShaderAssign.SamplerAssigns)
            {
                var fragOutput = sampler.Key;
                var bfresInput = sampler.Value;

                var textureIndex = bfresMaterial.Samplers.IndexOf(fragOutput);
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

            BindTexture(shader, ShadowDepthNearTextureID, GetSamplerID("shadowMapNear"), id++);
            BindTexture(shader, ShadowDepthFarTextureID, GetSamplerID("shadowMapFar"), id++);
            BindTexture(shader, ShadowDepthFarTextureID, GetSamplerID("shadowMapDecoBg"), id++);
            BindTexture(shader, ShadowDepthFarTextureID, GetSamplerID("shadowMapDecoLand"), id++);
            BindTexture(shader, ShadowDepthCharTextureID, GetSamplerID("shadowMapChara"), id++);
            BindTexture(shader, ColorBufferTextureID, GetSamplerID("refractionColorBuffer"), id++);
            BindTexture(shader, DepthBufferTextureID, GetSamplerID("refractionDepthBuffer"), id++);
            BindTexture(shader, DiffuseCubemapTextureID, GetSamplerID("diffuseEnv"), id++);
            BindTexture(shader, SpecularCubemapTextureID, GetSamplerID("specularEnv"), id++);
            BindTexture(shader, HalfTone, GetSamplerID("halftoneDot"), id++);
        }

        int GetSamplerID(string name)
        {
            for (int i = 0; i < ShaderModel.Samplers.Count; i++)
            {
                if (ShaderModel.SamplersDict.GetKey(i) == name)
                    return ShaderModel.Samplers[i].Index;
            }
            return -1;
        }

        static void BindTexture(ShaderProgram shader, GLTexture texture, int slot, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Bind();
            shader.SetInt($"{ConvertSamplerID(slot)}", id);
        }
    }
}
