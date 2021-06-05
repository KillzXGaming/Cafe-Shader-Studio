using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class SphereRender
    {
        public SphereRender()
        {

        }

        private static VertexArrayObject cubeVao;

        private static bool initialized = false;

        static int vertexBuffer;
        static int indexBuffer;

        static Vector3[] Vertices;

        static void Init()
        {
            if (!initialized)
            {
                GL.GenBuffers(1, out vertexBuffer);
                GL.GenBuffers(1, out indexBuffer);

                cubeVao = new VertexArrayObject(vertexBuffer, indexBuffer);
                cubeVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 12, 0);
                cubeVao.Initialize();

                Vertices = GetVertices();
                cubeVao.Bind();
                GL.BufferData(BufferTarget.ArrayBuffer, Vector3.SizeInBytes * Vertices.Length, Vertices, BufferUsageHint.StaticDraw);

                initialized = true;
            }
        }

        public static void Draw(GLContext context)
        {
            Init();

            cubeVao.Use();
            GL.DrawArrays(PrimitiveType.LineStrip, 0, Vertices.Length);
        }

        static Vector3[] GetVertices()
        {
            var vertices = DrawingHelper.GetVertices(1, 16);
            Vector3[] positions = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                positions[i] = vertices[i].Position;
            return positions;
        }
    }
}
