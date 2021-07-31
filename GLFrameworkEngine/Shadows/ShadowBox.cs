using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace GLFrameworkEngine
{
    public class ShadowBox
    {
        private static Vector4 UP = new Vector4(0, 1, 0, 0);
        private static Vector4 FORWARD = new Vector4(0, 0, -1, 0);
        private const float SHADOW_DISTANCE = 100;
        private const float OFFSET = 10;

        private Vector3 Min;
        private Vector3 Max;

        private Matrix4 lightViewMatrix;
        private Camera camera;

        private float farHeight, farWidth, nearHeight, nearWidth;

        public ShadowBox(Matrix4 lightViewMatrix, Camera camera)
        {
            this.lightViewMatrix = lightViewMatrix;
            this.camera = camera;
        }

        public void UpdateLightMatrix(Matrix4 lightViewMatrix) {
            this.lightViewMatrix = lightViewMatrix;
        }

        public void Update()
        {
            calculateWidthsAndHeights();

            Matrix4 rotation = calculateCameraRotationMatrix();
            Vector3 forwardVector = new Vector3(Vector4.Transform(FORWARD, rotation));

            Vector3 toFar = new Vector3(forwardVector);
            toFar *= SHADOW_DISTANCE;
            Vector3 toNear = forwardVector;
            toNear *= camera.ZNear;
            Vector3 centerNear = toNear + camera.GetViewPostion();
            Vector3 centerFar = toFar + camera.GetViewPostion();

            Vector4[] points = calculateFrustumVertices(rotation, forwardVector, centerNear, centerFar);

            Min = new Vector3(float.MaxValue);
            Max = new Vector3(float.MinValue);

            foreach (Vector4 point in points)
            {
                Min.X = MathF.Min(Min.X, point.X);
                Min.Y = MathF.Min(Min.Y, point.Y);
                Min.Z = MathF.Min(Min.Z, point.Z);
                Max.X = MathF.Max(Max.X, point.X);
                Max.Y = MathF.Max(Max.Y, point.Y);
                Max.Z = MathF.Max(Max.Z, point.Z);
            }
            Max.Z += OFFSET;
        }

        public Vector3 GetCenter()
        {
            float x = (Min.X + Max.X) / 2f;
            float y = (Min.Y + Max.Y) / 2f;
            float z = (Min.Z + Max.Z) / 2f;
            Vector4 center = new Vector4(x, y, z, 1.0f);
            Matrix4 inverted = lightViewMatrix.Inverted();
            return Vector4.Transform(center, inverted).Xyz;
        }

        public float GetWidth()  { return Max.X - Min.X; }
        public float GetHeight() { return Max.Y - Min.Y; }
        public float GetLength() { return Max.Z - Min.Z; }

        private Vector4[] calculateFrustumVertices(Matrix4 rotation,
            Vector3 forwardVector, Vector3 centerNear, Vector3 centerFar)
        {
            Vector3 upVector = new Vector3(Vector4.Transform(UP, rotation));
            Vector3 rightVector = Vector3.Cross(forwardVector, upVector);
            Vector3 downVector = -upVector;
            Vector3 leftVector = -rightVector;
            Vector3 farTop = centerFar + upVector * farHeight;
            Vector3 farBottom = centerFar + downVector * farHeight;
            Vector3 nearTop = centerNear + upVector * nearHeight;
            Vector3 nearBottom = centerNear + downVector * nearHeight;
            Vector4[] points = new Vector4[8];
            points[0] = CalculateLightSpaceFrustumCorner(farTop, rightVector, farWidth);
            points[1] = CalculateLightSpaceFrustumCorner(farTop, leftVector, farWidth);
            points[2] = CalculateLightSpaceFrustumCorner(farBottom, rightVector, farWidth);
            points[3] = CalculateLightSpaceFrustumCorner(farBottom, leftVector, farWidth);
            points[4] = CalculateLightSpaceFrustumCorner(nearTop, rightVector, nearWidth);
            points[5] = CalculateLightSpaceFrustumCorner(nearTop, leftVector, nearWidth);
            points[6] = CalculateLightSpaceFrustumCorner(nearBottom, rightVector, nearWidth);
            points[7] = CalculateLightSpaceFrustumCorner(nearBottom, leftVector, nearWidth);
            return points;
        }

        private Vector4 CalculateLightSpaceFrustumCorner(Vector3 startPoint, Vector3 direction, float width)
        {
            Vector3 point = startPoint + direction * width;
            Vector4 point4f = new Vector4(point, 1.0f);
            point4f = Vector4.Transform(point4f, lightViewMatrix);
            return point4f;
        }

        private Matrix4 calculateCameraRotationMatrix()
        {
            return Matrix4.CreateRotationY(camera.RotationY) *
                   Matrix4.CreateRotationX(camera.RotationX);
        }

        private void calculateWidthsAndHeights()
        {
            farWidth = (float)(SHADOW_DISTANCE * Math.Tan(camera.Fov));
            nearWidth = (float)(camera.ZNear * Math.Tan(camera.Fov));
            farHeight = farWidth / GetAspectRatio();
            nearHeight = nearWidth / GetAspectRatio();
        }

        private float GetAspectRatio() {
            return camera.Width / (float)camera.Height;
        }
    }
}
