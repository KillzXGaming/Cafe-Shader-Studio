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
        public class Plane
        {
            public float a, b, c, d;

            public Vector3 n => new Vector3(a, b, c);

            public void Normalize()
            {
                float mag = (float)Math.Sqrt(
                                a * a +
                                b * b +
                                c * c);

                a = a / mag;
                b = b / mag;
                c = c / mag;
                d = d / mag;
            }

            public Halfspace SideOfPlane(Vector3 pt)
            {
                float dist = DistanceToPoint(this, pt);
                if (dist < 0) return Halfspace.NEGATIVE;
                if (dist > 0) return Halfspace.POSITIVE;
                return Halfspace.ON_PLANE;
            }
        }

        static Plane[] Planes;

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
                case Fustrum.IN:
                    return true;
                case Fustrum.OUT:
                    return false;
                case Fustrum.INTERSECT: //Do bounding box detection
                    var boxFustrum = ContainsBox(Planes, bounding.Box);
                    return boxFustrum != Fustrum.OUT;
            }

            //We could do search trees here but the current models don't use them

            return false;
        }

        /// <summary>
        /// Checks if the given sphere is contained within the plane fustrum.
        /// </summary>
        static Fustrum ContainsSphere(
            Plane[] planes, Vector3 center, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                float dist = Vector3.Dot(center, planes[i].n) + planes[i].d;
                if (dist < -radius)
                    return Fustrum.OUT;

                if ((float)Math.Abs(dist) < radius)
                    return Fustrum.INTERSECT;
            }
            return Fustrum.IN;
        }

        /// <summary>
        /// Checks if the given bounding box is contained within the plane fustrum.
        /// </summary>
        static Fustrum ContainsBox(Plane[] planes, BoundingBox box)
        {
            int iTotalIn = 0;

            var vertices = box.GetVertices();
            for (int p = 0; p < 6; p++)
            {
                int iInCount = 8;
                int iPtIn = 1;

                for (int i = 0; i < 8; ++i)
                {
                    // test this point against the planes
                    if (planes[p].SideOfPlane(vertices[i]) == Halfspace.NEGATIVE)
                    {
                        iPtIn = 0;
                        --iInCount;
                    }
                }

                // were all the points outside of plane p?
                if (iInCount == 0)
                    return Fustrum.OUT;

                // check if they were all on the right side of the plane
                iTotalIn += iPtIn;
            }

            if (iTotalIn == 6)
                return Fustrum.IN;

            return Fustrum.INTERSECT;
        }

        public static Plane[] CreateCameraFustrum(Matrix4 m)
        {
            Plane[] planes = new Plane[6];
            for (int i = 0; i < 6; i++)
                planes[i] = new Plane();

            //Near
            planes[0].a = m[0, 3] + m[0, 2];
            planes[0].b = m[1, 3] + m[1, 2];
            planes[0].c = m[2, 3] + m[2, 2];
            planes[0].d = m[3, 3] + m[3, 2];

            //Far
            planes[1].a = m[0, 3] - m[0, 2];
            planes[1].b = m[1, 3] - m[1, 2];
            planes[1].c = m[2, 3] - m[2, 2];
            planes[1].d = m[3, 3] - m[3, 2];

            //Left
            planes[2].a = m[0, 3] - m[0, 0];
            planes[2].b = m[1, 3] - m[1, 0];
            planes[2].c = m[2, 3] - m[2, 0];
            planes[2].d = m[3, 3] - m[3, 0];

            //Right
            planes[3].a = m[0, 3] + m[0, 0];
            planes[3].b = m[1, 3] + m[1, 0];
            planes[3].c = m[2, 3] + m[2, 0];
            planes[3].d = m[3, 3] + m[3, 0];

            //Up
            planes[4].a = m[0, 3] - m[0, 1];
            planes[4].b = m[1, 3] - m[1, 1];
            planes[4].c = m[2, 3] - m[2, 1];
            planes[4].d = m[3, 3] - m[3, 1];

            //Down
            planes[5].a = m[0, 3] + m[0, 1];
            planes[5].b = m[1, 3] + m[1, 1];
            planes[5].c = m[2, 3] + m[2, 1];
            planes[5].d = m[3, 3] + m[3, 1];

            for (int i = 0; i < 6; i++)
                planes[i].Normalize();
            return planes;
        }

        public enum Fustrum
        {
            IN,
            OUT,
            INTERSECT,
        }

        public enum Halfspace
        {
            NEGATIVE = -1,
            ON_PLANE = 0,
            POSITIVE = 1,
        }

        static float DistanceToPoint(Plane plane, Vector3 pt)
        {
            return plane.a * pt.X + plane.b * pt.Y + plane.c * pt.Z + plane.d;
        }
    }
}
