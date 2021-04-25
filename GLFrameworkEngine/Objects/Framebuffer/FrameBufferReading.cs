using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public sealed partial class Framebuffer
    {
        public System.Drawing.Bitmap ReadImagePixels(bool saveAlpha = false, ReadBufferMode bufferMode = ReadBufferMode.ColorAttachment0)
        {
            int imageSize = Width * Height * 4;

            Bind();
            byte[] pixels = ReadPixels(Width, Height, imageSize, bufferMode, saveAlpha);
            var bitmap = GetBitmap(Width, Height, pixels);

            // Adjust for differences in the origin point.
            bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }

        private static byte[] ReadPixels(int width, int height, int imageSizeInBytes, ReadBufferMode bufferMode,  bool saveAlpha)
        {
            byte[] pixels = new byte[imageSizeInBytes];

            // Read the pixels from the framebuffer. PNG uses the BGRA format. 
            GL.ReadBuffer(bufferMode);
            GL.ReadPixels(0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            if (!saveAlpha)
                SetAlphaToWhite(width, height, 4, pixels);

            return pixels;
        }

        private static void SetAlphaToWhite(int width, int height, int pixelSizeInBytes, byte[] pixels)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    int pixelIndex = w + (h * width);
                    pixels[pixelIndex * pixelSizeInBytes + 3] = 255;
                }
            }
        }

        public static Bitmap GetBitmap(int width, int height, byte[] imageData)
        {
            Bitmap bmp = new Bitmap(width, height,  System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);

            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }
}
