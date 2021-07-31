using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class DrawableFloor 
    {
        public static System.Numerics.Vector4 GridColor = new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f);

        public static int CellAmount = 10;
        public static int CellSize = 1;

        public static bool Display = true;

        public enum Type
        {
            Grid,
            Solid,
            Texture,
        }

        int vbo_position;

        public void Destroy()
        {
            bool buffersWereInitialized = vbo_position != 0;
            if (!buffersWereInitialized)
                return;

            GL.DeleteBuffer(vbo_position);
        }

        public List<Vector3> FillVertices(int amount, int size)
        {
            var vertices = new List<Vector3>();
            for (var i = -amount; i <= amount; i++)
            {
                vertices.Add(new Vector3(-amount * size, 0f, i * size));
                vertices.Add(new Vector3(amount * size, 0f, i * size));
                vertices.Add(new Vector3(i * size, 0f, -amount * size));
                vertices.Add(new Vector3(i * size, 0f, amount * size));
            }
            return vertices;
        }

        Vector3[] Vertices
        {
            get
            {
                return FillVertices(CellAmount, CellSize).ToArray();
            }
        }

        VertexArrayObject vao;

        public void UpdateVertexData(GLContext control)
        {
            CellSize = (int)Runtime.GridSettings.CellSize;
            CellAmount = (int)Runtime.GridSettings.CellAmount;

            Vector3[] vertices = Vertices;
            List<float> buffer = new List<float>();
            for (int i = 0; i < vertices.Length; i++)
            {
                buffer.Add(vertices[i].X);
                buffer.Add(vertices[i].Y);
                buffer.Add(vertices[i].Z);
            }

            var data = buffer.ToArray();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                                   new IntPtr(data.Length * sizeof(float)),
                                   vertices, BufferUsageHint.StaticDraw);


            GLErrorHandler.CheckGLError();
        }

        void Init() {
            GL.GenBuffers(1, out vbo_position);
            vao = new VertexArrayObject(vbo_position);
            vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 12, 0);
            vao.Initialize();
        }

        public void Draw(GLContext control, Pass pass)
        {
            if (pass != Pass.OPAQUE || !Display)
                return;

            var gridShaderProgram = GlobalShaders.GetShader("GRID");

            bool buffersWereInitialized = vbo_position != 0;
            if (!buffersWereInitialized)
            {
                Init();
                UpdateVertexData(control);
            }

            if (Runtime.GridSettings.CellSize != CellSize || Runtime.GridSettings.CellAmount != CellAmount)
                UpdateVertexData(control);

            control.CurrentShader = gridShaderProgram;

            Matrix4 previewScale = Matrix4.CreateScale(1.0f);
            gridShaderProgram.SetMatrix4x4("previewScale", ref previewScale);

            Draw(control, gridShaderProgram);

            GL.UseProgram(0);
        }

        private void Uniforms(ShaderProgram shader) {
            shader.SetVector4("gridColor", new Vector4(GridColor.X, GridColor.Y, GridColor.Z, GridColor.W));
        }

        private void Draw(GLContext control, ShaderProgram shader)
        {
            Uniforms(shader);

            vao.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, Vertices.Length);
        }
    }
}