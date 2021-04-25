using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    //https://github.com/Sage-of-Mirrors/WindEditor/blob/master/Editor/Editor/TransformGizmo.cs
    public class TranslateGizmo
    {
        public Vector3[] _colors = new Vector3[3] {
             new Vector3(1.0f, 0, 0), new Vector3(0, 0, 1.0f), new Vector3(0, 1.0f, 0)
        };

        public Vector3[] _endPostions = new Vector3[3] {
            new Vector3(2, 0, 0),new Vector3(0, 2, 0),new Vector3(0, 0, 2)
        };

        public Vector4[] _rotations = new Vector4[3] {
            new Vector4(0, 1, 0, 90),  new Vector4(1, 0, 0, -90), new Vector4(0, 0, 1, -90)
        };

        public void Render(GLContext context, Vector3 position, bool[] isSelected) {
            var shader = GlobalShaders.GetShader("GIZMO");
            context.CurrentShader = shader;

            GL.Disable(EnableCap.DepthTest);

            //Scale from camera position
            float scale = 1.0f;
           // if (position.Length != 0)
            // scale *= (context.Camera.Translation.Length / position.Length) * 0.05f;

            var translateMtx = Matrix4.CreateTranslation(position);
            var scaleMtx = Matrix4.CreateScale(scale);
            var transform = scaleMtx * translateMtx;

            //Set a center cube
            context.CurrentShader.SetMatrix4x4("mtxMdl", ref transform);
            context.CurrentShader.SetVector4("color", new Vector4(1,1,1,1));
            CubeRenderer.Draw(context, 0.255f);

            //Draw each axis object.
            var ray = context.PointScreenRay();
            for (int i = 0; i < 3; i++)
            {
                //Check ray hits inside bounding boxes
                DrawAxis(context, isSelected[i], ref transform, _endPostions[i], _rotations[i], _colors[i]);
            }

            context.CurrentShader = null;
            GL.Enable(EnableCap.DepthTest);
        }

        public void DrawAxis(GLContext context, bool isSelected, ref Matrix4 transform, Vector3 endPosition, Vector4 rotation, Vector3 color)
        {
            var translateMtx = Matrix4.CreateTranslation(new Vector3(endPosition * 0.15f));
            var rotate = Matrix4.CreateFromAxisAngle(rotation.Xyz, MathHelper.DegreesToRadians(rotation.W));
            var output = rotate * transform * translateMtx;

            context.CurrentShader.SetVector4("color", new Vector4(color, 1.0f));
            context.CurrentShader.SetVector4("selectionColor", Vector4.Zero);
            if (isSelected)
                context.CurrentShader.SetVector4("selectionColor", new Vector4(1, 1, 0.5f, 0.25f));

            context.CurrentShader.SetMatrix4x4("mtxMdl", ref output);
            CylinderRenderer.Draw(context, 0.05f, 2);

            translateMtx = Matrix4.CreateTranslation(endPosition);
            rotate = Matrix4.CreateFromAxisAngle(rotation.Xyz, MathHelper.DegreesToRadians(rotation.W));
            output = rotate * translateMtx * transform;

            context.CurrentShader.SetMatrix4x4("mtxMdl", ref output);
            ConeRenderer.Draw(context, 0.25f, 0, 1);
        }

    }
}
