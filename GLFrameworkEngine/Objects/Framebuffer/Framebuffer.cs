using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public sealed partial class Framebuffer : GLObject
    {
        public FramebufferTarget Target { get; }

        public PixelInternalFormat PixelInternalFormat { get; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public List<IFramebufferAttachment> Attachments { get; }

        public Framebuffer(FramebufferTarget framebufferTarget) : base(GL.GenFramebuffer()) {
            Target = framebufferTarget;
            Attachments = new List<IFramebufferAttachment>();
        }

        public Framebuffer(FramebufferTarget target, int width, int height,
          PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba, int colorAttachmentsCount = 1, bool useDepth = true)
          : this(target)
        {
            if (colorAttachmentsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(colorAttachmentsCount), "Color attachment count must be non negative.");

            Bind();
            PixelInternalFormat = pixelInternalFormat;
            Width = width;
            Height = height;

            Attachments = CreateColorAttachments(width, height, colorAttachmentsCount);

            if (useDepth)
                SetUpRboDepth(width, height);
        }

        public Framebuffer(FramebufferTarget target, int width, int height, int numSamples,
     PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba, int colorAttachmentsCount = 1)
     : this(target)
        {
            if (colorAttachmentsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(colorAttachmentsCount), "Color attachment count must be non negative.");

            Bind();
            PixelInternalFormat = pixelInternalFormat;
            Width = width;
            Height = height;

            Attachments = CreateColorAttachments(width, height, colorAttachmentsCount, numSamples);

            SetUpRboDepth(width, height, numSamples);
        }

        public void Bind() {
            GL.BindFramebuffer(Target, ID);
        }

        public void Unbind() {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Dispoe() {
            GL.DeleteFramebuffer(ID);
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            foreach (var attatchment in Attachments)
            {
                if (attatchment is GLTexture2D)
                {
                    var tex = (GLTexture2D)attatchment;
                    tex.Bind();
                    GL.TexImage2D(tex.Target, 0, tex.PixelInternalFormat,
                        Width, Height, 0, tex.PixelFormat, tex.PixelType, IntPtr.Zero);
                    tex.Unbind();
                }
                else if (attatchment is DepthTexture)
                {
                    var tex = (DepthTexture)attatchment;
                    tex.Bind();
                    GL.TexImage2D(tex.Target, 0, tex.PixelInternalFormat,
                        Width, Height, 0, tex.PixelFormat, tex.PixelType, IntPtr.Zero);
                    tex.Unbind();
                }
                else if(attatchment is Renderbuffer)
                {
                    var buffer = (Renderbuffer)attatchment;
                    buffer.Bind();
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, buffer.InternalFormat, Width, Height);
                    buffer.Unbind();
                }
            }
        }

        public void DisposeRenderBuffer()
        {
            foreach (var attatchment in Attachments)
                if (attatchment is Renderbuffer)
                    attatchment.Dispose();
        }

        public void SetDrawBuffers(params DrawBuffersEnum[] drawBuffers)
        {
            Bind();
            GL.DrawBuffers(drawBuffers.Length, drawBuffers);
        }

        public void SetReadBuffer(ReadBufferMode readBufferMode)
        {
            Bind();
            GL.ReadBuffer(readBufferMode);
        }

        public FramebufferErrorCode GetStatus()
        {
            Bind();
            return GL.CheckFramebufferStatus(Target);
        }

        public void AddAttachment(FramebufferAttachment attachmentPoint, IFramebufferAttachment attachment)
        {
            // Check if the dimensions are uninitialized.
            if (Attachments.Count == 0 && Width == 0 && Height == 0)
            {
                Width = attachment.Width;
                Height = attachment.Height;
            }

            if (attachment.Width != Width || attachment.Height != Height)
                throw new ArgumentOutOfRangeException(nameof(attachment), "The attachment dimensions do not match the framebuffer's dimensions.");

            attachment.Attach(attachmentPoint, this);
            Attachments.Add(attachment);
        }

        private GLTexture2D CreateColorAttachment(int width, int height)
        {
            GLTexture2D texture = GLTexture2D.CreateUncompressedTexture(width, height,
                                        PixelInternalFormat, PixelFormat.Rgba, PixelType.Float);
            // Don't use mipmaps for color attachments.
            texture.MinFilter = TextureMinFilter.Linear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.UpdateParameters();
            return texture;
        }

        private GLTexture2D CreateColorAttachment(int width, int height, int numSamples)
        {
            GLTexture2D texture = GLTexture2DMultiSampled.CreateUncompressedTexture(width, height,
                                        PixelInternalFormat, PixelFormat.Rgba, PixelType.Float);
            // Don't use mipmaps for color attachments.
            texture.MinFilter = TextureMinFilter.Linear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.UpdateParameters();
            return texture;
        }

        private void SetUpRboDepth(int width, int height)
        {
            // Render buffer for the depth attachment, which is necessary for depth testing.
            Renderbuffer rboDepth = new Renderbuffer(width, height, RenderbufferStorage.Depth24Stencil8);
            AddAttachment(FramebufferAttachment.DepthStencilAttachment, rboDepth);
        }

        private void SetUpRboDepth(int width, int height, int numSamples)
        {
            // Render buffer for the depth attachment, which is necessary for depth testing.
            Renderbuffer rboDepth = new Renderbuffer(width, height, numSamples, RenderbufferStorage.Depth24Stencil8);
            AddAttachment(FramebufferAttachment.DepthStencilAttachment, rboDepth);
        }

        private List<IFramebufferAttachment> CreateColorAttachments(int width, int height, int colorAttachmentsCount)
        {
            var colorAttachments = new List<IFramebufferAttachment>();

            List<DrawBuffersEnum> attachmentEnums = new List<DrawBuffersEnum>();
            for (int i = 0; i < colorAttachmentsCount; i++)
            {
                DrawBuffersEnum attachmentPoint = DrawBuffersEnum.ColorAttachment0 + i;
                attachmentEnums.Add(attachmentPoint);

                GLTexture2D texture = CreateColorAttachment(width, height);
                colorAttachments.Add(texture);
                AddAttachment((FramebufferAttachment)attachmentPoint, texture);
            }

            SetDrawBuffers(attachmentEnums.ToArray());

            return colorAttachments;
        }

        private List<IFramebufferAttachment> CreateColorAttachments(int width, int height, int colorAttachmentsCount, int numSamples)
        {
            var colorAttachments = new List<IFramebufferAttachment>();

            List<DrawBuffersEnum> attachmentEnums = new List<DrawBuffersEnum>();
            for (int i = 0; i < colorAttachmentsCount; i++)
            {
                DrawBuffersEnum attachmentPoint = DrawBuffersEnum.ColorAttachment0 + i;
                attachmentEnums.Add(attachmentPoint);

                GLTexture2D texture = CreateColorAttachment(width, height, numSamples);
                colorAttachments.Add(texture);
                AddAttachment((FramebufferAttachment)attachmentPoint, texture);
            }

            SetDrawBuffers(attachmentEnums.ToArray());

            return colorAttachments;
        }
    }
}
