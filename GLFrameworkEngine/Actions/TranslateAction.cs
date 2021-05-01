using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public class TranslateAction : ITransformAction
    {
        public GLTransform Transform { get; set; } = new GLTransform();

        public Vector3 Translation = Vector3.Zero;

        public bool UseGizmo = true;

        public GLScene.Axis ActiveAxis { get; set; }

        private bool m_hasSetMouseOffset = false;
        private Vector3 m_translateOffset = Vector3.Zero;

        public List<AxisAlignedBoundingBox> AxisOjects = new List<AxisAlignedBoundingBox>();

        public bool[] isSelected = new bool[3];

        public bool IsActive { get; set; }

        public TranslateAction(GLTransform transform)
        {
            Transform = transform;

            float boxLength = 2.0f + 1.0F;
            float boxHalfWidth = 0.25f;

            var xBounding = new AxisAlignedBoundingBox(new Vector3(0, -boxHalfWidth, -boxHalfWidth), new Vector3(boxLength, boxHalfWidth, boxHalfWidth));
            var yBounding = new AxisAlignedBoundingBox(new Vector3(-boxHalfWidth, 0, -boxHalfWidth), new Vector3(boxHalfWidth, boxLength, boxHalfWidth));
            var zBounding = new AxisAlignedBoundingBox(new Vector3(-boxHalfWidth, -boxHalfWidth, 0), new Vector3(boxHalfWidth, boxHalfWidth, boxLength));

            AxisOjects = new List<AxisAlignedBoundingBox>() {
              xBounding, yBounding, zBounding,
            };
        }

        private TranslateGizmo Renderer = new TranslateGizmo();

        public void Render(GLContext context) {
            Renderer.Render(context, Transform.Position, isSelected);
        }

        public int OnMouseDown(GLContext context, MouseEventInfo e) {
            IsActive = CheckSelectedAxes(context, new Vector2(e.Position.X, e.Position.Y));
            Console.WriteLine($"IsActive {IsActive}");
            m_hasSetMouseOffset = false;
            m_translateOffset = Vector3.Zero;

            return IsActive ? 1 : 0;
        }

        public int OnMouseUp(GLContext context, MouseEventInfo e) {
            IsActive = false;
            return 0;
        }

        public int OnMouseMove(GLContext context, MouseEventInfo e) {
            if (!IsActive)
                CheckSelectedAxes(context, new Vector2(e.X, e.Y));

            if (IsActive)
            {
                return DragMouse(context, new Vector2(e.X, e.Y));
            }
            return 0;
        }

        public int DragMouse(GLContext context, Vector2 point) {
            bool transformed = SetTransform(context, point);
            if (!transformed)
                return 1;

            foreach (var obj in context.Scene.GetSelected())
            {
                //obj.Transform = this.Transform;
                obj.Transform.UpdateMatrix(true);
            }
            return 1;
        }

        private bool CheckSelectedAxes(GLContext context, Vector2 point)
        {
            if (!UseGizmo)
                return true;

            var ray = context.PointScreenRay((int)point.X, (int)point.Y);

            var position = Transform.Position;
            var rotation = Transform.Rotation;

            CameraRay localRay = new CameraRay();
            localRay.Direction = Vector3.Transform(ray.Direction, rotation.Inverted());
            localRay.Origin = new Vector4(Vector3.Transform(ray.Origin.Xyz - position, rotation.Inverted()), localRay.Origin.W);

            List<float> results = new List<float>();

            for (int i = 0; i < AxisOjects.Count; i++)
            {
                isSelected[i] = false;

                if (GLMath.RayIntersectsAABB(localRay, AxisOjects[i].Min, AxisOjects[i].Max, out float d)) {
                    results.Add(d);
                    isSelected[i] = true;
                }
            }

            results.Sort((x, y) => x.CompareTo(y));

            if (results.Count == 0)
                return false;

            Vector3 localHitPoint = localRay.Origin.Xyz + (localRay.Direction * results[0]);
            var m_hitPoint = Vector3.Transform(localHitPoint, Quaternion.Identity) + Transform.Position;

            return true;
        }

        public bool SetTransform(GLContext context, Vector2 point)
        {
            var ray = context.PointScreenRay((int)point.X, (int)point.Y);

            var axis = GLScene.Axis.None;
            for (int i = 0; i < 3; i++)
            {
                if (isSelected[i] || !UseGizmo)
                    axis |= (GLScene.Axis)(i+1);
            }

            Console.WriteLine($"axis {axis}");

            var position = Transform.Position;
            var rotation = Transform.Rotation;
            var invRotation = rotation.Inverted();

            Vector3 dirToCamera = (position - context.Camera.TargetPosition).Normalized();

            Vector3 axisB = GetSelectedAxisVector3(axis);
            Vector3 axisA = Vector3.Cross(axisB, dirToCamera);

            Vector3 planeNormal = Vector3.Cross(axisA, axisB).Normalized();
            var translationPlane = new FustrumPlane(planeNormal, position);

            if (translationPlane.RayIntersectsPlane(ray, out float intersectDist)) {
                Vector3 hitPos = ray.Origin.Xyz + (ray.Direction * intersectDist);
                Vector3 localDelta = Vector3.Transform(hitPos - position, invRotation);

                Vector3 newPos = position;
                if (axis.HasFlag(GLScene.Axis.X))
                    newPos += Vector3.Transform(Vector3.UnitX, rotation) * localDelta.X;
                if (axis.HasFlag(GLScene.Axis.Y))
                    newPos += Vector3.Transform(Vector3.UnitY, rotation) * localDelta.Y;
                if (axis.HasFlag(GLScene.Axis.Z))
                    newPos += Vector3.Transform(Vector3.UnitZ, rotation) * localDelta.Z;

                Vector3 newPosDirToCamera = (newPos - context.Camera.TargetPosition).Normalized();
                float dot = Math.Abs(Vector3.Dot(planeNormal, newPosDirToCamera));

                if (dot < 0.02f)
                    return false;

                if (!m_hasSetMouseOffset)
                {
                    m_translateOffset = position - newPos;
                    m_hasSetMouseOffset = true;
                    return false;
                }

                var deltaTranslation = Vector3.Transform(newPos - position + m_translateOffset, rotation.Inverted());

                if (!axis.HasFlag(GLScene.Axis.X)) deltaTranslation.X = 0f;
                if (!axis.HasFlag(GLScene.Axis.Y)) deltaTranslation.Y = 0f;
                if (!axis.HasFlag(GLScene.Axis.Z)) deltaTranslation.Z = 0f;

                Transform.Position += Vector3.Transform(deltaTranslation, rotation);
            }
            else
            {
                // Our raycast missed the plane
                return false;
            }

            return true;
        }

        private Vector3 GetSelectedAxisVector3(GLScene.Axis axis)
        {
            switch (axis)
            {
                case GLScene.Axis.X: return Vector3.UnitX;
                case GLScene.Axis.Y: return Vector3.UnitY;
                case GLScene.Axis.Z: return Vector3.UnitZ;
                default:
                    return Vector3.UnitZ;
            }
        }
    }
}
