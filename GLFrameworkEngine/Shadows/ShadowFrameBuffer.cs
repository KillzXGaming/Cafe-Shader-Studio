using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class ShadowFrameBuffer : Framebuffer
    {
        public DepthTexture GetShadowTexture() => (DepthTexture)Attachments[0];

        public ShadowFrameBuffer(int width, int height) : base(FramebufferTarget.Framebuffer)
        {
            Bind();
            this.PixelInternalFormat = PixelInternalFormat.DepthComponent24;
            this.Width = width;
            this.Height = height;

            this.SetDrawBuffers(DrawBuffersEnum.None);
            this.SetReadBuffer(ReadBufferMode.None);

            GLTexture shadowTexture = new DepthTexture(width, height, PixelInternalFormat.DepthComponent24);
            AddAttachment(FramebufferAttachment.DepthAttachment, shadowTexture);
        }
    }
}
