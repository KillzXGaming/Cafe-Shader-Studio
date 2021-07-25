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

        private static VertexArrayObject sphereVao;

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

                sphereVao = new VertexArrayObject(vertexBuffer, indexBuffer);
                sphereVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 24, 0);
                sphereVao.AddAttribute(1, 3, VertexAttribPointerType.Float, false, 24, 12);
                sphereVao.Initialize();

                Vertices = GetVertices();
                sphereVao.Bind();
                GL.BufferData(BufferTarget.ArrayBuffer, Vector3.SizeInBytes * Vertices.Length, Vertices, BufferUsageHint.StaticDraw);

                initialized = true;
            }
        }

        public static void Draw(GLContext context)
        {
            Init();

            sphereVao.Use();
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, Vertices.Length);
        }

        public static void DrawInstances(GLContext context, int numInstances)
        {
            Init();

            sphereVao.Use();
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, Vertices.Length, numInstances);
        }
        static Vector3[] GetVertices()
        {
            var vertices = DrawingHelper.GetSphereVertices(1, 8);
            Vector3[] buffer = new Vector3[vertices.Length * 2];

            for (int i = 0; i < vertices.Length; i++)
            {
                int index = i * 2;

                buffer[index] = vertices[i].Position;
                buffer[index + 1] = vertices[i].Normal;
            }
            return buffer;
        }
    }
}
