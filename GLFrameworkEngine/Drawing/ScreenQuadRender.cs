using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class ScreenQuadRender
    {
        static VertexBufferObject vao;

        static int Length;

        public static void Init()
        {
            if (Length == 0)
            {
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
                    new Vector2(0.0f, 1.0f),
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                };

               texCoords = new Vector2[4]
                {
                    new Vector2(0.0f, 1.0f),
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

        public static void Draw(ShaderProgram shader, int textureID)
        {
            Init();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            shader.SetInt("screenTexture", 1);

            vao.Enable(shader);
            vao.Use();
            GL.DrawArrays(PrimitiveType.QuadStrip, 0, Length);
        }

        public static void Draw()
        {
            Init();

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            vao.Enable(null);
            vao.Use();
            GL.DrawArrays(PrimitiveType.QuadStrip, 0, Length);
        }
    }
}
