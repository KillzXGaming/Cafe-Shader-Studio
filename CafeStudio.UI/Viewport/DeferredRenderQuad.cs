using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using AGraphicsLibrary;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class DeferredRenderQuad
    {
        public static ShaderProgram DefaultShaderProgram { get; private set; }

        static VertexBufferObject vao;

        static int Length;

        public static void Initialize(GLContext control)
        {
            if (DefaultShaderProgram != null)
                return;

            if (DefaultShaderProgram == null)
            {
                string frag = System.IO.File.ReadAllText("Shaders/FinalHDR.frag");
                string vert = System.IO.File.ReadAllText("Shaders/FinalHDR.vert");

                DefaultShaderProgram = new ShaderProgram(
                    new FragmentShader(frag),
                    new VertexShader(vert));

                int buffer = GL.GenBuffer();
                vao = new VertexBufferObject(buffer);
                vao.AddAttribute(0, 2, VertexAttribPointerType.Float, false, 16, 0);
                vao.AddAttribute(1, 2, VertexAttribPointerType.Float, false, 16, 8);
                vao.Initialize();

                Vector2[] positions = new Vector2[4]
                {
                    new Vector2(-1.0f, 1.0f),
                    new Vector2(-1.0f, -1.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, -1.0f),
                };

                Vector2[] texCoords = new Vector2[4]
                {
                    new Vector2( 0.0f, 1.0f),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                };

                List<float> list = new List<float>();
                for (int i = 0; i < 4; i++)
                {
                    list.Add(positions[i].X);
                    list.Add(positions[i].Y);
                    list.Add(texCoords[i].X);
                    list.Add(texCoords[i].Y);
                }

                Length = 4;

                float[] data = list.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
            }
        }


        public static void Draw(GLContext control, GLTexture colorPass,  GLTexture bloomPass)
        {
            Initialize(control);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.CullFace);

            control.CurrentShader = DefaultShaderProgram;

            DefaultShaderProgram.SetInt("ENABLE_BLOOM", 0);
            DefaultShaderProgram.SetInt("ENABLE_LUT", 0);
            DefaultShaderProgram.SetBoolToInt("ENABLE_SRGB", control.UseSRBFrameBuffer);

            GL.ActiveTexture(TextureUnit.Texture1);
            colorPass.Bind();
            DefaultShaderProgram.SetInt("uColorTex", 1);

            if (bloomPass != null && control.EnableBloom)
            {
                DefaultShaderProgram.SetInt("ENABLE_BLOOM", 1);

                GL.ActiveTexture(TextureUnit.Texture24);
                bloomPass.Bind();
                DefaultShaderProgram.SetInt("uBloomTex", 24);
            }

            /*


                        if (LightingEngine.LightSettings.ColorCorrectionTable != null)
                        {
                            DefaultShaderProgram.SetInt("ENABLE_LUT", 1);

                            GL.ActiveTexture(TextureUnit.Texture25);
                            LightingEngine.LightSettings.ColorCorrectionTable.Bind();
                            DefaultShaderProgram.SetInt("uLutTex", 25);
                        }
                        */

            vao.Enable(DefaultShaderProgram);
            vao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Length);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.UseProgram(0);
        }
    }
}
        