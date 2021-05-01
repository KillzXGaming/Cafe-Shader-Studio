using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace GLFrameworkEngine
{
    public class ColorPicker
    {
        private Framebuffer pickingBuffer;
        private uint pickingColor;

        public Dictionary<uint, IPickable> ColorPassIDs = new Dictionary<uint, IPickable>();

        public bool EnablePicking = false;

        public float NormalizedPickingDepth;

        private float Depth;

        private int pickingIndex = 1;

        public GLTexture2D GetDebugPickingDisplay() {
            if (pickingBuffer == null) return null;

           return (GLTexture2D)pickingBuffer.Attachments[0];
        }

        public SelectionMode PickingMode = SelectionMode.Mesh;

        public enum SelectionMode
        {
            Object,
            Model,
            Mesh,
            Material,
            Face,
        }

        public void SetPickingColorFaces(List<IPickable> pickables, ShaderProgram shader)
        {
            shader.SetInt("pickFace", 1);
            shader.SetInt("pickedIndex", pickingIndex);

            for (int i = 0; i < pickables.Count; i++)
            {
                var color = new Vector4(
                 ((pickingIndex >> 16) & 0xFF),
                 ((pickingIndex >> 8) & 0xFF),
                 (pickingIndex & 0xFF),
                 ((pickingIndex++ >> 24) & 0xFF)
                 );

                color = new Vector4(color.X, color.Y, color.Z, color.W);

                var key = BitConverter.ToUInt32(new byte[]{
                    (byte)color.X, (byte)color.Y,
                    (byte)color.Z, (byte)color.W
                }, 0);
                ColorPassIDs.Add(key, pickables[i]);
            }
        }

        public void SetPickingColor(IPickable pickable, ShaderProgram shader)
        {
            var color = new Vector4(
               ((pickingIndex >> 16) & 0xFF),
               ((pickingIndex >> 8) & 0xFF),
               (pickingIndex & 0xFF),
               ((pickingIndex++ >> 24) & 0xFF)
               );

            color = new Vector4(color.X, color.Y, color.Z, color.W);

            var key = BitConverter.ToUInt32(new byte[]{
                (byte)color.X, (byte)color.Y,
                (byte)color.Z, (byte)color.W
            }, 0);

            ColorPassIDs.Add(key, pickable);

            var input = color / 255.0f;
            shader.SetVector4("color", input);
        }

        private void Init(GLContext context)
        {
            if (pickingBuffer == null)
                pickingBuffer = new Framebuffer(FramebufferTarget.Framebuffer, context.Width, context.Height, PixelInternalFormat.Rgba, 1);

            if (pickingBuffer.Width != context.Width || pickingBuffer.Height != context.Height)
                pickingBuffer.Resize(context.Width, context.Height);
        }

        public IPickable FindPickableAtPosition(GLContext context, List<IPickable> drawables, Vector2 position)
        {
            Init(context);

            var camera = context.Camera;

            pickingBuffer.Bind();
            GL.Viewport(0, 0, context.Width, context.Height);
            GL.ClearColor(1, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ColorPassIDs.Clear();
            pickingIndex = 1;

            //Draw the pickable objects. Drawn IDs will be passed into ColorPassIDs
            foreach (var drawable in drawables)
                drawable.DrawColorPicking(context);

            GL.UseProgram(0);

            GL.ReadPixels((int)position.X, (int)position.Y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, ref pickingColor);
            GL.ReadPixels((int)position.X, (int)position.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref Depth);
            //Get normalized depth for z depth
            NormalizedPickingDepth = -(camera.ZFar * camera.ZNear / (Depth * (camera.ZFar - camera.ZNear) - camera.ZFar));

            pickingBuffer.Unbind();

            foreach (var drawable in drawables)
                drawable.IsHovered = false;

            return SearchPickedColor(pickingColor);
        }

        public void UpdatePickingDepth(GLContext context, Vector2 position)
        {
            var camera = context.Camera;

            GL.ReadPixels((int)position.X, (int)position.Y, 1, 1, PixelFormat.DepthComponent, PixelType.Float, ref Depth);

            //Get normalized depth for z depth
            if (Depth == 1.0f)
                NormalizedPickingDepth = camera.ZFar;
            else
                NormalizedPickingDepth = -(camera.ZFar * camera.ZNear / (Depth * (camera.ZFar - camera.ZNear) - camera.ZFar));

            camera.Depth = NormalizedPickingDepth;
        }

        /// <summary>
        /// Searches and returns the object that has a color id match from the picking color buffer.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public IPickable SearchPickedColor(uint color)
        {
            if (ColorPassIDs.ContainsKey(color))
                return ColorPassIDs[color];
            return null;
        }
    }
}
