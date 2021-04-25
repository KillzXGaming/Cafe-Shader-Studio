using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    public class FustrumPlane
    {
        public Vector3 Normal;
        public float Distance;

        public FustrumPlane(Vector3 normal, float distance) {
            Normal = normal;
            Distance = distance;
        }

        public FustrumPlane(Vector3 normal, Vector3 pointOnPlane) {
            Normal = normal.Normalized();
            Distance = -Vector3.Dot(normal, pointOnPlane);
        }

        public bool RayIntersectsPlane(CameraRay ray, out float intersectDist)
        {
            float a = Vector3.Dot(ray.Direction, Normal);
            float num = -Vector3.Dot(ray.Origin.Xyz, Normal) - Distance;

            if (Math.Abs(a) < float.Epsilon) {
                intersectDist = 0f;
                return false;
            }
            intersectDist = num / a;
            return intersectDist > 0f;
        }
    }
}
