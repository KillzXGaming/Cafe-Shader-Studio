using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using System.Drawing;
using Toolbox.Core.IO;

namespace CafeStudio.UI
{
    public class ImageEditorViewport : Viewport2D
    {
        public bool DisplayAlpha = true;

        public STGenericTexture ActiveTexture;

        public override void RenderScene()
        {
            var shader = GlobalShaders.GetShader("IMAGE_EDITOR");
            shader.Enable();

            ImageEditorBackground.Draw(ActiveTexture, Width, Height, Camera, DisplayAlpha);
        }

        public void Reset() {
        }
    }
}
