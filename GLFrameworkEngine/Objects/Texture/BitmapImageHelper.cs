using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GLFrameworkEngine
{
    public class BitmapImageHelper
    {
        public static Bitmap CreateBitmap(byte[] Buffer, int Width, int Height, PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
        {
            Rectangle Rect = new Rectangle(0, 0, Width, Height);

            Bitmap Img = new Bitmap(Width, Height, pixelFormat);

            BitmapData ImgData = Img.LockBits(Rect, ImageLockMode.WriteOnly, Img.PixelFormat);

            if (Buffer.Length > ImgData.Stride * Img.Height)
                throw new Exception($"Invalid Buffer Length ({Buffer.Length})!!!");

            Marshal.Copy(Buffer, 0, ImgData.Scan0, Buffer.Length);

            Img.UnlockBits(ImgData);

            return Img;
        }
    }
}
