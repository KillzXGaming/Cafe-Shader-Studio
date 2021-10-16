using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace BfresEditor
{
    [Serializable]
    public class BfresMaterialAsset : MaterialAsset
    {
        public virtual ShaderProgram Shader => BfresRender.DefaultShader;

        public override ShaderProgram GetShader() => Shader;

        public override string Name => MaterialData.Name;

        public BfresRender ParentRenderer { get; set; }

        public Matrix4 ParentTransform => ParentRenderer.Transform.TransformMatrix;

        public FMDL ParentModel { get; set; }

        public Dictionary<string, STGenericTexture> GetTextures() {
            return ParentRenderer.Textures;
        }

        public virtual void RenderCubemap(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            Matrix4 projViewMatrix = control.Camera.ViewMatrix * control.Camera.ProjectionMatrix;
            Matrix4 modelMatrix = this.ParentTransform;

            shader.SetMatrix4x4("mtxCam", ref projViewMatrix);
            shader.SetMatrix4x4("mtxMdl", ref modelMatrix);

            var bfresMaterial = (FMAT)this.MaterialData;

            SetBlendState(bfresMaterial);
            SetUniforms(shader, bfresMaterial);
            SetRenderState(bfresMaterial);

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            int id = 1;
            for (int i = 0; i < bfresMaterial.TextureMaps?.Count; i++)
            {
                var name = bfresMaterial.TextureMaps[i].Name;
                var sampler = bfresMaterial.TextureMaps[i].Sampler;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(sampler))
                    name = bfresMaterial.AnimatedSamplers[sampler];

                string uniformName = "";
                if (bfresMaterial.TextureMaps[i].Type == STTextureType.Diffuse)
                {
                    uniformName = "diffuseMap";
                    shader.SetBoolToInt("hasDiffuseMap", true);
                }

                if (uniformName == string.Empty)
                    continue;

                var binded = BindTexture(shader, GetTextures(), bfresMaterial.TextureMaps[i], name, id);
                shader.SetInt(uniformName, id++);
            }
        }

        public override void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        {
            MaterialData = mesh.Material;

            control.UseSRBFrameBuffer = true;

            var bfresMaterial = (FMAT)this.MaterialData;
            var bfresMesh = (BfresMeshAsset)mesh;
            var meshBone = ParentModel.Skeleton.Bones[bfresMesh.BoneIndex];

            //Set materials
            var transform = meshBone.Transform;
            shader.SetMatrix4x4("RigidBindTransform", ref transform);

            SetBlendState(bfresMaterial);
            SetUniforms(shader, bfresMaterial);
            SetTextureUniforms(control, shader, MaterialData);
            SetRenderState(bfresMaterial);

            shader.SetInt("SkinCount", bfresMesh.SkinCount);
            shader.SetInt("BoneIndex", bfresMesh.BoneIndex);

            shader.SetBool("alphaTest", bfresMaterial.BlendState.AlphaTest);
            shader.SetFloat("alphaRefValue", bfresMaterial.BlendState.AlphaValue);
            shader.SetInt("alphaFunc", GetAlphaFunc(bfresMaterial.BlendState.AlphaFunction));
        }

        static int GetAlphaFunc(AlphaFunction func)
        {
            if (func == AlphaFunction.Gequal) return 0;
            if (func == AlphaFunction.Greater) return 1;
            if (func == AlphaFunction.Equal) return 2;
            if (func == AlphaFunction.Less) return 3;
            if (func == AlphaFunction.Lequal) return 4;
            return 0;
        }

        public virtual void SetBlendState(FMAT material)
        {
            var blend = material.BlendState;

            blend.RenderDepthTest();

            if (blend.State == GLMaterialBlendState.BlendState.Opaque)
            {
                GL.Disable(EnableCap.AlphaTest);
                GL.Disable(EnableCap.Blend);
                return;
            }

            blend.RenderBlendState();
        }

        private void SetUniforms(ShaderProgram shader, FMAT mat)
        {
            
        }

        public virtual void SetRenderState(FMAT mat)
        {
            GL.Enable(EnableCap.CullFace);

            if (mat.CullFront && mat.CullBack)
                GL.CullFace(CullFaceMode.FrontAndBack);
            else if (mat.CullFront)
                GL.CullFace(CullFaceMode.Front);
            else if (mat.CullBack)
                GL.CullFace(CullFaceMode.Back);
            else
                GL.Disable(EnableCap.CullFace);
        }

        public virtual void SetTextureUniforms(GLContext control, ShaderProgram shader, STGenericMaterial mat)
        {
            var bfresMaterial = (FMAT)mat;

            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            shader.SetBoolToInt("hasDiffuseMap", false);
            shader.SetBoolToInt("hasAlphaMap", false);

            int id = 1;
            for (int i = 0; i < bfresMaterial.TextureMaps?.Count; i++)
            {
                var name = mat.TextureMaps[i].Name;
                var sampler = mat.TextureMaps[i].Sampler;
                //Lookup samplers targeted via animations and use that texture instead if possible
                if (bfresMaterial.AnimatedSamplers.ContainsKey(sampler))
                    name = bfresMaterial.AnimatedSamplers[sampler];

                string uniformName = GetSamplerUniform(sampler);
                if (uniformName == string.Empty)
                    continue;

                var binded = BindTexture(shader, GetTextures(), mat.TextureMaps[i], name, id);
                bool hasTexture = binded != null;


                switch (sampler)
                {
                    //Always load diffuse map with a placeholder texture 
                    case "_a0": shader.SetBoolToInt("hasDiffuseMap", true); 
                        break;
                    case "_ms0": shader.SetBoolToInt("hasAlphaMap", hasTexture);
                        break;
                }

                if (binded != null)
                    shader.SetInt(uniformName, id++);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        string GetSamplerUniform(string sampler)
        {
            switch (sampler)
            {
                case "_a0": return "diffuseMap";
                case "_ms0": return  "alphaMap";
            }
            return "";
        }

        public static GLTexture BindTexture(ShaderProgram shader, Dictionary<string, STGenericTexture> textures,
            STGenericTextureMap textureMap, string name, int id)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            GL.BindTexture(TextureTarget.Texture2D, RenderTools.defaultTex.ID);

            if (textures.ContainsKey(name))
                return BindGLTexture(textures[name], textureMap, shader);

            foreach (var tex in Runtime.TextureCache)
            {
                if (tex.Name == name)
                    return BindGLTexture(tex, textureMap, shader);
            }

            foreach (var model in DataCache.ModelCache.Values)
            {
                if (model.Textures.ContainsKey(name))
                    return BindGLTexture(model.Textures[name], textureMap, shader);
            }

            return null;
        }

        private static GLTexture BindGLTexture(STGenericTexture texture, STGenericTextureMap textureMap, ShaderProgram shader)
        {
            if (texture.RenderableTex == null) { 
                texture.LoadRenderableTexture();
            }

            if (texture.RenderableTex == null)
                return null;

            var target = ((GLTexture)texture.RenderableTex).Target;

            GL.BindTexture(target, texture.RenderableTex.ID);
            GL.TexParameter(target, TextureParameterName.TextureWrapS, (int)OpenGLHelper.WrapMode[textureMap.WrapU]);
            GL.TexParameter(target, TextureParameterName.TextureWrapT, (int)OpenGLHelper.WrapMode[textureMap.WrapV]);
            GL.TexParameter(target, TextureParameterName.TextureMinFilter, (int)OpenGLHelper.MinFilter[textureMap.MinFilter]);
            GL.TexParameter(target, TextureParameterName.TextureMagFilter, (int)OpenGLHelper.MagFilter[textureMap.MagFilter]);
            GL.TexParameter(target, TextureParameterName.TextureLodBias, textureMap.LODBias);
            GL.TexParameter(target, TextureParameterName.TextureMaxLod, textureMap.MaxLOD);
            GL.TexParameter(target, TextureParameterName.TextureMinLod, textureMap.MinLOD);

            int[] mask = new int[4]
            {
                    OpenGLHelper.GetSwizzle(texture.RedChannel),
                    OpenGLHelper.GetSwizzle(texture.GreenChannel),
                    OpenGLHelper.GetSwizzle(texture.BlueChannel),
                    OpenGLHelper.GetSwizzle(texture.AlphaChannel),
            };
            GL.TexParameter(target, TextureParameterName.TextureSwizzleRgba, mask);
            return (GLTexture)texture.RenderableTex;
        }
    }
}
