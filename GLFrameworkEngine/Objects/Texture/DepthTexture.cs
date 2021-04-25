using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class DepthTexture : GLTexture, IFramebufferAttachment
    {
        public DepthTexture(int width, int height, PixelInternalFormat pixelInternalFormat) 
            : base()
        {
            Target = TextureTarget.Texture2D;
            PixelInternalFormat = pixelInternalFormat;
            PixelFormat = PixelFormat.DepthComponent;
            PixelType = PixelType.Float;
            Width = width;
            Height = height;

            // Set texture settings.
            Bind();

            GL.TexImage2D(Target, 0, PixelInternalFormat, Width, Height, 0, PixelFormat, PixelType, IntPtr.Zero);
            MagFilter = TextureMagFilter.Nearest;
            MinFilter = TextureMinFilter.Nearest;

            // Use white for values outside the depth map's border.
            WrapS = TextureWrapMode.ClampToBorder;
            WrapT = TextureWrapMode.ClampToBorder;
            UpdateParameters();

            GL.TexParameter(Target, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
            GL.TexParameter(Target, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });

            Unbind();
        }

        public override void Attach(FramebufferAttachment attachment, Framebuffer target)
        {
            target.Bind();
            GL.FramebufferTexture2D(target.Target, attachment, TextureTarget.Texture2D, ID, 0);
        }
    }
}
