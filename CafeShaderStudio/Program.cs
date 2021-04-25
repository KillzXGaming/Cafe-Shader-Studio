using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using Toolbox.Core;
using GLFrameworkEngine;

namespace CafeShaderStudio
{
    class Program
    {
        static void Main(string[] args)
        {
            InitResourceCreation();

            Runtime.DisplayBones = false;

            GraphicsMode mode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, new ColorFormat(32), 2, false);
            MainWindow wnd = new MainWindow(mode);
            wnd.VSync = OpenTK.VSyncMode.On;
            wnd.Run();
        }

        //Render creation for the opengl backend
        //This is to keep the render handling more seperated from the core library
        static void InitResourceCreation()
        {
            //Called during LoadRenderable() in STGenericTexture to set the RenderableTex instance.
            RenderResourceCreator.CreateTextureInstance += TextureCreationOpenGL;
        }

        static IRenderableTexture TextureCreationOpenGL(object sender, EventArgs e)
        {
            var tex = sender as STGenericTexture;
            return GLTexture.FromGenericTexture(tex, tex.Parameters);
        }
    }
}
