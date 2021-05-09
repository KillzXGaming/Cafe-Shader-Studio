using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTexture2DArray : GLTexture
    {
        public GLTexture2DArray() : base()
        {
            Target = TextureTarget.Texture2DArray;
        }

        public static GLTexture2DArray CreateUncompressedTexture(int width, int height, int arrayCount, int mipCount,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.PixelInternalFormat = pixelInternalFormat;
            texture.Width = width; texture.Height = height;
            texture.Target = TextureTarget.Texture2DArray;
            texture.MinFilter = TextureMinFilter.LinearMipmapLinear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.MipCount = mipCount;
            texture.ArrayCount = arrayCount;
            texture.Bind();

            //Allocate mip data
            if (texture.MipCount > 1)
                texture.GenerateMipmaps();

            for (int mip = 0; mip < texture.MipCount; mip++)
            {
                int mipWidth = (int)(texture.Width * Math.Pow(0.5, mip));
                int mipHeight = (int)(texture.Height * Math.Pow(0.5, mip));

                GL.TexImage3D(texture.Target, 0, texture.PixelInternalFormat,
                    mipWidth, mipHeight, texture.ArrayCount, mip,
                      texture.PixelFormat, texture.PixelType, IntPtr.Zero);
            }

            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }

        public static GLTexture2DArray FromGeneric(STGenericTexture texture, ImageParameters parameters)
        {
            GLTexture2DArray glTexture = new GLTexture2DArray();
            glTexture.Width = (int)texture.Width;
            glTexture.Height = (int)texture.Height;
            glTexture.LoadImage(texture, parameters);
            return glTexture;
        }

        public static GLTexture2DArray FromDDS(DDS dds)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.Width = (int)dds.Width; texture.Height = (int)dds.Height;
            texture.Bind();

            GL.TexParameter(texture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(texture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(texture.Target, TextureParameterName.TextureMaxLevel, 13);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            InternalFormat format = InternalFormat.Rgba8;

            for (int j = 0; j < dds.MipCount; j++)
            {
                int mipWidth = CalculateMipDimension(texture.Width, j);
                int mipHeight = CalculateMipDimension(texture.Height, j);
                int imageSize = GLFormatHelper.CalculateImageSize(mipWidth, mipHeight, format) * (int)dds.ArrayCount;

                List<byte[]> levels = new List<byte[]>();
                for (int i = 0; i < dds.ArrayCount; i++)
                {
                    var data = dds.GetDecodedSurface(i, j);
                    if (i == 0 || i == 1)
                        data = FlipHorizontal(mipWidth, mipHeight, data);

                    levels.Add(data);
                }

                var surface = ByteUtils.CombineArray(levels.ToArray());
                if (format == InternalFormat.Rgba8)
                {
                    GL.TexImage3D(TextureTarget.Texture2DArray, j,
                        PixelInternalFormat.Rgba,
                        mipWidth, mipHeight, (int)dds.ArrayCount, 0, PixelFormat.Bgra, PixelType.UnsignedByte,
                        surface);
                }
                else
                {
                    GL.CompressedTexImage3D(TextureTarget.Texture2DArray, j,
                        format,
                        mipWidth, mipHeight, (int)dds.ArrayCount,
                        0, imageSize, surface);
                }
            }

            texture.Unbind();
            return texture;
        }

        private static byte[] FlipHorizontal(int Width, int Height, byte[] Input)
        {
            byte[] FlippedOutput = new byte[Width * Height * 4];

            for (int Y = 0; Y < Height; Y++)
            {
                for (int X = 0; X < Width; X++)
                {
                    int IOffs = (Y * Width + X) * 4;
                    int OOffs = (Y * Width + Width - 1 - X) * 4; 

                    FlippedOutput[OOffs + 0] = Input[IOffs + 0];
                    FlippedOutput[OOffs + 1] = Input[IOffs + 1];
                    FlippedOutput[OOffs + 2] = Input[IOffs + 2];
                    FlippedOutput[OOffs + 3] = Input[IOffs + 3];
                }
            }
            return FlippedOutput;
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

        public static GLTexture2DArray FromDDS(DDS[] dds, bool flipY = false)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.Width = (int)dds[0].Width; texture.Height = (int)dds[0].Height;
            texture.Bind();

            GL.TexParameter(texture.Target, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(texture.Target, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(texture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(texture.Target, TextureParameterName.TextureMaxLevel, 1);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(texture.Target, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            InternalFormat format = InternalFormat.Rgba8;
            for (int j = 0; j < dds.Length; j++)
            {
                int mipWidth = CalculateMipDimension(texture.Width, j);
                int mipHeight = CalculateMipDimension(texture.Height, j);
                int imageSize = GLFormatHelper.CalculateImageSize(mipWidth, mipHeight, format) * (int)dds[0].ArrayCount;

                List<byte[]> levels = new List<byte[]>();
                for (int i = 0; i < dds[0].ArrayCount; i++)
                {
                    var data = dds[j].GetDecodedSurface(i, 0);
                    if (i == 0 || i == 1)
                        data = FlipHorizontal(mipWidth, mipHeight, data);

                    levels.Add(data);
                }

                var surface = ByteUtils.CombineArray(levels.ToArray());
                if (format == InternalFormat.Rgba8)
                {
                    GL.TexImage3D(TextureTarget.Texture2DArray, j,
                        PixelInternalFormat.Rgba,
                        mipWidth, mipHeight, (int)dds[0].ArrayCount, 0, PixelFormat.Bgra, PixelType.UnsignedByte,
                        surface);
                }
                else
                {
                    GL.CompressedTexImage3D(TextureTarget.Texture2DArray, j,
                        format,
                        mipWidth, mipHeight, (int)dds[0].ArrayCount,
                        0, imageSize, surface);
                }
            }

            texture.Unbind();
            return texture;
        }

        public static GLTexture2DArray FromBitmap(Bitmap image)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.Width = image.Width; texture.Height = image.Height;
            texture.LoadImage(image);
            return texture;
        }

        public static GLTexture2DArray FromRawData(int width, int height, TexFormat format, byte[] data)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.Width = width; texture.Height = height;
            texture.LoadImage(width, height, format, data);
            return texture;
        }

        public void LoadImage(int width, int height, TexFormat format, byte[] data)
        {
            if (TextureFormatHelper.IsBCNCompressed(format))
            {
                var internalFormat = GLFormatHelper.ConvertCompressedFormat(format, true);
                int imageSize = GLFormatHelper.CalculateImageSize(width, height, internalFormat);

                GL.CompressedTexImage3D(TextureTarget.Texture2DArray, 0,
                internalFormat, width, height, 1, 0, imageSize, data);
            }
            else
            {
                var formatInfo = GLFormatHelper.ConvertPixelFormat(format);

                GL.TexImage3D(Target, 0, formatInfo.InternalFormat, width, height, 1, 0,
                      formatInfo.Format, formatInfo.Type, data);
            }
        }

        public void LoadImage(Bitmap image)
        {
            Bind();

            System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
              System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage3D(Target, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 1, 0,
                  OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);

            Unbind();
        }

        public void LoadImage(byte[] image)
        {
            Bind();

            GL.TexImage3D(Target, 0, PixelInternalFormat.Rgba, Width, Height, 1, 0,
           OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, image);

            Unbind();
        }

        public System.IO.Stream ToStream(bool saveAlpha = false)
        {
            var stream = new System.IO.MemoryStream();
            var bmp = ToBitmap(saveAlpha);
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream;
        }

        public void Save(string fileName, bool saveAlpha = false)
        {
            Bind();

            var bmp = ToBitmap(saveAlpha);
            bmp.Save(fileName + ".png");

            Unbind();
        }

        public System.Drawing.Bitmap ToBitmap(bool saveAlpha = false)
        {
            Bind();

            var bmp = new System.Drawing.Bitmap(Width, Height);

            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                saveAlpha ?
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
                :
                 System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.ReadPixels(0, 0, Width, Height, saveAlpha ? PixelFormat.Bgra : PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            bmp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);

            Unbind();

            return bmp;
        }
    }
}
