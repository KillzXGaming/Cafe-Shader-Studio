using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class DrawableBackground 
    {
        public static System.Numerics.Vector3 BackgroundTop = new System.Numerics.Vector3(0.1f, 0.1f, 0.1f);
        public static System.Numerics.Vector3 BackgroundBottom = new System.Numerics.Vector3(0.2f, 0.2f, 0.2f);

        ShaderProgram defaultShaderProgram;

        int vbo_position = 0;
        VertexBufferObject vao;

        public static bool Display = true;

        public void Destroy()
        {
            bool buffersWereInitialized = vbo_position != 0;
            if (!buffersWereInitialized)
                return;

            GL.DeleteBuffer(vbo_position);
        }

        public void UpdateVertexData()
        {
            Vector3[] vertices = new Vector3[3];
            vertices[0] = new Vector3(-1f, -1f, 1f);
            vertices[1] = new Vector3(3f, -1f, 1f);
            vertices[2] = new Vector3(-1f, 3f, 1f);

            GL.GenBuffers(1, out vbo_position);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                                   new IntPtr(vertices.Length * Vector3.SizeInBytes),
                                   vertices, BufferUsageHint.StaticDraw);

            vao = new VertexBufferObject(vbo_position);
            vao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, 12, 0);
            vao.Initialize();

            Prepare();

            GLErrorHandler.CheckGLError();
        }

        public void Draw(GLContext control, Pass pass)
        {
            if (pass == Pass.TRANSPARENT || !Display)
                return;

            bool buffersWereInitialized = vbo_position != 0;
            if (!buffersWereInitialized)
                UpdateVertexData();

            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            control.CurrentShader = defaultShaderProgram;

            Vector3 topColor = new Vector3(BackgroundTop.X, BackgroundTop.Y, BackgroundTop.Z);
            Vector3 bottomColor = new Vector3(BackgroundBottom.X, BackgroundBottom.Y, BackgroundBottom.Z);

            defaultShaderProgram.SetVector4("topColor", new Vector4(topColor, 1.0f));
            defaultShaderProgram.SetVector4("bottomColor", new Vector4(bottomColor, 1.0f));

            BindBuffer();

            GL.UseProgram(0);
            GL.Enable(EnableCap.CullFace);
        }

        private void BindBuffer()
        {
            vao.Enable(defaultShaderProgram);
            vao.Use();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        private bool Initialized = false;
        public void Prepare()
        {
            if (Initialized)
                return;

            var solidColorFrag = new FragmentShader(
              @"#version 330
				uniform vec4 bottomColor;
				uniform vec4 topColor;

			   in vec2 texCoord;

               out vec4 FragColor;

				void main(){
	                FragColor = mix(bottomColor, topColor, texCoord.y);
				}");
            var solidColorVert = new VertexShader(
              @"#version 330
				layout(location = 0) in vec3 position;

				out vec2 texCoord;

				void main(){
					texCoord.xy = (position.xy + vec2(1.0)) * 0.5;
					gl_Position = vec4(position, 1);
				}");

            defaultShaderProgram = new ShaderProgram(solidColorFrag, solidColorVert);
            Initialized = true;
        }
    }
}