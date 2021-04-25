using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;
using GLFrameworkEngine;

namespace MapStudio.Rendering
{
    public class SkeletonRenderer
    {
        private STSkeleton Skeleton;

        public VertexBufferObject vbo;

        public bool Visibile
        {
            get { return Skeleton.Visible; }
            set { Skeleton.Visible = value; }
        }

        System.Drawing.Color boneColor = System.Drawing.Color.FromArgb(255, 240, 240, 0);
        System.Drawing.Color selectedBoneColor = System.Drawing.Color.FromArgb(255, 240, 240, 240);

        int vbo_position;

        private static Matrix4 prismRotation = Matrix4.CreateFromAxisAngle(new Vector3(0, 0, 1), 1.5708f);

        public SkeletonRenderer(STSkeleton skeleton) {
            Skeleton = skeleton;
        }

        ShaderProgram ShaderProgram;


        static string VertexShader = @"
            #version 330

            in vec4 point;

            uniform mat4 mtxCam;
            uniform mat4 mtxMdl;

            uniform mat4 bone;
            uniform mat4 parent;
            uniform mat4 rotation;
            uniform mat4 ModelMatrix;
            uniform int hasParent;
            uniform float scale;

            void main()
            {
                vec4 position = bone * rotation * vec4(point.xyz * scale, 1);
                if (hasParent == 1)
                {
                    if (point.w == 0)
                        position = parent * rotation * vec4(point.xyz * scale, 1);
                    else
                        position = bone * rotation * vec4((point.xyz - vec3(0, 1, 0)) * scale, 1);
                }
	            gl_Position =  mtxCam  * mtxMdl * ModelMatrix * vec4(position.xyz, 1);
            }";

        static string FragShader = @"
            #version 330
            uniform vec4 boneColor;

            out vec4 FragColor;
            out vec4 brightnessOutput;

            void main(){
	            FragColor = boneColor;
                brightnessOutput = vec4(0);
            }";

        public void Prepare(GLContext control)
        {
            if (ShaderProgram != null)
                return;

            ShaderProgram = new ShaderProgram(
                new VertexShader(VertexShader),
                new FragmentShader(FragShader));
        }

        public void Render(GLContext control)
        {
            //  if (Skeleton == null || !Visibile || !Runtime.DisplayBones)
            //      return;

            if (ShaderProgram == null)
                Prepare(control);

            CheckBuffers(control);

            control.CurrentShader = ShaderProgram;
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);

            ShaderProgram.SetMatrix4x4("rotation", ref prismRotation);

            foreach (STBone bn in Skeleton.Bones)
            {
                if (!bn.Visible)
                    continue;

                Matrix4 modelMatrix = Matrix4.Identity;

                ShaderProgram.SetVector4("boneColor", ColorUtility.ToVector4(boneColor));
                ShaderProgram.SetFloat("scale", Runtime.BonePointSize * Skeleton.PreviewScale);
                ShaderProgram.SetMatrix4x4("ModelMatrix", ref modelMatrix);


                Matrix4 transform = bn.Transform;

                ShaderProgram.SetMatrix4x4("bone", ref transform);
                ShaderProgram.SetInt("hasParent", bn.ParentIndex != -1 ? 1 : 0);

                if (bn.ParentIndex != -1)
                {
                    var transformParent = ((STBone)bn.Parent).Transform;
                    ShaderProgram.SetMatrix4x4("parent", ref transformParent);
                }

                Draw(ShaderProgram);

                if (Runtime.SelectedBoneIndex == Skeleton.Bones.IndexOf(bn))
                    ShaderProgram.SetVector4("boneColor", ColorUtility.ToVector4(selectedBoneColor));

                ShaderProgram.SetInt("hasParent", 0);
                Draw(ShaderProgram);
            }

          //  GL.UseProgram(0);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
        }

        private void Draw(ShaderProgram shader)
        {
            vbo.Enable(ShaderProgram);
            vbo.Use();
            GL.DrawArrays(PrimitiveType.Lines, 0, Vertices.Length);
        }

        void Destroy() {
            vbo.Dispose();
        }

        public void UpdateVertexData(GLContext control)
        {
            Prepare(control);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer,
                                   new IntPtr(Vertices.Length * Vector4.SizeInBytes),
                                   Vertices, BufferUsageHint.StaticDraw);
        }

        private void CheckBuffers(GLContext control)
        {
            bool buffersWereInitialized = vbo_position != 0;
            if (!buffersWereInitialized)
            {
                GL.GenBuffers(1, out vbo_position);

                vbo = new VertexBufferObject(vbo_position);
                vbo.AddAttribute("point", 4, VertexAttribPointerType.Float, false, 16, 0);
                vbo.Initialize();

                UpdateVertexData(control);
            }
        }

        private static List<Vector4> screenPositions = new List<Vector4>()
        {
            // cube
            new Vector4(0f, 0f, -1f, 0),
            new Vector4(1f, 0f, 0f, 0),
            new Vector4(1f, 0f, 0f, 0),
            new Vector4(0f, 0f, 1f, 0),
            new Vector4(0f, 0f, 1f, 0),
            new Vector4(-1f, 0f, 0f, 0),
            new Vector4(-1f, 0f, 0f, 0),
            new Vector4(0f, 0f, -1f, 0),

            //point top parentless
            new Vector4(0f, 0f, -1f, 0),
            new Vector4(0f, 1f, 0f, 0),
            new Vector4(0f, 0f, 1f, 0),
            new Vector4(0f, 1f, 0f, 0),
            new Vector4(1f, 0f, 0f, 0),
            new Vector4(0f, 1f, 0f, 0),
            new Vector4(-1f, 0f, 0f, 0),
            new Vector4(0f, 1f, 0f, 0),

            //point top
            new Vector4(0f, 0f, -1f, 0),
            new Vector4(0f, 1f, 0f, 1),
            new Vector4(0f, 0f, 1f, 0),
            new Vector4(0f, 1f, 0f, 1),
            new Vector4(1f, 0f, 0f, 0),
            new Vector4(0f, 1f, 0f, 1),
            new Vector4(-1f, 0f, 0f, 0),
            new Vector4(0f, 1f, 0f, 1),

            //point bottom
            new Vector4(0f, 0f, -1f, 0),
            new Vector4(0f, -1f, 0f, 0),
            new Vector4(0f, 0f, 1f, 0),
            new Vector4(0f, -1f, 0f, 0),
            new Vector4(1f, 0f, 0f, 0),
            new Vector4(0f, -1f, 0f, 0),
            new Vector4(-1f, 0f, 0f, 0),
            new Vector4(0f, -1f, 0f, 0),
        };

        Vector4[] Vertices
        {
            get
            {
                return screenPositions.ToArray();
            }
        }
    }
}
