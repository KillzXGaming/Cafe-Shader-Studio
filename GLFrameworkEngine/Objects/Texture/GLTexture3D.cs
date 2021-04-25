using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTexture3D : GLTexture
    {
        public int Depth { get; set; }

        public GLTexture3D() : base()
        {
            Target = TextureTarget.Texture3D;
        }

        public static GLTexture3D CreateUncompressedTexture(int width, int height, int depth,
            PixelInternalFormat format = PixelInternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTexture3D texture = new GLTexture3D();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = width; 
            texture.Height = height;
            texture.Depth = depth;
            texture.Target = TextureTarget.Texture3D;
            texture.Bind();

            GL.TexImage3D(TextureTarget.Texture3D, 0, format,
                texture.Width, texture.Height, texture.Depth,
                0, pixelFormat, pixelType, IntPtr.Zero);

            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }

        public static GLTexture3D FromGeneric(STGenericTexture texture, ImageParameters parameters)
        {
            GLTexture3D glTexture = new GLTexture3D();
            glTexture.Target = TextureTarget.Texture3D;
            glTexture.Width = (int)texture.Width;
            glTexture.Height = (int)texture.Height;
            glTexture.LoadImage(texture, parameters);
            return glTexture;
        }

        public void Save(string fileName)
        {
            Bind();

            var bmp = ToBitmap();
            bmp.Save(fileName);

            Unbind();
        }

        public override System.Drawing.Bitmap ToBitmap(bool saveAlpha = true)
        {
            Bind();

            byte[] data = new byte[Width * Height * Depth * 4];
            GL.GetTexImage(Target, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data);

            Unbind();

            return Toolbox.Core.Imaging.BitmapExtension.CreateBitmap(
                data, Width * Depth, Height);
        }
    }
}
