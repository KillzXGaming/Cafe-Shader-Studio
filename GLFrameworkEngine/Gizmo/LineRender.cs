using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class LineRender
    {
        VertexArrayObject vao;
        int length;

        void Init()
        {
            int buffer = GL.GenBuffer();
            vao = new VertexArrayObject(buffer);
            vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 16, 0);
            vao.AddAttribute(1, 4, VertexAttribPointerType.UnsignedByte, true, 16, 12);
            vao.Initialize();
        }

        void UpdateVertexData(List<Vector3> points, List<Vector4> colors)
        {
            if (vao == null)
                Init();

            List<float> list = new List<float>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector4 color = new Vector4(1);
                if (colors.Count > i)
                    color = colors[i];

                list.Add(points[i].X);
                list.Add(points[i].Y);
                list.Add(points[i].Z);
                list.Add(BitConverter.ToSingle(new byte[4]
                {
                            (byte)(color.X * 255),
                            (byte)(color.Y * 255),
                            (byte)(color.Z * 255),
                            (byte)(color.W * 255)
                }, 0));
            }

            length = points.Count;

            float[] data = list.ToArray();
            vao.Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);
        }

        public void Draw(Vector3 start, Vector3 end, Vector4 color, bool forceUpdate = false)
        {
            if (length == 0 || forceUpdate)
                UpdateVertexData(new List<Vector3>() { start, end }, new List<Vector4>() { color });

            vao.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, length);
        }

        public void Draw(List<Vector3> points, List<Vector4> colors, bool forceUpdate = false)
        {
            if (length == 0 || forceUpdate)
                UpdateVertexData(points, colors);

            vao.Use();
            GL.DrawArrays(PrimitiveType.LineStrip, 0, length);
        }
    }
}
