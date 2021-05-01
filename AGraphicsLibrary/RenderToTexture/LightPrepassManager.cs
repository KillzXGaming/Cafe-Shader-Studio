using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Diagnostics;
using GLFrameworkEngine;

namespace AGraphicsLibrary
{
    public class LightPrepassManager
    {
        public static Framebuffer Filter;

        public static Vector3 PointPosition;

        public static void Init(int width, int height)
        {
            Filter = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.R11fG11fB10f, 0, false);
            Filter.SetDrawBuffers(DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1);

            if (GL.GetError() == ErrorCode.InvalidOperation) Debugger.Break();
        }

        public static void CreateLightPrepassTexture(GLContext control, 
            int normalsTexture, int depthTexture, GLTexture output)
        {
            GL.BindTexture(output.Target, 0);

            if (Filter == null)
                Init(control.Width, control.Height);

            Filter.Bind();

            if (Filter.Width != control.Width || Filter.Height != control.Height)
                Filter.Resize(control.Width, control.Height);

            if (output.Width != control.Width || output.Height != control.Height)
            {
                output.Bind();
                if (output is GLTexture2DArray)
                {
                    GL.TexImage3D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 1, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                }
                else
                {
                    GL.TexImage2D(output.Target, 0, output.PixelInternalFormat,
                          control.Width, control.Height, 0, output.PixelFormat, output.PixelType, IntPtr.Zero);
                }
                output.Unbind();
            }

            GL.Viewport(0, 0, control.Width, control.Height);

            var shader = GlobalShaders.GetShader("LIGHTPREPASS");
            shader.Enable();
            UpdateUniforms(shader, control.Camera, normalsTexture, depthTexture);

            for (int i = 0; i < 1; i++)
            {
                if (output is GLTexture2DArray)
                {
                    GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0, output.ID, 0, i);
                }
                else
                {
                    GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0, output.ID, 0);
                }
            }

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenQuadRender.Draw();

            var errorcheck = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorcheck != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(errorcheck.ToString());

            GL.UseProgram(0);
            Filter.Unbind();

            if (GL.GetError() == ErrorCode.InvalidOperation) Debugger.Break();
        }

        static void UpdateUniforms(ShaderProgram shader, Camera camera,
          int normalsTexture, int depthTexture)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + 1);
            GL.BindTexture(TextureTarget.Texture2D, normalsTexture);
            shader.SetInt("normalsTexture", 1);

            GL.ActiveTexture(TextureUnit.Texture0 + 2);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            shader.SetInt("depthTexture", 2);

            int programID = shader.program;

            var projectionMatrixInverse = camera.ProjectionMatrix.Inverted();
            var viewMatrixInverse = camera.ViewMatrix.Inverted();
            var mtxProjView = camera.ProjectionMatrix * camera.ViewMatrix;

            shader.SetMatrix4x4("mtxProjInv", ref projectionMatrixInverse);
            shader.SetMatrix4x4("mtxViewInv", ref viewMatrixInverse);
            shader.SetVector3("cameraPosition", camera.TargetPosition);

            
            float projectionA = camera.ZFar / (camera.ZFar - camera.ZNear);
            float projectionB = (-camera.ZFar * camera.ZNear) / (camera.ZFar - camera.ZNear);
            shader.SetFloat("projectionA", projectionA);
            shader.SetFloat("projectionB", projectionB);
            shader.SetFloat("z_range", camera.ZFar - camera.ZNear);
            shader.SetFloat("fov_x", camera.Fov);
            shader.SetFloat("fov_y", camera.Fov);

            PointLight[] pointLights = new PointLight[32];
            for (int i = 0; i < 32; i++)
            {
                pointLights[i] = new PointLight();
                if (i == 0)
                {
                    pointLights[i].Position = PointPosition;
                    pointLights[i].Color = new Vector4(1, 0, 0, 1);
                }

                GL.Uniform4(GL.GetUniformLocation(programID, $"pointLights[{i}].uColor"), pointLights[i].Color);
                GL.Uniform3(GL.GetUniformLocation(programID, $"pointLights[{i}].uPosition"), pointLights[i].Position);
            }
        }

        class PointLight
        {
            public Vector3 Position { get; set; }
            public Vector4 Color { get; set; }
        }
    }
}
