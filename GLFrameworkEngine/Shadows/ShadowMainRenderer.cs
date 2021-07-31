using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class ShadowMainRenderer
    {
        private ShadowBox shadowBox;
        private Matrix4 projectionMatrix = new Matrix4();
        private Matrix4 lightViewMatrix = new Matrix4();
        private Matrix4 projectionViewMatrix = new Matrix4();
        private Matrix4 offset = createOffset();
        private ShadowFrameBuffer ShadowFrameBuffer;

        public ShadowMainRenderer(Camera camera) {
            shadowBox = new ShadowBox(lightViewMatrix, camera);
            ShadowFrameBuffer = new ShadowFrameBuffer(camera.Width, camera.Height);
        }

        public DepthTexture GetProjectedShadow() => ShadowFrameBuffer.GetShadowTexture();

        public Matrix4 GetLightSpaceMatrix() => lightViewMatrix;
        public Matrix4 GetProjectionViewMatrix() => projectionViewMatrix;
        public Matrix4 GetProjectionMatrix() => projectionMatrix;

        public void Render(GLContext context, Vector3 lightDirection)
        {
            shadowBox.Update();
            Prepare(lightDirection, shadowBox);

            var shader = GlobalShaders.GetShader("SHADOW");
            context.CurrentShader = shader;

            foreach (var obj in context.Scene.Objects)
                obj.DrawShadowModel(context);

            context.CurrentShader = null;

            Finish();
        }
         
        public Matrix4 GetToShadowMapSpaceMatrix()
        {
            return offset * projectionViewMatrix;
        }

        private void Prepare(Vector3 lightDirection, ShadowBox box)
        {
            updateOrthoProjectionMatrix(box.GetWidth(), box.GetHeight(), box.GetLength());
            updateLightViewMatrix(lightDirection, box.GetCenter());
            projectionViewMatrix = lightViewMatrix * projectionMatrix;

            ShadowFrameBuffer.Bind();
            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        private void Finish() {
            ShadowFrameBuffer.Unbind();
        }

        private void updateLightViewMatrix(Vector3 direction, Vector3 center)
        {
            direction.Normalize();

            float pitch = (float)MathF.Acos(new Vector2(direction.X, direction.Z).Length);
            float yaw = (float)MathHelper.RadiansToDegrees(((float)Math.Atan(direction.X / direction.Z)));
            yaw = direction.Z > 0 ? yaw - 180 : yaw;

            lightViewMatrix = Matrix4.Identity;
            lightViewMatrix *= Matrix4.CreateRotationX(pitch);
            lightViewMatrix *= Matrix4.CreateRotationY((float)-MathHelper.DegreesToRadians(yaw));
            lightViewMatrix *= Matrix4.CreateTranslation(-center);
            shadowBox.UpdateLightMatrix(lightViewMatrix);
        }

        private void updateOrthoProjectionMatrix(float width, float height, float length)
        {
            projectionMatrix = Matrix4.Identity;
            projectionMatrix[0, 0] = 2f / width;
            projectionMatrix[1, 1] = 2f / height;
            projectionMatrix[2, 2] = -2f / length;
            projectionMatrix[3, 3] = 1;
        }


        private static Matrix4 createOffset()
        {
            return Matrix4.CreateTranslation(new Vector3(0.5f, 0.5f, 0.5f)) *
                   Matrix4.CreateScale(new Vector3(0.5f, 0.5f, 0.5f));
        }
    }
}
