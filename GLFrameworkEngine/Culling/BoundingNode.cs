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
            return transformedCenter;
        }

        /// <summary>
        /// The bounding radius size for sphere detection in world coordinates.
        /// </summary>
        /// <returns></returns>
        public float GetRadius() {
            return transformedRadius;
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

        public BoundingNode()
        {
            Box = new BoundingBox();
            Center = Vector3.Zero;
            Radius = 1.0f;
        }
    }
}
