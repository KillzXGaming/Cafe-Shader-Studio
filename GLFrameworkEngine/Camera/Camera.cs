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
        /// Keyed values for animating a camera.
        /// </summary>
        public Dictionary<CameraAnimationKeys, float> AnimationKeys = new Dictionary<CameraAnimationKeys, float>();

        /// <summary>
        /// The speed of the camera used when zooming.
        /// </summary>
        public float ZoomSpeed { get; set; } = 1.0f;

        /// <summary>
        /// The speed of the camera used when padding.
        /// </summary>
        public float PanSpeed { get; set; } = 1.0f;

        /// <summary>
        /// The move speed of the camera used when using key movements.
        /// </summary>
        public float KeyMoveSpeed { get; set; } = 10.0f;

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
            get {
                if (AnimationKeys.ContainsKey(CameraAnimationKeys.FieldOfView))
                    return AnimationKeys[CameraAnimationKeys.FieldOfView];

                return _fov; }
            set
            {
                _fov = Math.Max(value, 0.01f);
                _fov = Math.Min(_fov, 3.14f);
            }
        }

        private float _znear = 1.0f;

        /// <summary>
        /// The z near value.
        /// </summary>
        public float ZNear
        {
            get {
                if (AnimationKeys.ContainsKey(CameraAnimationKeys.Near))
                    return AnimationKeys[CameraAnimationKeys.Near];

                return _znear; }
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
            get {
                if (AnimationKeys.ContainsKey(CameraAnimationKeys.Far))
                    return AnimationKeys[CameraAnimationKeys.Far];

                return _zfar; }
            set
            {
                _zfar = Math.Max(value, 10.0f);
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

        internal Vector3 _targetPosition;

        /// <summary>
        /// The position of the camera in world space.
        /// </summary>
        public Vector3 TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; }
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
        public float Distance
        {
            get { return Math.Abs(_targetPosition.Z); }
            set
            {
                _targetPosition.Z = value;
            }
        }

        internal float _targetDistance;

        /// <summary>
        /// The distance to the camera target
        /// </summary>
        public float TargetDistance
        {
            get { return _targetDistance; }
            set { _targetDistance = value; }
        }

        protected Matrix4 projectionMatrix;
        protected Matrix4 viewMatrix;
        protected Matrix3 invRotationMatrix;

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
        /// Gets or sets the inverse rotation matrix.
        /// </summary>
        public Matrix3 InverseRotationMatrix
        {
            get { return invRotationMatrix; }
            set { invRotationMatrix = value; }
        }

        /// <summary>
        /// Inverts the camera rotation controls on the X axis.
        /// </summary>
        public bool InvertRotationX { get; set; } = false;

        /// <summary>
        /// Inverts the camera rotation controls on the Y axis.
        /// </summary>
        public bool InvertRotationY { get; set; } = false;

        /// <summary>
        /// The factor of the camera fustrum on the X axis.
        /// </summary>
        public float FactorX => (2f * (float)Math.Tan(Fov * 0.5f) * AspectRatio) / Width;

        /// <summary>
        /// The factor of the camera fustrum on the Y axis.
        /// </summary>
        public float FactorY => (2f * (float)Math.Tan(Fov * 0.5f) * AspectRatio) / Height;

        /// <summary>
        /// The depth of the mouse cursor.
        /// </summary>
        public float Depth { get; set; }

        /// <summary>
        /// Resets the camera transform values.
        /// </summary>
        public void ResetTransform()
        {
            TargetPosition = new Vector3();
            RotationX = 0;
            RotationY = 0;
            TargetDistance = 0;
            UpdateMatrices();
        }

        /// <summary>
        /// Resets the viewport camera transform values.
        /// </summary>
        public void ResetViewportTransform()
        {
            RotationX = 0;
            RotationY = 0;
            if (Mode == CameraMode.Inspect)
            {
                TargetPosition = new OpenTK.Vector3(0, 1, 0);
                TargetDistance = 5;
            }
            else
            {
                TargetPosition = new OpenTK.Vector3(0, 1, 5);
                TargetDistance = 0;
            }
            UpdateMatrices();
        }

        /// <summary>
        /// Updates the view and projection matrices with current camera data.
        /// </summary>
        public void UpdateMatrices()
        {
            projectionMatrix = GetProjectionMatrix();
            viewMatrix = GetViewMatrix();
            ViewProjectionMatrix = viewMatrix * projectionMatrix;
            invRotationMatrix = Matrix3.CreateRotationX(-RotationX) *
                                     Matrix3.CreateRotationY(-RotationY);

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

        //Look at properties
        public bool RotationLookat { get; set; }

        /// <summary>
        /// Gets or sets the eye position for look at rotation type.
        /// </summary>
        public Vector3 EyePosition { get; set; }
        /// <summary>
        /// Gets or sets the Z rotation for look at rotation type.
        /// </summary>
        public float Twist { get; set; }

        /// <summary>
        /// Gets the calculated projection matrix.
        /// </summary>
        public Matrix4 GetProjectionMatrix()
        {
            if (IsOrthographic)
            {
                //Make sure the scale isn't negative or it would invert the viewport
                float scale = Math.Max((Distance + TargetDistance) / 1000.0f, 0.000001f);
                return Matrix4.CreateOrthographicOffCenter(-(Width * scale), Width * scale, -(Height * scale), Height * scale, -100000, 100000);
            }
            else
                return Matrix4.CreatePerspectiveFieldOfView(Fov, AspectRatio, ZNear, ZFar);
        }

        /// <summary>
        /// Gets the calculated view matrix.
        /// </summary>
        public Matrix4 GetViewMatrix()
        {
            var position = TargetPosition;
            var rotation = new Vector3(RotationX, RotationY, 0);
            var twist = this.Twist;
            var eye = this.EyePosition;
            var distance = this.TargetDistance;

            if (AnimationKeys.ContainsKey(CameraAnimationKeys.PositionX)) position.X = AnimationKeys[CameraAnimationKeys.PositionX];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.PositionY)) position.Y = AnimationKeys[CameraAnimationKeys.PositionY];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.PositionZ)) position.Z = AnimationKeys[CameraAnimationKeys.PositionZ];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.RotationX)) rotation.X = AnimationKeys[CameraAnimationKeys.RotationX];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.RotationY)) rotation.Y = AnimationKeys[CameraAnimationKeys.RotationY];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.RotationZ)) rotation.Z = AnimationKeys[CameraAnimationKeys.RotationZ];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.EyeX)) eye.X = AnimationKeys[CameraAnimationKeys.EyeX];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.EyeY)) eye.Y = AnimationKeys[CameraAnimationKeys.EyeY];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.EyeZ)) eye.Z = AnimationKeys[CameraAnimationKeys.EyeZ];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.Twist)) twist = AnimationKeys[CameraAnimationKeys.Twist];
            if (AnimationKeys.ContainsKey(CameraAnimationKeys.Distance)) distance = AnimationKeys[CameraAnimationKeys.Distance];

            var translationMatrix = Matrix4.CreateTranslation(-position);
            var rotationMatrix = Matrix4.CreateRotationY(rotation.Y) * Matrix4.CreateRotationX(rotation.X);
            if (RotationLookat)
                rotationMatrix = Matrix4.LookAt(position, eye, new Vector3(0, 1, 0)) * Matrix4.CreateRotationZ(twist);

            var distanceMatrix = Matrix4.CreateTranslation(0, 0, -distance);
            return translationMatrix * rotationMatrix * distanceMatrix;
        }

        public void ResetAnimations()
        {
            RotationLookat = false;
            AnimationKeys.Clear();
            UpdateMatrices();
        }

        public void SetKeyframe(CameraAnimationKeys keyType, float value)
        {
            if (AnimationKeys.ContainsKey(keyType))
                AnimationKeys[keyType] = value;
            else
                AnimationKeys.Add(keyType, value);
        }

        public void FrameBoundingSphere(Vector4 boundingSphere) {
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

            translation.X = center.X;
            translation.Y = center.Y;

            float distanceOffset = offset / minFov;
            translation.Z =  (distance + distanceOffset);

            if (Mode == CameraMode.Inspect)
            {
                TargetPosition = new Vector3(translation.X, translation.Y, 0);
                TargetDistance = translation.Z;
            }
            else
                TargetPosition = translation;
        }

        /// <summary>
        /// Gets the 3D coordinates for the given mouse XY coordinates and depth value.
        /// </summary>
        /// <returns></returns>
        public Vector3 CoordFor(int x, int y, float depth)
        {
            Vector3 vec;

            Vector2 normCoords = OpenGLHelper.NormMouseCoords(x, Height - y, Width, Height);

            Vector3 cameraPosition = TargetPosition + invRotationMatrix.Row2 * Distance;

            vec.X = (normCoords.X * depth) * FactorX;
            vec.Y = (normCoords.Y * depth) * FactorY;

            vec.Z = depth - Distance;

            return -cameraPosition + Vector3.Transform(invRotationMatrix, vec);
        }

        public Camera()
        {
            UpdateMode();
        }

        private void UpdateMode()
        {
            if (Mode == CameraMode.Inspect)
                Controller = new InspectCameraController(this);
            else if (Mode == CameraMode.FlyAround)
                Controller = new FlyCameraController(this);
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
            FlyAround,
            Inspect,
        }
    }

    public class FlyCameraController : ICameraController
    {
        private Camera _camera;

        private float rotFactorX => _camera.InvertRotationX ? -0.002f : 0.002f;
        private float rotFactorY => _camera.InvertRotationY ? -0.002f : 0.002f;

        public FlyCameraController(Camera camera)
        {
            _camera = camera;
        }

        public void MouseClick(MouseEventInfo e, KeyEventInfo k) { }

        public void MouseMove(MouseEventInfo e, KeyEventInfo k, Vector2 previousLocation)
        {
            var position = new Vector2(e.X, e.Y);
            var movement = position - previousLocation;

            if ((e.LeftButton == ButtonState.Pressed ||
                e.RightButton == ButtonState.Pressed) && !_camera.LockRotation)
            {
                if (k.KeyCtrl)
                {
                    float delta = ((float)movement.Y * -5 * Math.Min(0.01f, _camera.Depth / 500f));
                    Vector3 vec;
                    vec.X = 0;
                    vec.Y = 0;
                    vec.Z = delta;

                    _camera.TargetPosition += Vector3.Transform(_camera.InverseRotationMatrix, vec);
                }
                else
                {
                    if (!k.IsKeyDown("y"))
                        _camera.RotationX += movement.Y * rotFactorX;
                    if (!k.IsKeyDown("x"))
                        _camera.RotationY += movement.X * rotFactorY;

                    //Reset direction
                    _camera.Direction = Camera.FaceDirection.Any;
                }
            }
            if (e.LeftButton == ButtonState.Pressed)
            {

            }
        }

        public void MouseWheel(MouseEventInfo e, KeyEventInfo k)
        {
            if (k.KeyShift)
            {
                float amount = e.Delta * 0.1f;
                _camera.KeyMoveSpeed += amount;
            }
            else
            {
                float delta = (e.Delta * Math.Min(k.KeyShift ? 0.04f : 0.01f, _camera.Depth / 500f));

                Vector3 vec;

                Vector2 normCoords = OpenGLHelper.NormMouseCoords(e.X, e.Y, _camera.Width, _camera.Height);

                vec.X = (-normCoords.X * delta) * _camera.FactorX;
                vec.Y = (normCoords.Y * delta) * _camera.FactorY;
                vec.Z = delta;

                _camera.TargetPosition -= Vector3.Transform(_camera.InverseRotationMatrix, vec);
            }
        }

        public void KeyPress(KeyEventInfo e)
        {
            float movement = 0.2f * _camera.KeyMoveSpeed;
            Vector3 vec = Vector3.Zero;

            if (e.KeyShift)
                movement *= 2;

            if (e.IsKeyDown(KeyController.View3D.MOVE_FORWARD))
                vec.Z -= movement;
            if (e.IsKeyDown(KeyController.View3D.MOVE_BACK))
                vec.Z += movement;
            if (e.IsKeyDown(KeyController.View3D.MOVE_LEFT))
                vec.X -= movement;
            if (e.IsKeyDown(KeyController.View3D.MOVE_RIGHT))
                vec.X += movement;

            if (e.IsKeyDown(KeyController.View3D.MOVE_DOWN))
                vec.Y -= movement;
            else if (e.IsKeyDown(KeyController.View3D.MOVE_UP))
                vec.Y += movement;

            float UP = 0;

            _camera.TargetPosition += Vector3.Transform(_camera.InverseRotationMatrix, vec) + Vector3.UnitY * UP;
        }
    }

    public class InspectCameraController : ICameraController
    {
        private Camera _camera;

        private float rotFactorX => _camera.InvertRotationX ? -0.01f : 0.01f;
        private float rotFactorY => _camera.InvertRotationY ? -0.01f : 0.01f;

        public InspectCameraController(Camera camera)
        {
            _camera = camera;
        }

        public void MouseClick(MouseEventInfo e, KeyEventInfo k)
        {
            if (k.KeyCtrl && e.RightButton == ButtonState.Pressed && _camera.Depth != _camera.ZFar)
            {
               // _camera.TargetPosition = -_camera.CoordFor(e.X, e.Y, _camera.Depth);
            }
        }

        public void MouseMove(MouseEventInfo e, KeyEventInfo k, Vector2 previousLocation)
        {
            var position = e.Position;
            var movement = new Vector2(position.X, position.Y) - previousLocation;

            if (e.RightButton == ButtonState.Pressed && !_camera.LockRotation)
            {
                if (k.KeyCtrl)
                {
                    _camera._targetDistance *= 1 - movement.Y * -5 * 0.001f;
                }
                else
                {
                    if (!k.IsKeyDown("y"))
                        _camera.RotationX += movement.Y * rotFactorX;
                    if (!k.IsKeyDown("x"))
                        _camera.RotationY += movement.X * rotFactorY;

                    //Reset direction
                    _camera.Direction = Camera.FaceDirection.Any;
                }
            }
            if (e.LeftButton == ButtonState.Pressed)
            {
                Pan(movement.X * _camera.PanSpeed, movement.Y * _camera.PanSpeed);
            }

            _camera.UpdateMatrices();
        }

        public void MouseWheel(MouseEventInfo e, KeyEventInfo k)
        {
            if (k.KeyCtrl)
            {
                float delta = -e.Delta * Math.Min(0.1f, _camera.Depth / 500f);

                delta *= _camera.TargetDistance;

                Vector2 normCoords = OpenGLHelper.NormMouseCoords(e.X, e.Y, _camera.Width, _camera.Height);

                Vector3 vec = _camera.InverseRotationMatrix.Row0 * -normCoords.X * delta * _camera.FactorX +
                              _camera.InverseRotationMatrix.Row1 * normCoords.Y * delta * _camera.FactorY +
                              _camera.InverseRotationMatrix.Row2 * delta;

                _camera.TargetPosition += vec;
            }
            else
            {
                Zoom(e.Delta * 0.1f * _camera.ZoomSpeed, true);
            }
        }

        public void KeyPress(KeyEventInfo e)
        {

        }

        private void Pan(float xAmount, float yAmount, bool scaleByDistanceToOrigin = true)
        {
            // Find the change in normalized screen coordinates.
            float deltaX = -xAmount / _camera.Width;
            float deltaY = yAmount / _camera.Height;

            if (scaleByDistanceToOrigin)
            {
                // Translate the camera based on the distance from the target and field of view.
                // Objects will "follow" the mouse while panning.
                deltaY *= ((float)Math.Sin(_camera.Fov) * _camera._targetDistance);
                deltaX *= ((float)Math.Sin(_camera.Fov) * _camera._targetDistance);
            }

            Matrix3 mtx = _camera.InverseRotationMatrix;
            // Regular panning.
            _camera._targetPosition += mtx.Row1 * deltaY;
            _camera._targetPosition += mtx.Row0 * deltaX;

            _camera.UpdateMatrices();
        }

        private void Zoom(float amount, bool scaleByDistanceToOrigin)
        {
            // Increase zoom speed when zooming out. 
            float zoomScale = 1;
            if (scaleByDistanceToOrigin && _camera._targetDistance > 0)
                zoomScale *= _camera._targetDistance;
            else
                zoomScale = 1f;

            _camera._targetDistance -= amount * zoomScale;

            _camera.UpdateMatrices();
        }
    }
}
