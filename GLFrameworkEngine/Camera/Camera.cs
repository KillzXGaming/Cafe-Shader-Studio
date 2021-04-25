using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class Camera
    {
        /// <summary>
        /// The speed of the camera used when zooming.
        /// </summary>
        public float ZoomSpeed { get; set; } = 1.0f;

        /// <summary>
        /// The speed of the camera used when padding.
        /// </summary>
        public float PanSpeed { get; set; } = 1.0f;

        /// <summary>
        /// The width of the camera fustrum.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the camera fustrum.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The aspect ratio of the camera fustrum.
        /// </summary>
        public float AspectRatio => (float)Width / Height;

        /// <summary>
        /// The field of view in degrees.
        /// </summary>
        public float FovDegrees
        {
            get { return Fov * STMath.Rad2Deg; }
            set { Fov = value * STMath.Deg2Rad; }
        }

        private float _fov = 45 * STMath.Deg2Rad;

        /// <summary>
        /// The field of view in radians.
        /// </summary>
        public float Fov
        {
            get { return _fov; }
            set
            {
                _fov = Math.Max(value, 0.01f);
                _fov = Math.Min(_fov, 3.14f);
            }
        }

        private float _znear = 0.1f;

        /// <summary>
        /// The z near value.
        /// </summary>
        public float ZNear
        {
            get { return _znear; }
            set
            {
                _znear = Math.Max(value, 0.001f);
                _znear = Math.Min(_znear, 1.001f);
            }
        }

        private float _zfar = 100000.0f;

        /// <summary>
        /// The z far value.
        /// </summary>
        public float ZFar
        {
            get { return _zfar; }
            set
            {
                _zfar = Math.Max(value, 1.0f);
            }
        }

        /// <summary>
        /// The rotation of the camera on the X axis.
        /// </summary>
        public float RotationX = 0;

        /// <summary>
        /// The rotation of the camera on the Y axis.
        /// </summary>
        public float RotationY = 0;

        /// <summary>
        /// Locks the camera state to prevent rotations.
        /// </summary>
        public bool LockRotation { get; set; } = false;

        internal Vector3 _translation;

        /// <summary>
        /// The position of the camera in world space.
        /// </summary>
        public Vector3 Translation
        {
            get { return _translation; }
            set { _translation = value; }
        }

        /// <summary>
        /// The controller of the camera to handle user movement.
        /// </summary>
        public ICameraController Controller { get; set; }

        /// <summary>
        /// Toggles orthographic projection in the camera.
        /// </summary>
        public bool IsOrthographic { get; set; }

        /// <summary>
        /// Gets the distance of the camera.
        /// </summary>
        public float Distance => Math.Abs(Translation.Z);

        protected Matrix4 projectionMatrix;
        protected Matrix4 viewMatrix;

        /// <summary>
        /// Gets the model matrix of the camera.
        /// </summary>
        public Matrix4 ModelMatrix { get; set; } = Matrix4.Identity;

        /// <summary>
        /// Gets the combined view projection matrix of the camera.
        /// </summary>
        public Matrix4 ViewProjectionMatrix { get; private set; }

        /// <summary>
        /// Gets or sets the projection matrix.
        /// </summary>
        public Matrix4 ProjectionMatrix
        {
            get { return projectionMatrix; }
            set { projectionMatrix = value; }
        }

        /// <summary>
        /// Gets or sets the view matrix.
        /// </summary>
        public Matrix4 ViewMatrix
        {
            get { return viewMatrix; }
            set { viewMatrix = value; }
        }

        /// <summary>
        /// Updates the view and projection matrices with current camera data.
        /// </summary>
        public void UpdateTransform()
        {
            projectionMatrix = GetProjectionMatrix();
            viewMatrix = GetViewMatrix();
            ViewProjectionMatrix = viewMatrix * projectionMatrix;

            CameraFustrum.UpdateCamera(this);
        }

        private CameraMode cameraMode = CameraMode.Inspect;

        /// <summary>
        /// Sets and updates the camera controller mode.
        /// </summary>
        public CameraMode Mode
        {
            get { return cameraMode; }
            set
            {
                cameraMode = value;
                UpdateMode();
            }
        }

        private FaceDirection faceDirection = FaceDirection.Any;

        /// <summary>
        /// Gets and sets the current camera direction being faced.
        /// </summary>
        public FaceDirection Direction
        {
            get { return faceDirection; }
            set
            {
                faceDirection = value;
                UpdateDirection();
            }
        }

        public Matrix4 GetProjectionMatrix()
        {
            if (IsOrthographic)
            {
                float scale = Distance / 1000.0f;
                return Matrix4.CreateOrthographicOffCenter(-(Width * scale), Width * scale, -(Height * scale), Height * scale, -100000, 100000);
            }
            else
                return Matrix4.CreatePerspectiveFieldOfView(Fov, AspectRatio, ZNear, ZFar);
        }

        public Matrix4 GetViewMatrix()
        {
            var translationMatrix = Matrix4.CreateTranslation(Translation.X, -Translation.Y, Translation.Z);
            var rotationMatrix = Matrix4.CreateRotationY(RotationY) * Matrix4.CreateRotationX(RotationX);
            return rotationMatrix * translationMatrix;
        }

        public void FrameBoundingSphere(Vector4 boundingSphere)
        {
            FrameBoundingSphere(boundingSphere.Xyz, boundingSphere.W, 0);
        }

        public void FrameBoundingSphere(Vector3 center, float radius, float offset)
        {
            // Find the min to avoid clipping for non square aspect ratios.
            float fovHorizontal = (float)(2 * Math.Atan(Math.Tan(Fov / 2) * AspectRatio));
            float minFov = Math.Min(Fov, fovHorizontal);

            // Calculate the height of a right triangle using field of view and the sphere radius.
            float distance = radius / (float)Math.Tan(minFov / 2.0f);

            Vector3 translation = Vector3.Zero;

            translation.X = -center.X;
            translation.Y = center.Y;

            float distanceOffset = offset / minFov;
            translation.Z = -1 * (distance + distanceOffset);

            Translation = translation;
        }

        public Camera()
        {
            UpdateMode();
        }

        private void UpdateMode()
        {
            if (Mode == CameraMode.Inspect)
                Controller = new InspectCameraController(this);
            else if (Mode == CameraMode.Walk)
                Controller = new WalkCameraController(this);
            else
                throw new Exception($"Invalid camera mode! {Mode}");
        }

        private void UpdateDirection()
        {
            switch (faceDirection)
            {
                case FaceDirection.Top:
                    RotationX = 1.570796f;
                    RotationY = 0.0f;
                    break;
                case FaceDirection.Bottom:
                    RotationX = -1.570796f;
                    RotationY = 0.0f;
                    break;
                case FaceDirection.Front:
                    RotationX = 0.0f;
                    RotationY = 0.0f;
                    break;
                case FaceDirection.Back:
                    RotationX = 0.0f;
                    RotationY = 3.14159f;
                    break;
                case FaceDirection.Left:
                    RotationX = 0.0f;
                    RotationY = 1.570796f;
                    break;
                case FaceDirection.Right:
                    RotationX = 0.0f;
                    RotationY = -1.570796f;
                    break;
            }
        }

        public enum FaceDirection
        {
            Any,
            Top,
            Bottom,
            Front,
            Back,
            Left,
            Right,
        }

        public enum CameraMode
        {
            Walk,
            Inspect,
        }
    }


    public class WalkCameraController : ICameraController
    {
        private Camera _camera;

        public WalkCameraController(Camera camera)
        {
            _camera = camera;
        }

        public void MouseClick(MouseEventInfo e) { }

        public void MouseMove(MouseEventInfo e, Vector2 previousLocation)
        {
            var position = new Vector2(e.X, e.Y);
            var movement = position - previousLocation;

            if (e.RightButton == ButtonState.Pressed && !_camera.LockRotation)
            {
                _camera.RotationX += movement.Y / 100f;
                _camera.RotationY += movement.X / 100f;

                //Reset direction
                _camera.Direction = Camera.FaceDirection.Any;
            }
        }

        public void MouseWheel(MouseEventInfo e)
        {

        }

        public void KeyPress(KeyEventInfo e)
        {

        }
    }

    public class InspectCameraController : ICameraController
    {
        private Camera _camera;

        public InspectCameraController(Camera camera)
        {
            _camera = camera;
        }

        public void MouseClick(MouseEventInfo e)
        {

        }

        public void MouseMove(MouseEventInfo e, Vector2 previousLocation)
        {
            var position = new Vector2(e.X, e.Y);
            var movement = position - previousLocation;

            if (e.RightButton == ButtonState.Pressed && !_camera.LockRotation)
            {
                _camera.RotationX += movement.Y / 100f;
                _camera.RotationY += movement.X / 100f;

                //Reset direction
                _camera.Direction = Camera.FaceDirection.Any;
            }
            if (e.LeftButton == ButtonState.Pressed)
            {
                Pan(movement.X * _camera.PanSpeed, movement.Y * _camera.PanSpeed);
            }

            _camera.UpdateTransform();
        }

        public void MouseWheel(MouseEventInfo e)
        {
            Zoom((e.Delta) * 0.1f * _camera.ZoomSpeed, true);
        }

        public void KeyPress(KeyEventInfo e)
        {
            switch (e.KeyChar)
            {
                case 'w': break;
                case 'a': break;
                case 's': break;
                case 'd': break;
            }
        }

        private void Pan(float xAmount, float yAmount, bool scaleByDistanceToOrigin = true)
        {
            // Find the change in normalized screen coordinates.
            float deltaX = xAmount / _camera.Width;
            float deltaY = yAmount / _camera.Height;

            if (scaleByDistanceToOrigin)
            {
                // Translate the camera based on the distance from the origin and field of view.
                // Objects will "follow" the mouse while panning.
                _camera._translation.Y += deltaY * ((float)Math.Sin(_camera.Fov) * _camera._translation.Length);
                _camera._translation.X += deltaX * ((float)Math.Sin(_camera.Fov) * _camera._translation.Length);
            }
            else
            {
                // Regular panning.
                _camera._translation.Y += deltaY;
                _camera._translation.X += deltaX;
            }
            _camera.UpdateTransform();
        }

        private void Zoom(float amount, bool scaleByDistanceToOrigin)
        {
            // Increase zoom speed when zooming out. 
            float zoomScale = 1;
            if (scaleByDistanceToOrigin)
                zoomScale *= _camera.Distance;
            else
                zoomScale = 200;

            _camera._translation.Z += amount * zoomScale;

            _camera.UpdateTransform();
        }
    }
}
