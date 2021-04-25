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
    public class UVViewport : Viewport2D
    {
        public STGenericTextureMap ActiveTextureMap { get; set; }
        public List<STGenericMesh> ActiveObjects = new List<STGenericMesh>();

        public UVMap DrawableUVMap = new UVMap();

        public int PolygonGroupIndex = -1;
        public int UvChannelIndex = 0;
        public bool Repeat = true;
        public float Brightness = 1.0f;

        public bool DisplayUVs = true;

        public bool UpdateVertexBuffer { get; set; } 

        public override void RenderScene()
        {
            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            STGenericTexture tex = null;

            foreach (var render in GLFrameworkEngine.DataCache.ModelCache.Values) {
                if (ActiveTextureMap != null) {
                    var brender = render;

                    if (brender.Textures.ContainsKey(ActiveTextureMap.Name))
                        tex = brender.Textures[ActiveTextureMap.Name];
                }
            }

            Vector2 aspectScale = UpdateAspectScale(Width, Height, tex);

            UVBackground.Draw(tex, ActiveTextureMap, Width, Height, aspectScale, Camera);
            DrawableUVMap.UpdateVertexBuffer(PolygonGroupIndex, UvChannelIndex, ActiveObjects, ActiveTextureMap);
            DrawableUVMap.Draw(Camera, aspectScale);
        }

        public void Reset() {
            DrawableUVMap.Reset();
        }

        static Vector2 UpdateAspectScale(int width, int height, STGenericTexture tex)
        {
            Vector2 scale = new Vector2(1);

            if (tex == null) return scale;

            //Adjust scale via aspect ratio
            if (width > height)
            {
                float aspect = (float)tex.Width / (float)tex.Height;
                scale.X *= aspect;
            }
            else
            {
                float aspect = (float)tex.Height / (float)tex.Width;
                scale.Y *= aspect;
            }
            return scale;
        }
    }
}
