using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTextureCubeArray : GLTexture
    {
        public GLTextureCubeArray() : base()
        {
            Target = TextureTarget.TextureCubeMapArray;
        }

        public static GLTextureCubeArray CreateEmptyCubemap(int size,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTextureCubeArray texture = new GLTextureCubeArray();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = size; texture.Height = size;
            texture.Target = TextureTarget.TextureCubeMapArray;
            texture.MinFilter = TextureMinFilter.LinearMipmapLinear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.Bind();

            GL.TexImage3D(texture.Target, 0, pixelInternalFormat, texture.Width, texture.Height, 6, 0,
                pixelFormat, pixelType, IntPtr.Zero);

            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }

        public static GLTextureCubeArray FromGeneric(STGenericTexture texture, ImageParameters parameters)
        {
            GLTextureCubeArray glTexture = new GLTextureCubeArray();
            glTexture.Target = TextureTarget.Texture2D;
            glTexture.Width = (int)texture.Width;
            glTexture.Height = (int)texture.Height;
            glTexture.LoadImage(texture, parameters);
            return glTexture;
        }

        public static GLTextureCubeArray FromDDS(DDS dds)
        {
            int size = (int)dds.Width;

            GLTextureCubeArray texture = new GLTextureCubeArray();
            texture.Width = size;
            texture.Height = size;
            texture.Bind();

            var format = dds.Platform.OutputFormat;

            var surfaces = dds.GetSurfaces();
            List<byte[]> cubemapSurfaces = new List<byte[]>();
            for (int a = 0; a < surfaces.Count; a++)
                cubemapSurfaces.Add(surfaces[a].mipmaps[0]);

            int depth = surfaces.Count;
            Console.WriteLine($"depth {depth}");

            byte[] buffer = ByteUtils.CombineArray(cubemapSurfaces.ToArray());

            for (int j = 0; j < dds.MipCount; j++)
            {
                int mipWidth = CalculateMipDimension(texture.Width, j);
                int mipHeight = CalculateMipDimension(texture.Height, j);

                if (dds.IsBCNCompressed())
                {
                    var internalFormat = GLFormatHelper.ConvertCompressedFormat(format, true);
                    GLTextureDataLoader.LoadCompressedImage(texture.Target, mipWidth, mipHeight, depth, internalFormat, buffer, j);
                }
                else
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(format);
                    GLTextureDataLoader.LoadImage(texture.Target, mipWidth, mipHeight, depth, formatInfo, buffer, j);
                }
            }

            GL.TexParameter(texture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(texture.Target, TextureParameterName.TextureMaxLevel, 13);
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMapArray);

            texture.Unbind();
            return texture;
        }

        public override void SaveDDS(string fileName)
        {
            List<STGenericTexture.Surface> surfaces = new List<STGenericTexture.Surface>();

            Bind();

            int arrayCount = 1;

            int size = this.Width;
            for (int i = 0; i < 6 * arrayCount; i++)
            {
                var surface = new STGenericTexture.Surface();
                surfaces.Add(surface);

                for (int m = 0; m < MipCount; m++)
                {
                    int mipSize = (int)(size * Math.Pow(0.5, m));
                    byte[] outputRaw = new byte[mipSize * mipSize * 4];
                    GL.GetTextureSubImage(this.ID, m, 0, 0, i, Width, Height, 1,
                      PixelFormat.Bgra, PixelType.UnsignedByte, outputRaw.Length, outputRaw);

                    surface.mipmaps.Add(outputRaw);
                }
            }

            var dds = new DDS();
            dds.MainHeader.Width = (uint)this.Width;
            dds.MainHeader.Height = (uint)this.Height;
            dds.MainHeader.Depth = 1;
            dds.MainHeader.MipCount = (uint)this.MipCount;
            dds.MainHeader.PitchOrLinearSize = (uint)surfaces[0].mipmaps[0].Length;

            dds.SetFlags(TexFormat.RGBA8_UNORM, arrayCount > 1, true);

            if (dds.IsDX10)
            {
                if (dds.Dx10Header == null)
                    dds.Dx10Header = new DDS.DX10Header();

                dds.Dx10Header.ResourceDim = 3;
                dds.Dx10Header.ArrayCount = (uint)arrayCount;
            }

            dds.Save(fileName, surfaces);

            Unbind();
        }

        public void Save(string fileName)
        {
            Bind();
            for (int i = 0; i < 6; i++)
            {
                byte[] output = new byte[Width * Height * 4];
                GL.GetTextureSubImage((int)this.Target, 0, 0, 0, i, Width, Height, 1,
                PixelFormat.Bgra, PixelType.UnsignedByte, output.Length, output);

                //Remove alpha
                output = SetImageData(output, true, true);

                var bitmap = BitmapImageHelper.CreateBitmap(output, Width, Height);
                bitmap.Save(fileName + $"_{i}.png");
            }
            Unbind();
        }

        public byte[] GetImage(int index)
        {
            Bind();
            byte[] output = new byte[Width * Height * 4];

            GL.GetTextureSubImage((int)this.Target, 0, 0, 0, index, Width, Height, 1,
              PixelFormat.Bgra, PixelType.UnsignedByte, output.Length, output);

            output = SetImageData(output, true, true);

            Unbind();
            return output;
        }

        private byte[] SetImageData(byte[] input, bool flipImage, bool removeAlpha)
        {
            byte[] output = new byte[Width * Height * 4];
            int stride = Width * 4;

            if (flipImage)
            {
                for (int y = 0; y < Height; y++)
                {
                    int IOffs = stride * y;
                    int OOffs = stride * (Height - 1 - y);

                    for (int x = 0; x < Width; x++)
                    {
                        output[OOffs + 0] = input[IOffs + 0];
                        output[OOffs + 1] = input[IOffs + 1];
                        output[OOffs + 2] = input[IOffs + 2];
                        output[OOffs + 3] = removeAlpha ? (byte)255 : input[IOffs + 3];

                        IOffs += 4;
                        OOffs += 4;
                    }
                }
            }
            else if (removeAlpha)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        int pixelIndex = x + (y * Width);
                        output[pixelIndex * 4 + 3] = 255;
                    }
                }
            }
            return output;
        }
    }
}
