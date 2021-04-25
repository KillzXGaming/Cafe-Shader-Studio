using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core; 

namespace GLFrameworkEngine
{
    public class OpenGLHelper
    {
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
    }
}
