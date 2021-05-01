using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using GLFrameworkEngine;

namespace GLFrameworkEngine
{
    [Serializable]
    public class MaterialAsset : AssetBase
    {
        /// <summary>
        /// Gets or sets a list of baked textures cached to this material. 
        /// Baked textures can be kept during the process of swapping different materials.
        /// </summary>
        public List<STGenericTextureMap> CachedBakedTextures { get; set; } = new List<STGenericTextureMap>();

        public STGenericMaterial MaterialData { get; set; } = new STGenericMaterial();

        public bool IsRenderingBuffer { get; set; }

        public virtual ShaderProgram GetShader() => null;

        public virtual void Prepare(GLContext control)
        {

        }

        public virtual void Render(GLContext control, ShaderProgram shader, GenericPickableMesh mesh)
        { 

        }

        public void CreateMaterialRender(GLContext control, GenericPickableMesh mesh,
           RenderObject renderObject, EventHandler thumbnailUpdated, int width = 50, int height = 50)
        {
            ShaderProgram shader = this.GetShader();
            if (shader == null)
                return;

            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height);
            frameBuffer.Bind();

            //Create a simple mvp matrix to render the material data
            Matrix4 modelMatrix = Matrix4.CreateTranslation(0, 0, -12);
            Matrix4 viewMatrix = Matrix4.Identity;
            Matrix4 mtxProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, 1.0f, 1000f);
            Matrix4 viewProj = mtxProj * viewMatrix;

            GL.UseProgram(shader.program);
            shader.SetMatrix4x4("mtxCam", ref viewProj);
            shader.SetMatrix4x4("mtxMdl", ref modelMatrix);
            shader.SetVector3("camPosition", control.Camera.TargetPosition);
            shader.SetVector2("iResolution", new Vector2(control.Width, control.Height));
            shader.SetInt("materialRenderDisplay", 1);

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, width, height);

            //Render material data onto a textured sphere
            Render(control, shader, mesh);
 
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            //Draw the model to render onto
            switch (renderObject)
            {
                case RenderObject.Sphere:
                    DrawSphere(control);
                    break;
                case RenderObject.Plane:
                    DrawPlane(control);
                    break;
            }

            //Disable the uniform data
            shader.SetInt("materialRenderDisplay", 0);

            //Disable shader and textures
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            var thumbnail = frameBuffer.ReadImagePixels(true);

            //Dispose frame buffer
            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();

            this.Thumbnail = thumbnail;
            thumbnailUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void DrawSphere(GLContext control)
        {

        }

        private void DrawPlane(GLContext control)
        {
 
        }

        public virtual void Dispose()
        {

        }

        public enum RenderObject
        {
            Sphere,
            Cube,
            Plane,
        }
    }
}
