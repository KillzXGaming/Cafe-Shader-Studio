using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLTexture2DMultiSampled : GLTexture2D
    {
        public static GLTexture2DMultiSampled CreateUncompressedTexture(int width, int height, int numSamples,
         PixelInternalFormat format = PixelInternalFormat.Rgba8,
         PixelFormat pixelFormat = PixelFormat.Rgba,
         PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTexture2DMultiSampled texture = new GLTexture2DMultiSampled();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = width; texture.Height = height;
            texture.Target = TextureTarget.Texture2DMultisample;
            texture.Bind();

            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample,
                numSamples, format, texture.Width, texture.Height, true);

            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }

        public override void Attach(FramebufferAttachment attachment, Framebuffer target)
        {
            target.Bind();
            GL.FramebufferTexture2D(target.Target, attachment, TextureTarget.Texture2DMultisample , ID, 0);
        }
    }
}
