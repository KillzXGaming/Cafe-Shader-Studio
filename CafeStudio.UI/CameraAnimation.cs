using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.Animations;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace CafeStudio.UI
{
    public class CameraAnimation : STAnimation
    {
        LineRender PositionPathRender;
        LineRender TargetPositionPathRender;
        CameraRenderer CameraRenderer;

        public Camera CameraTarget { get; set; } = new Camera();

        public void UpdateViewportCamera(GLContext context)
        {
            context.Camera.RotationLookat = CameraTarget.RotationLookat;
            context.Camera.AnimationKeys = CameraTarget.AnimationKeys;
        }

        public override void NextFrame() {
            NextFrame(GLContext.ActiveContext);
        }

        public virtual void NextFrame(GLContext context)
        {

        }

        public virtual List<Vector3> GetPositionKeyedPath()
        {
            return new List<Vector3>();
        }

        public virtual List<Vector3> GetTargetPositionKeyedPath()
        {
            return new List<Vector3>();
        }

        public virtual Vector3 GetPosition(float frame)
        {
            return Vector3.Zero;
        }

        public virtual Vector3 GetTargetPosition(float frame)
        {
            return Vector3.Zero;
        }

        void Init()
        {
            TargetPositionPathRender = new LineRender();
            PositionPathRender = new LineRender();
            CameraRenderer = new CameraRenderer();
        }

        public virtual void DrawPath(GLContext context)
        {
            if (PositionPathRender == null) Init();

            GL.LineWidth(3);

            var points = GetPositionKeyedPath();
            var targetPoints = GetTargetPositionKeyedPath();
            var transform = CalculateLookatCameraTransform(
                GetPosition(this.Frame), 
                GetTargetPosition(this.Frame));

            DrawCamera(context, transform);
            DrawCube(context, GetTargetPosition(this.Frame));

            DrawPath(context, PositionPathRender, points, new Vector4(1, 0, 0, 1));
            DrawPath(context, TargetPositionPathRender, targetPoints, new Vector4(0, 1, 0, 1));

            GL.LineWidth(1);
        }

        private void DrawCamera(GLContext context, Matrix4 transform)
        {
            var scale = Matrix4.CreateScale(1);
            CameraRenderer.Transform = scale * transform;

            CameraRenderer.Camera = context.Camera;
            CameraRenderer.Draw(context, Pass.OPAQUE, Vector4.One, Vector4.Zero, true);

            context.CurrentShader = null;
        }

        private Matrix4 CalculateLookatCameraTransform(Vector3 position, Vector3 target)
        {
            var dir = position - target;
            var rotation = RotationFromTo(new Vector3(0, 0, 1), dir.Normalized());
            return rotation * Matrix4.CreateTranslation(position);
        }

        static Matrix4 RotationFromTo(Vector3 start, Vector3 end)
        {
            var axis = Vector3.Cross(start, end).Normalized();
            var angle = (float)Math.Acos(Vector3.Dot(start, end));
            return Matrix4.CreateFromAxisAngle(axis, angle);
        }

        private void DrawCube(GLContext context, Vector3 position)
        {
            var shader = GlobalShaders.GetShader("BASIC");
            context.CurrentShader = shader;

            var transform = Matrix4.CreateTranslation(position);

            shader.SetMatrix4x4("mtxMdl", ref transform);
            shader.SetVector4("color", new Vector4(1));

            CubeRenderer.Draw(context, 0.5f);

            context.CurrentShader = null;
        }

        private void DrawPath(GLContext context, LineRender render, List<Vector3> points, Vector4 color)
        {
            var shader = GlobalShaders.GetShader("LINE");
            context.CurrentShader = shader;
            shader.SetVector4("color", color);

            render.Draw(points, new List<Vector4>(), true);

            context.CurrentShader = null;
        }
    }
}
