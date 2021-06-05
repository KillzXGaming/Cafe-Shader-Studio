using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class AxisAlignedBoundingBox
    {
        public Vector3 Max { get; set; }
        public Vector3 Min { get; set; }

        public Vector3 Center => (Max - Min) / 2f;

        public AxisAlignedBoundingBox(Vector3 min, Vector3 max) {
            Min = min;
            Max = max;
        }

        public void Draw(GLContext context)
        {
            BoundingBoxRender.Draw(context, Min, Max);
        }

        public bool Contains(Vector3 point) {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public static Vector3[] GetVertices(Vector3 Min, Vector3 Max)
        {
            Vector3[] corners = new Vector3[8];

            corners[0] = Min;
            corners[1] = new Vector3(Min.X, Min.Y, Max.Z);
            corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
            corners[3] = new Vector3(Min.X, Max.Y, Max.Z);
            corners[4] = new Vector3(Max.X, Min.Y, Min.Z);
            corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
            corners[6] = new Vector3(Max.X, Max.Y, Min.Z);
            corners[7] = Max;

            return corners;
        }
    }
}
