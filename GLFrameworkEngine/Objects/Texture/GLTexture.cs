using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTexture : GLObject, IFramebufferAttachment, IRenderableTexture
    {
        public string Name { get; set; }

        public TextureTarget Target { get; set; }

        public TextureMagFilter MagFilter { get; set; }
        public TextureMinFilter MinFilter { get; set; }

        public TextureWrapMode WrapS { get; set; }
        public TextureWrapMode WrapT { get; set; }
        public TextureWrapMode WrapR { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int MipCount
        {
            get { return _mipCount; }
            set { _mipCount = Math.Max(value, 1); }
        }
        private int _mipCount = 1;

        public int ArrayCount
        {
            get { return _arrayCount; }
            set { _arrayCount = Math.Max(value, 1); }
        }
        private int _arrayCount = 1;

        public PixelInternalFormat PixelInternalFormat { get; internal set; }
        public PixelFormat PixelFormat { get; internal set; }
        public PixelType PixelType { get; internal set; }

        public GLTexture() : base(GL.GenTexture())
        {
            Target = TextureTarget.Texture2D;
            WrapS = TextureWrapMode.ClampToEdge;
            WrapT = TextureWrapMode.ClampToEdge;
            WrapR = TextureWrapMode.ClampToEdge;
            MinFilter = TextureMinFilter.Linear;
            MagFilter = TextureMagFilter.Linear;
            PixelInternalFormat = PixelInternalFormat.Rgba;

            GL.TexParameter(Target, TextureParameterName.TextureLodBias, 0);
            GL.TexParameter(Target, TextureParameterName.TextureMinLod, 0);
            GL.TexParameter(Target, TextureParameterName.TextureMaxLod, 14);

            UpdateParameters();
        }

        public void GenerateMipmaps() {
            Bind();
            GL.GenerateMipmap((GenerateMipmapTarget)Target);
        }

        public void Bind() {
            GL.BindTexture(Target, ID);
        }

        public void Unbind() {
            GL.BindTexture(Target, 0);
        }

        public void Dispose() {
            GL.DeleteTexture(ID);
        }

        public virtual void Attach(FramebufferAttachment attachment, Framebuffer target) {
            target.Bind();
            GL.FramebufferTexture(target.Target, attachment, ID, 0);
        }

        public void UpdateParameters()
        {
            GL.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
            GL.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
            GL.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
            GL.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
            GL.TexParameter(Target, TextureParameterName.TextureWrapR, (int)WrapR);
        }

        public virtual void SaveDDS(string fileName)
        {

        }

        public static GLTexture FromGenericTexture(STGenericTexture texture, ImageParameters parameters = null)
        {
            if (parameters == null) parameters = new ImageParameters();

            switch (texture.SurfaceType)
            {
                case STSurfaceType.Texture2D_Array:
                 return GLTexture2DArray.FromGeneric(texture, parameters);
                case STSurfaceType.Texture3D:
                    return GLTexture3D.FromGeneric(texture, parameters);
                case STSurfaceType.TextureCube:
                    return GLTextureCube.FromGeneric(texture, parameters);
                default:
                    return GLTexture2D.FromGeneric(texture, parameters);
            }
        }

        public void LoadImage(STGenericTexture texture, ImageParameters parameters)
        {
            if (parameters == null) parameters = new ImageParameters();

            Bind();

            var format = texture.Platform.OutputFormat;
            var width = CalculateMipDimension((int)texture.Width, 0);
            var height = CalculateMipDimension((int)texture.Height, 0);

            int numSurfaces = 1;
            if (Target == TextureTarget.Texture3D || Target == TextureTarget.Texture2DArray) {
                numSurfaces = (int)Math.Max(1, texture.Depth);
            }
            if (Target == TextureTarget.TextureCubeMap || Target == TextureTarget.TextureCubeMapArray) {
                numSurfaces = (int)Math.Max(1, texture.ArrayCount);
            }

            for (int i = 0; i < numSurfaces; i++)
            {
                var surface = texture.GetDeswizzledSurface(i, 0);
                int depth = 1;

                bool loadAsBitmap = !IsPower2(width, height) && texture.IsBCNCompressed() && false;
                if (texture.IsASTC() || parameters.FlipY)
                    loadAsBitmap = true;

                int numMips = 1;

                for (int mipLevel = 0; mipLevel < numMips; mipLevel++)
                {
                    if (loadAsBitmap || parameters.UseSoftwareDecoder)
                    {
                        var rgbaData = texture.GetDecodedSurface(i, mipLevel);
                        if (parameters.FlipY)
                            rgbaData = FlipVertical(width, height, rgbaData);

                        var formatInfo = GLFormatHelper.ConvertPixelFormat(TexFormat.RGBA8_UNORM);
                        if (texture.IsSRGB) formatInfo.InternalFormat = PixelInternalFormat.Srgb8Alpha8;

                        GLTextureDataLoader.LoadImage(Target, width, height, depth, formatInfo, rgbaData, mipLevel);
                    }
                    else if (texture.IsBCNCompressed())
                    {
                        var internalFormat = GLFormatHelper.ConvertCompressedFormat(format, true);
                        GLTextureDataLoader.LoadCompressedImage(Target, width, height, depth, internalFormat, surface, mipLevel);
                    }
                    else
                    {
                        var formatInfo = GLFormatHelper.ConvertPixelFormat(format);
                        GLTextureDataLoader.LoadImage(Target, width, height, depth, formatInfo, surface, mipLevel);
                    }
                }

                if (texture.MipCount > 1 && texture.Platform.OutputFormat != TexFormat.BC5_SNORM)
                    GL.GenerateMipmap((GenerateMipmapTarget)Target);
                else
                {
                    //Set level to base only
                    GL.TexParameter(Target, TextureParameterName.TextureBaseLevel, 0);
                    GL.TexParameter(Target, TextureParameterName.TextureMaxLevel, 0);
                }
            }

            Unbind();
        }

        public static bool IsPower2(int width, int height) {
            return IsPow2(width) && IsPow2(height);
        }

        public virtual System.Drawing.Bitmap ToBitmap(bool saveAlpha = false)
        {
            return null;
        }

        internal static int CalculateMipDimension(int baseLevelDimension, int mipLevel) {
            return baseLevelDimension / (int)Math.Pow(2, mipLevel);
        }

        internal static bool IsPow2(int Value){
            return Value != 0 && (Value & (Value - 1)) == 0;
        }

        private static byte[] FlipVertical(int Width, int Height, byte[] Input)
        {
            byte[] FlippedOutput = new byte[Width * Height * 4];

            int Stride = Width * 4;
            for (int Y = 0; Y < Height; Y++)
            {
                int IOffs = Stride * Y;
                int OOffs = Stride * (Height - 1 - Y);

                for (int X = 0; X < Width; X++)
                {
                    FlippedOutput[OOffs + 0] = Input[IOffs + 0];
                    FlippedOutput[OOffs + 1] = Input[IOffs + 1];
                    FlippedOutput[OOffs + 2] = Input[IOffs + 2];
                    FlippedOutput[OOffs + 3] = Input[IOffs + 3];

                    IOffs += 4;
                    OOffs += 4;
                }
            }
            return FlippedOutput;
        }
    }
}
