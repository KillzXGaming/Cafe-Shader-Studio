using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class BoundingBoxRender
    {
        public BoundingBoxRender() {

        }

        private static VertexArrayObject cubeVao;

        private static bool initialized = false;

        static int vertexBuffer;
        static int indexBuffer;

        static void Init(Vector3 min, Vector3 max)
        {
            if (!initialized)
            {
                GL.GenBuffers(1, out vertexBuffer);
                GL.GenBuffers(1, out indexBuffer);

                cubeVao = new VertexArrayObject(vertexBuffer, indexBuffer);
                cubeVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 12, 0);
                cubeVao.Initialize();

                initialized = true;
            }

            Vector3[] data = GetVertices(min, max);
            cubeVao.Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, Vector3.SizeInBytes * data.Length, data, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(int)), Indices, BufferUsageHint.StaticDraw);
        }

        public static int[] Indices = new int[]
       {
            0, 1, 2, 3, //Bottom & Top
            4, 5, 6, 7, //Bottom & Top -Z
            0, 2, 1, 3, //Bottom to Top
            4, 6, 5, 7, //Bottom to Top -Z
            0, 4, 6, 2, //Bottom Z to -Z
            1, 5,  3, 7 //Top Z to -Z
        };

        public static void Draw(GLContext context, Vector3 min, Vector3 max)
        {
            Init(min, max);

            cubeVao.Use();
            GL.DrawElements(BeginMode.Lines, Indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        static Vector3[] GetVertices(Vector3 Min, Vector3 Max)
        {
            Vector3[] corners = new Vector3[8];

            corners[0] = Min;
            corners[1] = new Vector3(Min.X, Min.Y, Max.Z);
            corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
            corners[3] = new Vector3(Min.X, Max.Y, Max.Z);
            corners[4] = new Vector3(Max.X, Min.Y, Min.Z);
            corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
            corners[6] = new Vector3(Max.X, Max.Y, Min.Z);
            corners[7] = Max;

            return corners;
        }
    }
}
