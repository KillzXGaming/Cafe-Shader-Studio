using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using MapStudio.Rendering;
using GLFrameworkEngine;

namespace BfresEditor
{
    /// <summary>
    /// Creates a screen buffer texture of the current color buffer.
    /// This is drawn before meshes with the UserColorPass setting used.
    /// Meshes with UserColorPass can obtain the buffer with GetColorBuffer()
    /// </summary>
    public class ScreenBufferTexture
    {
        private static Framebuffer Filter;

        public static GLTexture2D ScreenBuffer;

        public static void Init()
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer,
                640, 720, PixelInternalFormat.Rgba16f, 1);
        }

        public static GLTexture2D GetColorBuffer(GLContext control) {
            return ScreenBuffer;
        }

        public static void FilterScreen(GLContext control)
        {
            if (Filter == null)
                Init();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            Filter.Bind();
            GL.Viewport(0, 0, control.Width, control.Height);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            var shader = GlobalShaders.GetShader("SCREEN");
            shader.Enable();
            shader.SetInt("flipVertical", 1);

            var texture = (GLTexture2D)control.ScreenBuffer.Attachments[0];

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            GL.ClearColor(0,0,0,0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenQuadRender.Draw(shader, texture.ID);
            ScreenBuffer = (GLTexture2D)Filter.Attachments[0];

            GL.Flush();

            Filter.Unbind();
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);

            control.ScreenBuffer.Bind();
            GL.Viewport(0, 0, control.Width, control.Height);
        }
    }
}
