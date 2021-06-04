using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class Pipeline
    {
        DrawableFloor _floor;
        DrawableBackground _background;

        public List<IRenderableFile> Files = new List<IRenderableFile>();
        public List<GenericRenderer> SceneObjects = new List<GenericRenderer>();
        public List<CameraAnimation> CameraAnimations = new List<CameraAnimation>();

        public int Width { get; set; }

        public int Height { get; set; }

        public GLContext _context;
        public Camera _camera;

        private OpenTK.Vector2 _previousPosition = OpenTK.Vector2.Zero;

        private DepthTexture DepthTexture;

        private Framebuffer PostEffects;
        private Framebuffer BloomEffects;
        private Framebuffer ScreenBuffer;
        private Framebuffer GBuffer;
        private Framebuffer FinalBuffer;

        public int GetViewportTexture() => ((GLTexture)FinalBuffer.Attachments[0]).ID;

        public void InitScene()
        {
            _floor = new DrawableFloor();
            _background = new DrawableBackground();

            _context = new GLContext();
            _camera = new Camera();
            _context.Camera = _camera;
            _context.ScreenBuffer = ScreenBuffer;
            _context.Camera.ResetViewportTransform();
        }

        public void InitBuffers()
        {
            InitScene();

            ScreenBuffer = new Framebuffer(FramebufferTarget.Framebuffer,
                this.Width, this.Height, 16, PixelInternalFormat.Rgba16f, 1);
            ScreenBuffer.Resize(Width, Height);

            PostEffects = new Framebuffer(FramebufferTarget.Framebuffer,
                 Width, Height, PixelInternalFormat.Rgba16f, 1);
            PostEffects.Resize(Width, Height);

            BloomEffects = new Framebuffer(FramebufferTarget.Framebuffer,
                 Width, Height, PixelInternalFormat.Rgba16f, 1);
            BloomEffects.Resize(Width, Height);

            /*
                     DepthTexture = new DepthTexture(Width, Height, PixelInternalFormat.DepthComponent24);

                     //Set the GBuffer (Depth, Normals and another output)
                  GBuffer = new Framebuffer(FramebufferTarget.Framebuffer);
                     GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment0,
                         GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgba, PixelType.Float));
                     GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment3,
                         GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.Rgb10A2, PixelFormat.Rgba, PixelType.Float));
                     GBuffer.AddAttachment(FramebufferAttachment.ColorAttachment4,
                         GLTexture2D.CreateUncompressedTexture(Width, Height, PixelInternalFormat.Rgb10A2, PixelFormat.Rgba, PixelType.Float));
                     GBuffer.AddAttachment(FramebufferAttachment.DepthAttachment, DepthTexture);

                     GBuffer.SetReadBuffer(ReadBufferMode.None);
                     GBuffer.SetDrawBuffers(
                         DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.None, DrawBuffersEnum.None,
                         DrawBuffersEnum.ColorAttachment3, DrawBuffersEnum.ColorAttachment4);
                     GBuffer.Unbind();
                     */

            FinalBuffer = new Framebuffer(FramebufferTarget.Framebuffer,
                this.Width, this.Height, PixelInternalFormat.Rgba16f, 1);
        }

        //Adds a camera to the scene for path viewing
        public void AddCameraAnimation(CameraAnimation animation)
        {
            CameraAnimations.Clear();
            CameraAnimations.Add(animation);
        }

        public void AddFile(IRenderableFile renderFile) {
            Files.Add(renderFile);
            if (renderFile.Renderer is IPickable)
                _context.Scene.PickableObjects.Add((IPickable)renderFile.Renderer);
        }

        public void AddFile(GenericRenderer renderFile) {
            SceneObjects.Add(renderFile);
            if (renderFile is IPickable)
                _context.Scene.PickableObjects.Add((IPickable)renderFile);
        }

        public void RenderScene()
        {
            _context.Width = this.Width;
            _context.Height = this.Height;

            GL.Enable(EnableCap.DepthTest);

            _context.Camera.UpdateMatrices();

            DrawModels();
            GL.UseProgram(0);

            //Transfer the screen buffer to the post effects buffer (screen buffer is multi sampled)
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ScreenBuffer.ID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, PostEffects.ID);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            FinalBuffer.Bind();
            GL.Viewport(0, 0, Width, Height);

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            
            //Draw post effects onto the final buffer
            DrawPostScreenBuffer(PostEffects);

            //Finally transfer the screen buffer depth onto the final buffer for non post processed objects
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, ScreenBuffer.ID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FinalBuffer.ID);
            GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);

            _background.Draw(_context, Pass.OPAQUE);
            _floor.Draw(_context, Pass.OPAQUE);
            _context.Scene.DrawSelection(_context);

            foreach (var anim in CameraAnimations)
                anim.DrawPath(_context);

            FinalBuffer.Unbind();
        }

        public void OnResize()
        {
            // Update the opengl viewport
            GL.Viewport(0, 0, Width, Height);

            //Resize all the screen buffers
            ScreenBuffer?.Resize(Width, Height);
            PostEffects?.Resize(Width, Height);
            GBuffer?.Resize(Width, Height);
            FinalBuffer?.Resize(Width, Height);
            BloomEffects?.Resize(Width, Height);

            //Store the screen buffer instance for color buffer effects
            _context.ScreenBuffer = ScreenBuffer;
            _context.Width = this.Width;
            _context.Height = this.Height;
            _context.Camera.Width = this.Width;
            _context.Camera.Height = this.Height;
            _context.Camera.UpdateMatrices();
        }

        public void PickScene(MouseEventInfo e, bool selectAction)
        {
            if (!_context.ColorPicker.EnablePicking)
                return;

            if (selectAction && !Keyboard.GetState().IsKeyDown(Key.ControlLeft))
                _context.Scene.ResetSelected();

            OpenTK.Vector2 position = new OpenTK.Vector2(e.Position.X, _context.Height - e.Position.Y);
            var pickable = _context.Scene.FindPickableAtPosition(_context, position);
            if (pickable != null)
            {
                pickable.IsHovered = true;
                if (selectAction)
                {
                    pickable.IsSelected = true;
                    _context.Scene.SetTransformAction(pickable.Transform, GLScene.TransformActions.Translate);
                }
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Width, Height);

            if (_context.Scene.GetSelected().Count == 0)
                _context.Scene.ActiveAction = null;
        }


        public IPickable GetPickedObject(MouseEventInfo e)
        {
            OpenTK.Vector2 position = new OpenTK.Vector2(e.Position.X, _context.Height - e.Position.Y);

            return _context.Scene.FindPickableAtPosition(_context, position);
        }


        private void DrawModels()
        {
            /*  if (AGraphicsLibrary.LightingEngine.LightSettings.ColorCorrectionTable == null ||
                  AGraphicsLibrary.LightingEngine.LightSettings.UpdateColorCorrection)
                  AGraphicsLibrary.LightingEngine.LightSettings.UpdateColorCorrectionTable();*/

            //  DrawGBuffer();

            GL.Viewport(0, 0, Width, Height);
            ScreenBuffer.Bind();

            GL.ClearColor(0.01f, 0.01f, 0.01f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            DrawBfresModels();

            //Update depth information of the current mouse.
            _context.ColorPicker.UpdatePickingDepth(_context, _context.CurrentMousePoint);

            ScreenBuffer.Unbind();

            _context.CurrentShader = null;
        }

        private void DrawBfresModels()
        {
            foreach (var file in SceneObjects)
                file.DrawModel(_context, Pass.OPAQUE, OpenTK.Vector4.Zero);

            foreach (var file in Files)
                file.Renderer.DrawModel(_context, Pass.OPAQUE, OpenTK.Vector4.Zero);

            foreach (var file in SceneObjects)
                file.DrawModel(_context, Pass.TRANSPARENT, OpenTK.Vector4.Zero);

            foreach (var file in Files)
                file.Renderer.DrawModel(_context, Pass.TRANSPARENT, OpenTK.Vector4.Zero);
        }

        private void DrawGBuffer()
        {
            GBuffer.Bind();
            GL.Viewport(0, 0, Width, Height);

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var file in Files)
                file.Renderer.DrawGBuffer(_context);

            GBuffer.Unbind();

            AGraphicsLibrary.LightingEngine.LightSettings.UpdateLightPrepass(_context,
                 ((GLTexture2D)GBuffer.Attachments[1]).ID, DepthTexture.ID);

            /*    AGraphicsLibrary.LightingEngine.LightSettings.UpdateShadowPrepass(this,
                     ShadowRenderer.ShadowMapID,
                     DepthTexture.ID);*/
        }

        private GLTexture2D bloomPass;

        private void DrawPostScreenBuffer(Framebuffer screen)
        {
            if (bloomPass == null) {
                bloomPass = GLTexture2D.CreateUncompressedTexture(1, 1);
            }

            var colorPass = (GLTexture2D)screen.Attachments[0];

            if (_context.EnableBloom)
            {
                var brightnessTex = BloomExtractionTexture.FilterScreen(_context, colorPass);
                BloomProcess.Draw(brightnessTex, BloomEffects, _context, Width, Height);
                bloomPass = (GLTexture2D)BloomEffects.Attachments[0];
            }

            FinalBuffer.Bind();
            DeferredRenderQuad.Draw(_context, colorPass, bloomPass);
        }

        public void OnMouseMove(MouseEventInfo e, KeyEventInfo k)
        {
            _context.OnMouseMove(e);

            if (!e.HasValue)
                e.Position = new Point((int)_previousPosition.X, (int)_previousPosition.Y);

            int transformState = 0;
            if (_context.Scene.ActiveAction != null)
                transformState = _context.Scene.ActiveAction.OnMouseMove(_context, e);

            if (transformState != 0)
                return;

            _context.Camera.Controller.MouseMove(e, k, _previousPosition);
            _previousPosition = new OpenTK.Vector2(e.X, e.Y);
        }

        private float previousMouseWheel;

        public void ResetPrevious() {
            previousMouseWheel = 0;
        }

        public void OnMouseWheel(MouseEventInfo e, KeyEventInfo k)
        {
            if (previousMouseWheel == 0)
                previousMouseWheel = e.WheelPrecise;

            e.Delta = e.WheelPrecise - previousMouseWheel;
            _context.Camera.Controller.MouseWheel(e, k);
            previousMouseWheel = e.WheelPrecise;
        }

        public void OnMouseUp(MouseEventInfo e)
        {
            if (_context.Scene.ActiveAction != null)
                _context.Scene.ActiveAction.OnMouseUp(_context, e);

            _previousPosition = new OpenTK.Vector2(e.X, e.Y);
        }

        public void OnMouseDown(MouseEventInfo e, KeyEventInfo k)
        {
            _context.OnMouseDown(e);

            _previousPosition = new OpenTK.Vector2(e.X, e.Y);

            if (_context.Scene.ActiveAction != null)
            {
                var state = _context.Scene.ActiveAction.OnMouseDown(_context, e);
                if (state != 0)
                    return;
            }

            if (e.LeftButton == ButtonState.Pressed)
                PickScene(e, true);

            _context.Camera.Controller.MouseClick(e, k);
        }
    }
}
