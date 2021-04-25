﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class ShadowPrepassManager
    {
        public static Framebuffer Filter;

        public static void Init()
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer,
                640, 720, PixelInternalFormat.Rgba16f, 1);
        }

        public static void CreateShadowPrepassTexture(GLContext control,
          int shadowMap, int depthTexture,  GLTexture2D output)
        {
            if (Filter == null)
                Init();

            Filter.Bind();

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, output.ID, 0);

            GL.Viewport(0, 0, control.Width, control.Height);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            var shader = GlobalShaders.GetShader("SHADOWPREPASS");
            shader.Enable();

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            if (output.Width != control.Width || output.Height != control.Height)
            {
                output.Bind();
                GL.TexImage2D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                output.Unbind();
            }

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            shader.SetInt("depthTexture", 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, shadowMap);
            shader.SetInt("shadowMap", 2);

            ScreenQuadRender.Draw();

            GL.Flush();

            Filter.Unbind();
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);
            GL.Viewport(0, 0, control.Width, control.Height);
        }
    }
}
