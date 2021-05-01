using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    //A cubic render of the camera fustrum using the view and projection matrix data
    public class CameraRenderer
    {
        public Camera Camera { get; set; }

        VertexBufferObject vao;

        int Length;

        public CameraRenderer()
        {
            Camera = new Camera();
            Camera.TargetPosition = new Vector3();
            Camera.RotationX = 0;
            Camera.RotationY = 0;
            Camera.ZNear = 1.0f;
            Camera.ZFar = 100.0f;
            Camera.Width = 100;
            Camera.Height = 50;
            Camera.UpdateMatrices();
        }

        public void Init()
        {
            if (Camera == null)
                return;

            if (Length == 0)
            {
                int buffer = GL.GenBuffer();
                int indexBuffer = GL.GenBuffer();
                vao = new VertexBufferObject(buffer, indexBuffer);
                vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 12, 0);
                vao.Initialize();

                float farPlane = Camera.ZFar;
                float nearPlane = Camera.ZNear;
                float tan = (float)Math.Tan(Camera.Fov / 2);
                float aspect = Camera.AspectRatio;

                float nearHeight = nearPlane * tan;
                float nearWidth = nearHeight * aspect;
                float farHeight = farPlane * tan;
                float farWidth = farHeight * aspect;

                Vector3[] vertices = new Vector3[8];
                // near bottom left
                vertices[2][0] = -nearWidth; vertices[2][1] = -nearHeight; vertices[2][2] = -nearPlane;
                // near bottom right
                vertices[3][0] = nearWidth; vertices[3][1] = -nearHeight; vertices[3][2] = -nearPlane;

                // near top left
                vertices[1][0] = -nearWidth; vertices[1][1] = nearHeight; vertices[1][2] = -nearPlane;
                // near top right
                vertices[0][0] = nearWidth; vertices[0][1] = nearHeight; vertices[0][2] = -nearPlane;

                // far bottom left
                vertices[6][0] = -farWidth; vertices[6][1] = -farHeight; vertices[6][2] = -farPlane;
                // far bottom right
                vertices[7][0] = farWidth; vertices[7][1] = -farHeight; vertices[7][2] = -farPlane;

                // far top left
                vertices[5][0] = -farWidth; vertices[5][1] = farHeight; vertices[5][2] = -farPlane;
                // far top right
                vertices[4][0] = farWidth; vertices[4][1] = farHeight; vertices[4][2] = -farPlane;


                List<float> list = new List<float>();
                for (int i = 0; i < vertices.Length; i++)
                {
                    list.Add(vertices[i].X);
                    list.Add(vertices[i].Y);
                    list.Add(vertices[i].Z);
                }

                Length = vertices.Length;

                float[] data = list.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(int)), Indices, BufferUsageHint.StaticDraw);
            }
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

        public void Draw(GLContext control, Pass pass,
       Vector4 sphereColor, Vector4 outlineColor, Vector4 pickingColor)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            Init();

            if (Length == 0)
                return;

            GL.LineWidth(5f);

            var shader = GlobalShaders.GetShader("BASIC");
            shader.Enable();
            if (pass == Pass.OPAQUE)
            {
                shader.SetVector4("color", sphereColor);

                vao.Enable(control.CurrentShader);
                vao.Use();
                GL.DrawElements(BeginMode.Lines, Indices.Length, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                shader.SetVector4("color", pickingColor);

                vao.Enable(control.CurrentShader);
                vao.Use();
                GL.DrawElements(BeginMode.Lines, Indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            GL.LineWidth(1f);
        }
    }
}
