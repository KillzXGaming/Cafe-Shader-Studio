using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public interface IFramebufferAttachment
    {
        int Width { get; }

        int Height { get; }

        void Attach(FramebufferAttachment attachment, Framebuffer target);

        void Dispose();
    }
}
