using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents a camera screen object located in the scene.
    /// This can get the view of the camera fustrum and project it onto a quad.
    /// </summary>
    [Serializable]
    public class CameraRenderAsset : AssetBase
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Camera Camera { get; set; }

        public CameraRenderer Renderer { get; set; }

        public GLTexture2D RenderedScreen { get; set; }

        public CameraRenderAsset()
        {
            Camera = new Camera();
            Camera.UpdateTransform();

            Renderer = new CameraRenderer();
            Renderer.Camera = Camera;
        }

        public override void Draw(GLContext control)
        {
            if (RenderedScreen == null) return;

            Renderer.Draw(control, Pass.OPAQUE, Vector4.Zero, Vector4.Zero, Vector4.Zero);
        }

        private void GenerateRender(GLContext control, EventHandler thumbnailUpdate)
        {
            Framebuffer frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, Width, Height);
            frameBuffer.Bind();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, control.Width, control.Height);

            var camMtx = Camera.ViewMatrix;
            var projMtx = Camera.ProjectionMatrix;

            //Draw all the models into the current camera screen
       //     scene.Draw(control, Pass.OPAQUE);
       //     scene.Draw(control, Pass.TRANSPARENT);

            //Disable shader and textures
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            RenderedScreen = (GLTexture2D)frameBuffer.Attachments[1];
            var thumbnail = frameBuffer.ReadImagePixels(true);

            //Dispose frame buffer
            frameBuffer.Dispoe();
            frameBuffer.DisposeRenderBuffer();

            this.Thumbnail = thumbnail;
            thumbnailUpdate?.Invoke(this, EventArgs.Empty);
        }
    } 
}
