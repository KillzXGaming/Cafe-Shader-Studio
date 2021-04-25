using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class CylinderRenderer
    {
        private static VertexBufferObject sphereVao;

        private static Vertex[] Vertices = new Vertex[0];

        public static void Initialize(float radius, float height, float slices)
        {
            if (Vertices.Length == 0)
            {
                int buffer = GL.GenBuffer();
                sphereVao = new VertexBufferObject(buffer);
                sphereVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 24, 0);
                sphereVao.AddAttribute(1, 3, VertexAttribPointerType.Float, false, 24, 12);
                sphereVao.Initialize();

                List<float> list = new List<float>();
                Vertices = GetVertices(radius, height, slices);
                for (int i = 0; i < Vertices.Length; i++)
                {
                    var mat = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90)); 
                    Vertices[i].Position = Vector3.TransformPosition(Vertices[i].Position, mat);
                    Vertices[i].Normal = Vector3.TransformNormal(Vertices[i].Normal, mat);

                    list.Add(Vertices[i].Position.X);
                    list.Add(Vertices[i].Position.Y);
                    list.Add(Vertices[i].Position.Z);
                    list.Add(Vertices[i].Normal.X);
                    list.Add(Vertices[i].Normal.Y);
                    list.Add(Vertices[i].Normal.Z);
                }

                float[] data = list.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
            }
            else
            {
              //  sphereVao.Initialize();
               // DefaultShaderProgram.Link();
            }
        }

        public static void Draw(GLContext control, float radius, float height)
        {
            Initialize(radius, height, 32);

            sphereVao.Enable(control.CurrentShader);
            sphereVao.Use();
            GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
        }

        private static Vertex[] GetVertices(float radius, float height, float slices)
        {
            List<Vertex> vertices = new List<Vertex>();

            List<Vector3> discPointsBottom = new List<Vector3>();
            List<Vector3> discPointsTop = new List<Vector3>();

            float sliceArc = 360.0f / (float)slices;
            float angle = 0;
            for (int i = 0; i < slices; i++)
            {
                float x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                float z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));
                discPointsBottom.Add(new Vector3(x, 0, z));

                x = radius * (float)Math.Cos(MathHelper.DegreesToRadians(angle));
                z = radius * (float)Math.Sin(MathHelper.DegreesToRadians(angle));

                discPointsTop.Add(new Vector3(x, height, z));
                angle += sliceArc;
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p2 = discPointsBottom[i];
                Vector3 p1 = new Vector3(discPointsBottom[(i + 1) % discPointsBottom.Count]);

                vertices.Add(new Vertex() { Position = new Vector3(0, 0, 0) });
                vertices.Add(new Vertex() { Position = new Vector3(p2.X, 0, p2.Z) });
                vertices.Add(new Vertex() { Position = new Vector3(p1.X, 0, p1.Z) });

                p2 = discPointsTop[i % discPointsTop.Count];
                p1 = discPointsTop[(i + 1) % discPointsTop.Count];

                vertices.Add(new Vertex() { Position = new Vector3(0, height, 0) });
                vertices.Add(new Vertex() { Position = new Vector3(p1.X, height, p1.Z) });
                vertices.Add(new Vertex() { Position = new Vector3(p2.X, height, p2.Z) });
            }

            for (int i = 0; i < slices; i++)
            {
                Vector3 p1 = discPointsBottom[i];
                Vector3 p2 = discPointsBottom[((i + 1) % discPointsBottom.Count())];
                Vector3 p3 = discPointsTop[i];
                Vector3 p4 = discPointsTop[(i + 1) % discPointsTop.Count()];

                vertices.Add(new Vertex() { Position = p1 });
                vertices.Add(new Vertex() { Position = p3 });
                vertices.Add(new Vertex() { Position = p4 });

                vertices.Add(new Vertex() { Position = p1 });
                vertices.Add(new Vertex() { Position = p4 });
                vertices.Add(new Vertex() { Position = p2 });
            }

            return vertices.ToArray();
        }

        public struct Vertex
        {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
        }
    }
}
