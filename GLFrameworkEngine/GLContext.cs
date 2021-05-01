using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class GLContext
    {
        public Framebuffer ScreenBuffer { get; set; }

        public ColorPicker ColorPicker = new ColorPicker();

        public GLScene Scene = new GLScene();

        public int Width { get; set; }
        public int Height { get; set; }

        public static float PreviewScale { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the mouse position after a mouse down event.
        /// </summary>
        public Vector2 MouseOrigin { get; set; }

        /// <summary>
        /// Gets or sets the current mouse postion.
        /// </summary>
        public Vector2 CurrentMousePoint = Vector2.Zero;

        /// <summary>
        /// Gets or sets the offset from the mouse origin.
        /// </summary>
        public Vector2 MouseOffset => CurrentMousePoint - MouseOrigin;

        /// <summary>
        /// Determines to enable SRGB or not for the current context.
        /// </summary>
        public bool UseSRBFrameBuffer;

        public Camera Camera { get; set; }

        public CameraRay PointScreenRay() => CameraRay.PointScreenRay((int)CurrentMousePoint.X, (int)CurrentMousePoint.Y, Camera);
        public CameraRay PointScreenRay(int x, int y) => CameraRay.PointScreenRay(x, y, Camera);

        public Vector2 ScreenCoordFor(Vector3 coord)
        {
            Vector3 vec = Vector3.Project(coord, 0, 0, Width, Height, -1, 1, Camera.ViewMatrix * Camera.ProjectionMatrix);
            return new Vector2((int)vec.X, Height - (int)(vec.Y));
        }

        public Vector3 GetScreenPoint(Vector3 position)
        {
            Vector4 n = Vector4.Transform(new Vector4(position, 1), Camera.ViewMatrix * Camera.ProjectionMatrix);
            n.X /= n.W;
            n.Y /= n.W;
            n.Z /= n.W;
            return n.Xyz;
        }

        public void OnMouseDown(MouseEventInfo e) {
            MouseOrigin = new Vector2(e.X, e.Y);
        }

        public void OnMouseMove(MouseEventInfo e) {
            CurrentMousePoint = new Vector2(e.X, Height - e.Y);
        }

        public bool IsShaderActive(ShaderProgram shader) {
            return shader != null && shader.program == CurrentShader.program;
        }

        private ShaderProgram shader;
        public ShaderProgram CurrentShader
        {
            get { return shader; }
            set
            {
                if(value == null)
                {
                    GL.UseProgram(0);
                    return;
                }

                //Toggle shader if not active
                if (value != shader)
                {
                    shader = value;
                    shader.Enable();
                }

                //Update standard camera matrices
                var mtxMdl = Camera.ModelMatrix;
                var mtxCam = Camera.ViewProjectionMatrix;
                shader.SetMatrix4x4("mtxMdl", ref mtxMdl);
                shader.SetMatrix4x4("mtxCam", ref mtxCam);
            }
        }
    }
}
