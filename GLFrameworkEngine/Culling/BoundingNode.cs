using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Repesents a node that can be detected from the camera fustrum.
    /// These are used for culling in the camera fustrum.
    /// </summary>
    public class BoundingNode
    {
        /// <summary>
        /// The children nodes used for additional cull searching. 
        /// </summary>
        public List<BoundingNode> Children = new List<BoundingNode>();

        /// <summary>
        /// The bounding box of the node.
        /// </summary>
        public BoundingBox Box { get; set; }

        /// <summary>
        /// The center of the bounding sphere.
        /// </summary>
        public Vector3 Center { get; set; }

        /// <summary>
        /// The radius of the bounding sphere.
        /// </summary>
        public float Radius { get; set; }

        private Vector3 transformedCenter;
        private float transformedRadius;

        /// <summary>
        /// Gets the center point of the bounding node in world coordinates used to place the sphere.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCenter() {
            return Center;
        }

        /// <summary>
        /// The bounding radius size for sphere detection in world coordinates.
        /// </summary>
        /// <returns></returns>
        public float GetRadius() {
            return Radius;
        }

        /// <summary>
        /// Updates the bounding radius and bounding boxes with the given transform.
        /// </summary>
        /// <param name="transform"></param>
        public void UpdateTransform(Matrix4 transform)
        {
            transformedCenter = Vector3.TransformPosition(Center, transform);
            transformedRadius = Radius;
            Box.UpdateTransform(transform);
        }

        /// <summary>
        /// Generates 8 sub bounding octrees inside the boudning node.
        /// </summary>
        public void CreateOctree()
        {
            Vector3 size = this.Box.GetSize();
            Vector3 half = size / 2.0f;
            Vector3 center = this.Box.Min + half;

            //Divide boundings into 8 child regions
            BoundingNode[] octrees = new BoundingNode[8];
            octrees[0] = new BoundingNode(Box.Min, center);
            octrees[1] = new BoundingNode(new Vector3(center.X, Box.Min.Y, Box.Min.Z), new Vector3(Box.Max.X, center.Y, center.Z));
            octrees[2] = new BoundingNode(new Vector3(center.X, Box.Min.Y, center.Z), new Vector3(Box.Max.X, center.Y, Box.Max.Z));
            octrees[3] = new BoundingNode(new Vector3(Box.Min.X, Box.Min.Y, center.Z), new Vector3(center.X, center.Y, Box.Max.Z));
            octrees[4] = new BoundingNode(new Vector3(Box.Min.X, center.Y, Box.Min.Z), new Vector3(center.X, Box.Max.Y, center.Z));
            octrees[5] = new BoundingNode(new Vector3(center.X, center.Y, Box.Min.Z), new Vector3(Box.Max.X, Box.Max.Y, center.Z));
            octrees[6] = new BoundingNode(center, Box.Max);
            octrees[7] = new BoundingNode(new Vector3(Box.Min.X, center.Y, center.Z), new Vector3(center.X, Box.Max.Y, Box.Max.Z));
            this.Children = octrees.ToList();
        }

        public BoundingNode()
        {
            Box = new BoundingBox();
            Center = Vector3.Zero;
            Radius = 1.0f;
        }

        public BoundingNode(Vector3 min, Vector3 max)
        {

        }
    }
}
