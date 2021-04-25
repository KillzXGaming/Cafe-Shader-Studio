using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class Renderbuffer : GLObject, IFramebufferAttachment
    {
        public int Width { get; }

        public int Height { get; }

        public RenderbufferStorage InternalFormat { get; private set; }

        public Renderbuffer(int width, int height, RenderbufferStorage internalFormat)
            : base(GL.GenRenderbuffer())
        {
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, internalFormat, width, height);
        }

        public Renderbuffer(int width, int height, int samples, RenderbufferStorage internalFormat)
          : base(GL.GenRenderbuffer())
        {
            Width = width;
            Height = height;
            InternalFormat = internalFormat;

            // Allocate storage for the renderbuffer.
            Bind();
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4,
                internalFormat, width, height);
        }

        public void Bind() {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, ID);
        }

        public void Unbind() {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }

        public void Attach(FramebufferAttachment attachment, Framebuffer target) {
            target.Bind();
            GL.FramebufferRenderbuffer(target.Target, attachment, RenderbufferTarget.Renderbuffer, ID);
        }

        public void Dispose() {
            GL.DeleteRenderbuffer(ID);
        }
    }
}
