using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core; 

namespace GLFrameworkEngine
{
    public class OpenGLHelper
    {
        public static Vector2 NormMouseCoords(int x, int y, int width, int height)
        {
            return new Vector2(x - width / 2, y - height / 2);
        }

        public static int GetSwizzle(STChannelType channel)
        {
            switch (channel)
            {
                case STChannelType.Red: return (int)All.Red;
                case STChannelType.Green: return (int)All.Green;
                case STChannelType.Blue: return (int)All.Blue;
                case STChannelType.Alpha: return (int)All.Alpha;
                case STChannelType.One: return (int)All.One;
                case STChannelType.Zero: return (int)All.Zero;
                default: return 0;
            }
        }
        public static readonly Dictionary<STTextureMinFilter, TextureMinFilter> MinFilter = new Dictionary<STTextureMinFilter, TextureMinFilter>()
        {
            {  STTextureMinFilter.Nearest, TextureMinFilter.Nearest},
            {  STTextureMinFilter.Linear, TextureMinFilter.Linear},
            {  STTextureMinFilter.NearestMipmapLinear, TextureMinFilter.NearestMipmapLinear},
            {  STTextureMinFilter.NearestMipmapNearest, TextureMinFilter.NearestMipmapNearest},
            {  STTextureMinFilter.LinearMipmapLinear, TextureMinFilter.LinearMipmapLinear},
            {  STTextureMinFilter.LinearMipmapNearest, TextureMinFilter.LinearMipmapNearest},
        };
        public static readonly Dictionary<STTextureMagFilter, TextureMagFilter> MagFilter = new Dictionary<STTextureMagFilter, TextureMagFilter>()
        {
            { STTextureMagFilter.Linear, TextureMagFilter.Linear},
            { STTextureMagFilter.Nearest, TextureMagFilter.Nearest},
            { (STTextureMagFilter)3, TextureMagFilter.Linear},
        };

        public static Dictionary<STTextureWrapMode, TextureWrapMode> WrapMode = new Dictionary<STTextureWrapMode, TextureWrapMode>(){
            { STTextureWrapMode.Repeat, TextureWrapMode.Repeat},
            { STTextureWrapMode.Mirror, TextureWrapMode.MirroredRepeat},
            { STTextureWrapMode.Clamp, TextureWrapMode.ClampToEdge},
            { (STTextureWrapMode)3, TextureWrapMode.ClampToEdge},
            { (STTextureWrapMode)4, TextureWrapMode.ClampToEdge},
            { (STTextureWrapMode)5, TextureWrapMode.ClampToEdge},
        };

        public static Dictionary<STPrimitiveType, PrimitiveType> PrimitiveTypes = new Dictionary<STPrimitiveType, PrimitiveType>(){
            { STPrimitiveType.Triangles, PrimitiveType.Triangles},
            { STPrimitiveType.LineLoop, PrimitiveType.LineLoop},
            { STPrimitiveType.Lines, PrimitiveType.Lines},
            { STPrimitiveType.Points, PrimitiveType.Points},
            { STPrimitiveType.Quad, PrimitiveType.Quads},
            { STPrimitiveType.QuadStrips, PrimitiveType.QuadStrip},
            { STPrimitiveType.TriangleStrips, PrimitiveType.TriangleStrip},
            { STPrimitiveType.TriangleFans, PrimitiveType.TriangleFan},
        };
    }
}
