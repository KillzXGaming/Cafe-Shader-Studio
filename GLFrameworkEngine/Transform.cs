using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTransform
    {
        /// <summary>
        /// Gets or sets the position of the bone in world space.
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero;

        /// <summary>
        /// Gets or sets the scale of the bone in world space.
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.One;

        /// <summary>
        /// Gets or sets the rotation of the bone in world space.
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Gets or sets the <see cref="Rotation"/> using euler method in radians. 
        /// </summary>
        public Vector3 RotationEuler
        {
            get { return GLMath.ToEulerAngles(Rotation); }
            set { Rotation = GLMath.FromEulerAngles(value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Rotation"/> using euler method in degrees. 
        /// </summary>
        public Vector3 RotationEulerDegrees
        {
            get { return RotationEuler * STMath.Rad2Deg; }
            set { RotationEuler = value * STMath.Deg2Rad; }
        }

        /// <summary>
        /// Determines to update the transform or not.
        /// </summary>
        public bool UpdateTransform = true;

        /// <summary>
        /// Gets or sets the calculated transform matrix.
        /// </summary>
        public Matrix4 TransformMatrix { get; set; } = Matrix4.Identity;

        /// <summary>
        /// Updates the TransformMatrix from the current position, scale and rotation values.
        /// </summary>
        public void UpdateMatrix(bool forceUpdate = false)
        {
            if (!UpdateTransform && !forceUpdate)
                return;

            var translationMatrix = Matrix4.CreateTranslation(Position);
            var rotationMatrix = Matrix4.CreateFromQuaternion(Rotation);
            var scaleMatrix = Matrix4.CreateScale(Scale);
            TransformMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            UpdateTransform = false;
        }
    }
}
