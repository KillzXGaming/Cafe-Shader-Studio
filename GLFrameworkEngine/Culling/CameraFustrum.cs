using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    // https://www.flipcode.com/archives/Frustum_Culling.shtml

    /// <summary>
    /// Detects if objects are within the given camera's fustrum
    /// </summary>
    public class CameraFustrum
    {
        static Vector4[] Planes;
        static BoundingBox AABB = new BoundingBox();

        /// <summary>
        /// Updates the fustrum planes using the given control's camera and projection matricies.
        /// This must be called each time the camera is updated.
        /// </summary>
        /// <param name="camera"></param>
        public static void UpdateCamera(Camera camera) {
            Planes = CreateCameraFustrum(camera.ViewMatrix * camera.ProjectionMatrix);
        }

        /// <summary>
        /// Determines if the given bounding node is within the current camera fustrum.
        /// </summary>
        public static bool CheckIntersection(Camera camera, BoundingNode bounding)
        {
            if (Planes == null) UpdateCamera(camera);

            //Check sphere detection
            var sphereFustrum = ContainsSphere(Planes,
                bounding.GetCenter(),
                bounding.GetRadius());

            switch (sphereFustrum)
            {
                case Fustrum.FULL:
                    return true;
                case Fustrum.NONE: //Check the box anyways atm to be sure
                case Fustrum.PARTIAL: //Do bounding box detection
                    var boxFustrum = ContainsBox(Planes, bounding.Box);
                    if (boxFustrum != Fustrum.NONE)
                        return true;
                    else
                        break;
            }

            foreach (var child in bounding.Children) {
                bool hasIntersection = CheckIntersection(camera, child);
                if (hasIntersection)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the given sphere is contained within the plane fustrum.
        /// </summary>
        static Fustrum ContainsSphere(Vector4[] planes, Vector3 center, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                float dist = Vector3.Dot(center, planes[i].Xyz) + planes[i].W;
                if (dist < -radius)
                    return Fustrum.NONE;

                if (MathF.Abs(dist) < radius)
                    return Fustrum.PARTIAL;
            }
            return Fustrum.FULL;
        }

        /// <summary>
        /// Checks if the given bounding box is contained within the plane fustrum.
        /// </summary>
        static Fustrum ContainsBox(Vector4[] planes, BoundingBox box)
        {
            Fustrum finalResult = Fustrum.FULL;
            for (int p = 0; p < 6; p++)
            {
                var result = TestIntersct(planes[p],
                    box.GetCenter(), box.GetExtent());

                if (result == Fustrum.NONE)
                    return Fustrum.NONE;

                if (result == Fustrum.PARTIAL)
                    finalResult = Fustrum.PARTIAL;
            }
            return finalResult;
        }

        static Fustrum TestIntersct(Vector4 plane, Vector3 center, Vector3 extent)
        {
            float d = Vector3.Dot(center, plane.Xyz) + plane.W;
            float n = extent.X * MathF.Abs(plane.X) +
                      extent.Y * MathF.Abs(plane.Y) +
                      extent.Z * MathF.Abs(plane.Z);

            if (d - n >= 0)
                return Fustrum.FULL;
            if (d + n > 0)
                return Fustrum.PARTIAL;
            return Fustrum.NONE;
        }

        public static Vector4[] CreateCameraFustrum(Matrix4 m, bool normalize = true)
        {
            Vector4[] planes = new Vector4[6];
            for (int i = 0; i < 6; i++)
                planes[i] = new Vector4();

            //Left
            planes[0] = m.Column3 + m.Column0;
            //Right
            planes[1] = m.Column3 - m.Column0;
            //Up
            planes[2] = m.Column3 - m.Column1;
            //Down
            planes[3] = m.Column3 + m.Column1;
            //Near
            planes[4] = m.Column3 + m.Column2;
            //Far
            planes[5] = m.Column3 - m.Column2;

            if (normalize)
            {
                for (int i = 0; i < 6; i++)
                    planes[i] = Vector4.Normalize(planes[i]);
            }

            AABB.Set(planes);

            return planes;
        }

        public enum Fustrum
        {
            FULL,
            NONE,
            PARTIAL,
        }
    }
}
