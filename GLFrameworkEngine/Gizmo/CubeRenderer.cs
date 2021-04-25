using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class CubeRenderer
    {
        private static VertexArrayObject cubeVao;

        private static Vertex[] Vertices;

        public static void Initialize(float size)
        {
            GL.GenBuffers(1, out int vertexBuffer);
            GL.GenBuffers(1, out int indexBuffer);

            cubeVao = new VertexArrayObject(vertexBuffer, indexBuffer);
            cubeVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 28, 0);
            cubeVao.AddAttribute(1, 3, VertexAttribPointerType.Float, false, 28, 12);
            cubeVao.AddAttribute(2, 4, VertexAttribPointerType.UnsignedByte, true, 28, 24);
            cubeVao.Initialize();

            UpdateVertexData(new Vector4(1, 0, 0, 1), size);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(int)), Indices, BufferUsageHint.StaticDraw);
        }

        static void UpdateVertexData(Vector4 color, float size)
        {
            Vertices = GetVertices(color);

            List<float> list = new List<float>();
            for (int i = 0; i < Vertices.Length; i++)
            {
                list.Add(Vertices[i].Position.X * size);
                list.Add(Vertices[i].Position.Y * size);
                list.Add(Vertices[i].Position.Z * size);
                list.Add(Vertices[i].Normal.X);
                list.Add(Vertices[i].Normal.Y);
                list.Add(Vertices[i].Normal.Z);
                list.Add(BitConverter.ToSingle(new byte[4]
               {
                            (byte)(Vertices[i].Color.X * 255),
                            (byte)(Vertices[i].Color.Y * 255),
                            (byte)(Vertices[i].Color.Z * 255),
                            (byte)(Vertices[i].Color.W * 255)
               }, 0));
            }

            float[] data = list.ToArray();
            cubeVao.Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
        }

        private static Vertex[] GetVertices(Vector4 color)
        {
            Vector4 darker = color * 0.5f;
            int[] bottomFaces = new int[] { 0, 1, 4, 5 };

            List<Vertex> vertices = new List<Vertex>();
            for (int i = 0; i < points.Length; i++)
            {
                var col = bottomFaces.Contains(i) ? darker : color;

                vertices.Add(new Vertex()
                {
                    Position = new Vector3(
                        points[i][0],
                        points[i][1],
                        points[i][2]),
                    Normal = new Vector3(
                        normals[i][0],
                        normals[i][1],
                        normals[i][2]),
                    Color = col
                });
            }
            return vertices.ToArray();
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

        public static float[][] normals = new float[][]
        {
                new float[]{-1, -1, -1},
                new float[]{ 1, -1, -1},
                new float[]{ 1,  1, -1},
                new float[]{-1,  1, -1},
                new float[]{-1, -1,  1},
                new float[]{ 1, -1,  1},
                new float[]{ 1,  1,  1},
                new float[]{-1,  1,  1},
         };

        public static float[][] points = new float[][]
        {
                new float[]{-1,-1, 1}, //Bottom Left
                new float[]{ 1,-1, 1}, //Bottom Right
                new float[]{-1, 1, 1}, //Top Left
                new float[]{ 1, 1, 1}, //Top Right
                new float[]{-1,-1,-1}, //Bottom Left -Z
                new float[]{ 1,-1,-1}, //Bottom Right -Z
                new float[]{-1, 1,-1}, //Top Left -Z
                new float[]{ 1, 1,-1}  //Top Right -Z
         };

        public struct Vertex
        {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
            public Vector4 Color { get; set; }
        }

        public static void Draw(GLContext control, float size)
        {
            if (Vertices == null)
                Initialize(size);

            cubeVao.Use();
            GL.DrawElements(BeginMode.QuadStrip, Indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }
}
