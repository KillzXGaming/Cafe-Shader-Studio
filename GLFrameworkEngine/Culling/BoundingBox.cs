using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    public class BoundingBox
    {
        /// <summary>
        /// The minimum point in the box.
        /// </summary>
        public Vector3 Min { get; set; }

        /// <summary>
        /// The maximum point in the box.
        /// </summary>
        public Vector3 Max { get; set; }

        /// <summary>
        /// Gets the center of the bounding box.
        /// </summary>
        public Vector3 GetCenter()
        {
            return new Vector3((Min + Max) * 0.5f);
        }

        /// <summary>
        /// Gets the extent of the bounding box.
        /// </summary>
        public Vector3 GetExtent()
        {
            return GetSize() * 0.5f;
        }

        /// <summary>
        /// Gets the size of the bounding box.
        /// </summary>
        public Vector3 GetSize()
        {
            return Max - Min;
        }

        //Vertices of the box (local space)
        private Vector3[] Vertices;

        //Pre transformed vertices (world space)
        private Vector3[] TransformedVertices;

        public BoundingBox() {
            Vertices = new Vector3[8];
        }

        public void Set(Vector4[] vertices)
        {
            Vector3 max = new Vector3(float.MinValue);
            Vector3 min = new Vector3(float.MaxValue);
            for (int i = 0; i < vertices.Length; i++)
            {
                max.X = MathF.Max(max.X, vertices[i].X);
                max.Y = MathF.Max(max.Y, vertices[i].Y);
                max.Z = MathF.Max(max.Z, vertices[i].Z);
                min.X = MathF.Min(min.X, vertices[i].X);
                min.Y = MathF.Min(min.Y, vertices[i].Y);
                min.Z = MathF.Min(min.Z, vertices[i].Z);
            }
            Min = min;
            Max = max;
            FromMinMax(min, max);
        }

        public bool IsInside(Vector3 position)
        {
            return (position.X >= Min.X && position.X <= Max.X) &&
                   (position.Y >= Min.Y && position.Y <= Max.Y) &&
                   (position.Z >= Min.Z && position.Z <= Max.Z);
        }

        public bool IsOverlapping(BoundingBox bounding)
        {
            if (Min.X <= bounding.Max.X && Max.X >= bounding.Min.X &&
                Min.Y <= bounding.Max.Y && Max.Y >= bounding.Min.Y &&
                Min.Z <= bounding.Max.Z && Max.Z >= bounding.Min.Z)
                return false;

            return true;
        }

        public Vector3[] GetVertices() {
            //Return transformed vertices if used
            return TransformedVertices != null ? TransformedVertices : Vertices;
        }

        /// <summary>
        /// Updates the current box points from the given transform.
        /// </summary>
        public void UpdateTransform(Matrix4 transform)
        {
           TransformedVertices = new Vector3[8];
            for (int i = 0; i < 8; i++)
                TransformedVertices[i] = Vector3.TransformPosition(Vertices[i], transform);
        }

        /// <summary>
        /// Creates a bounding box from a min and max vertex point.
        /// </summary>
        public static BoundingBox FromMinMax(Vector3 min, Vector3 max)
        {
            BoundingBox box = new BoundingBox();
            box.Min = min;
            box.Max = max;

            box.Vertices[0] = new Vector3(min.X, min.Y, max.Z);
            box.Vertices[1] = new Vector3(max.X, min.Y, max.Z);
            box.Vertices[2] = new Vector3(min.X, max.Y, max.Z);
            box.Vertices[3] = new Vector3(max.X, max.Y, max.Z);
            box.Vertices[4] = new Vector3(min.X, min.Y, min.Z);
            box.Vertices[5] = new Vector3(max.X, min.Y, min.Z);
            box.Vertices[6] = new Vector3(min.X, max.Y, min.Z);
            box.Vertices[7] = new Vector3(max.X, max.Y, min.Z);

            return box;
        }

        /// <summary>
        /// Gets the min and max vector values from a list of points fpr creatomg a bounding box.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public static void CalculateMinMax(List<Vector3> points, out Vector3 min, out Vector3 max)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                maxX = Math.Max(points[i].X, maxX);
                maxY = Math.Max(points[i].Y, maxY);
                maxZ = Math.Max(points[i].Z, maxZ);
                minX = Math.Min(points[i].X, minX);
                minY = Math.Min(points[i].Y, minY);
                minZ = Math.Min(points[i].Z, minZ);
            }
            min = new Vector3(minX, minY, minZ);
            max = new Vector3(maxX, maxY, maxZ);
        }
    }
}
