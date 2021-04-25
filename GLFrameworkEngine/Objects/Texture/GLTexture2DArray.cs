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

        public static GLTexture2DArray CreateUncompressedTexture(int width, int height,
            PixelInternalFormat format = PixelInternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTexture2DArray texture = new GLTexture2DArray();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = width; texture.Height = height;
            texture.Target = TextureTarget.Texture2DArray;
            texture.Bind();

            GL.TexImage3D(TextureTarget.Texture2DArray, 0, format,
                texture.Width, texture.Height, 1,
                0, pixelFormat, pixelType, IntPtr.Zero);

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
