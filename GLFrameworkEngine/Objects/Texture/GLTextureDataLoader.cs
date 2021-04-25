using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLTextureDataLoader
    {
        public static void LoadCompressedImage(TextureTarget target, int width, int height,
            int depth, InternalFormat format, byte[] data, int mipLevel = 0)
        {
            switch (target)
            {
                case TextureTarget.Texture2D:
                    LoadCompressedImage2D(mipLevel, width, height, format, data);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    LoadCompressedImage3D(target, mipLevel, depth, width, height, format, data);
                    break;
            }
        }

        public static void LoadImage(TextureTarget target, int width, int height,
           int depth, GLFormatHelper.PixelFormatInfo format, byte[] data, int mipLevel = 0)
        {
            switch (target)
            {
                case TextureTarget.Texture2D:
                    LoadImage2D(mipLevel, width, height, format, data);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    LoadImage3D(target, mipLevel, depth, width, height, format, data);
                    break;
            }
        }

        public static void LoadImage(TextureTarget target, int width, int height,
               int depth, GLFormatHelper.PixelFormatInfo format, Bitmap bitmap, int mipLevel = 0)
        {
            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
           ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            switch (target)
            {
                case TextureTarget.Texture2D:
                    LoadImage2D(mipLevel, width, height, format, data.Scan0);
                    break;
                case TextureTarget.Texture2DArray:
                case TextureTarget.Texture3D:
                    LoadImage3D(target, mipLevel, depth, width, height, format, data.Scan0);
                    break;
            }

            bitmap.UnlockBits(data);
        }

        static void LoadCompressedImage2D(int mipLevel, int width, int height, InternalFormat format, byte[] data)
        {
            int imageSize = GLFormatHelper.CalculateImageSize(width, height, format);

            GL.CompressedTexImage2D(TextureTarget.Texture2D, mipLevel,
                format, width, height, 0, imageSize, data);
        }

        static void LoadCompressedImage3D(TextureTarget target, int mipLevel, int depth, int width, int height, InternalFormat format, byte[] data)
        {
            int imageSize = GLFormatHelper.CalculateImageSize(width, height, format);

            GL.CompressedTexImage3D(target, mipLevel,
                format, width, height, depth, 0, imageSize, data);
        }

        static void LoadImage2D(int mipLevel, int width, int height, GLFormatHelper.PixelFormatInfo formatInfo, byte[] data)
        {
            GL.TexImage2D(TextureTarget.Texture2D, mipLevel, formatInfo.InternalFormat, width, height, 0,
                  formatInfo.Format, formatInfo.Type, data);
        }

        static void LoadImage2D(int mipLevel, int width, int height, GLFormatHelper.PixelFormatInfo formatInfo, IntPtr data)
        {
            GL.TexImage2D(TextureTarget.Texture2D, mipLevel, formatInfo.InternalFormat, width, height, 0,
                  formatInfo.Format, formatInfo.Type, data);
        }

        static void LoadImage3D(TextureTarget target, int mipLevel, int depth, int width, int height, GLFormatHelper.PixelFormatInfo formatInfo, byte[] data)
        {
            GL.TexImage3D(target, mipLevel, formatInfo.InternalFormat, width, height, depth, 0,
              formatInfo.Format, formatInfo.Type, data);

        }

        static void LoadImage3D(TextureTarget target, int mipLevel, int depth, int width, int height, GLFormatHelper.PixelFormatInfo formatInfo, IntPtr data)
        {
            GL.TexImage3D(target, mipLevel, formatInfo.InternalFormat, width, height, depth, 0,
                 formatInfo.Format, formatInfo.Type, data);
        }
    }
}
