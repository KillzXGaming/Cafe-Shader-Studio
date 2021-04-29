using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLTextureCube : GLTexture
    {
        public GLTextureCube() : base()
        {
            Target = TextureTarget.TextureCubeMap;
        }

        public static GLTextureCube CreateEmptyCubemap(int size,
            PixelInternalFormat pixelInternalFormat = PixelInternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte, int numMips = 1)
        {
            GLTextureCube texture = new GLTextureCube();
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = size; texture.Height = size;
            texture.Target = TextureTarget.TextureCubeMap;

            texture.Bind();
            texture.WrapR = TextureWrapMode.ClampToEdge;
            texture.WrapS = TextureWrapMode.ClampToEdge;
            texture.WrapT = TextureWrapMode.ClampToEdge;
            texture.MinFilter = TextureMinFilter.LinearMipmapLinear;
            texture.MagFilter = TextureMagFilter.Linear;
            texture.MipCount = numMips;

            //Allocate mip data
            if (texture.MipCount > 1)
                texture.GenerateMipmaps();

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < texture.MipCount; j++)
                {
                    var width = CalculateMipDimension(texture.Width, j);
                    var height = CalculateMipDimension(texture.Height, j);

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j, pixelInternalFormat,
                        width, height,
                        0, pixelFormat, pixelType, IntPtr.Zero);
                }
            }
            texture.UpdateParameters();
            texture.Unbind();
            return texture;
        }

        public static GLTextureCube FromGeneric(STGenericTexture texture, ImageParameters parameters)
        {
            GLTextureCube glTexture = new GLTextureCube();
            glTexture.Target = TextureTarget.Texture2D;
            glTexture.Width = (int)texture.Width;
            glTexture.Height = (int)texture.Height;
            glTexture.LoadImage(texture, parameters);
            return glTexture;
        }

        public static GLTextureCube FromDDS(DDS dds, DDS mipLevel2)
        {
            var surfaces = dds.GetSurfaces(0, false, 6);
            var surfacesMip = mipLevel2.GetSurfaces(0, false, 6);

            int size = (int)dds.Width;

            GLTextureCube texture = new GLTextureCube();
            texture.Width = size; texture.Height = size;
            texture.Bind();

            InternalFormat format = InternalFormat.CompressedRgbaS3tcDxt5Ext;
            if (dds.Platform.OutputFormat == TexFormat.BC6H_UF16)
                format = InternalFormat.CompressedRgbBptcUnsignedFloat;
            if (dds.Platform.OutputFormat == TexFormat.BC6H_SF16)
                format = InternalFormat.CompressedRgbBptcSignedFloat;

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < dds.MipCount; j++)
                {
                    int mipWidth = CalculateMipDimension(texture.Width, j);
                    int mipHeight = CalculateMipDimension(texture.Height, j);
                    int imageSize = GLFormatHelper.CalculateImageSize(mipWidth, mipHeight, format);

                    if (j == 0)
                    {
                        GL.CompressedTexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j,
                            format,
                            mipWidth, mipHeight,
                            0, imageSize, surfaces[i].mipmaps[0]);
                    }
                    else if (j == 1)
                    {
                        GL.CompressedTexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j,
                            format,
                            mipWidth, mipHeight,
                            0, imageSize, surfacesMip[i].mipmaps[0]);
                    }
                    else
                    {
                        GL.CompressedTexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j,
                            format,
                            mipWidth, mipHeight,
                            0, imageSize, IntPtr.Zero);
                    }
                }
            }

            texture.Unbind();
            return texture;
        }

        public static GLTextureCube FromDDS(DDS dds, bool flipY = false, bool isBGRA = false)
        {
            int size = (int)dds.Width;

            GLTextureCube texture = new GLTextureCube();
            texture.Width = size; texture.Height = size;
            texture.Bind();

            InternalFormat format = InternalFormat.CompressedRgbaS3tcDxt5Ext;
            if (dds.Platform.OutputFormat == TexFormat.BC6H_UF16)
                format = InternalFormat.CompressedRgbBptcUnsignedFloat;
            if (dds.Platform.OutputFormat == TexFormat.BC6H_SF16)
                format = InternalFormat.CompressedRgbBptcSignedFloat;
            if (dds.Platform.OutputFormat == TexFormat.RGBA8_UNORM)
                format = InternalFormat.Rgba8;

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < dds.MipCount; j++)
                {
                    int mipWidth = CalculateMipDimension(texture.Width, j);
                    int mipHeight = CalculateMipDimension(texture.Height, j);
                    int imageSize = GLFormatHelper.CalculateImageSize(mipWidth, mipHeight, format);
                    var surface = dds.GetDeswizzledSurface(i, j);

                    if (dds.Parameters.UseSoftwareDecoder || flipY)
                    {
                        surface = dds.GetDecodedSurface(i, j);
                        format = InternalFormat.Rgba8;
                    }

                    if (flipY)
                        surface = FlipVertical(mipWidth, mipHeight, surface);

                    if (format == InternalFormat.Rgba8)
                    {
                        GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j,
                            PixelInternalFormat.Rgba,
                            mipWidth, mipHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
                            surface);
                    }
                    else
                    {
                        GL.CompressedTexImage2D(TextureTarget.TextureCubeMapPositiveX + i, j,
                            format,
                            mipWidth, mipHeight,
                            0, imageSize, surface);
                    }
                }
            }

            texture.Unbind();
            return texture;
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

        public override void SaveDDS(string fileName)
        {
            List<STGenericTexture.Surface> surfaces = new List<STGenericTexture.Surface>();

            Bind();

            int size = this.Width;
            for (int i = 0; i < 6; i++) {
                var surface = new STGenericTexture.Surface();
                surfaces.Add(surface);

                for (int m = 0; m < this.MipCount; m++) {
                    int mipSize = (int)(size * Math.Pow(0.5, m));
                    byte[] outputRaw = new byte[mipSize * mipSize * 4];
                    GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + i, m,
                      PixelFormat.Bgra, PixelType.UnsignedByte, outputRaw);

                    surface.mipmaps.Add(outputRaw);
                }
            }

            var dds = new DDS();
            dds.MainHeader.Width = (uint)this.Width;
            dds.MainHeader.Height = (uint)this.Height;
            dds.MainHeader.Depth = 1;
            dds.MainHeader.MipCount = (uint)this.MipCount;
            dds.MainHeader.PitchOrLinearSize = (uint)surfaces[0].mipmaps[0].Length;

            dds.SetFlags(TexFormat.RGBA8_UNORM, false, true);

            if (dds.IsDX10)
            {
                if (dds.Dx10Header == null)
                    dds.Dx10Header = new DDS.DX10Header();

                dds.Dx10Header.ResourceDim = 3;
                dds.Dx10Header.ArrayCount = 1;
            }

            dds.Save(fileName, surfaces);

            Unbind();
        }

        public void Save(string fileName)
        {
            Bind();

            var decomp = GetDecompressedRawImageData(0);
            var bitmap = BitmapImageHelper.CreateBitmap(decomp, Width, Height * 6);
            bitmap.Save(fileName);

            Unbind();
        }

        public byte[] GetDecompressedRawImageData(int level)
        {
            int size = CalculateMipDimension(Width, level);

            List<byte[]> outputs = new List<byte[]>();
            for (int i = 0; i < 6; i++)
            {
                byte[] outputRaw = new byte[size * size * 4];
                GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + i, level,
                  PixelFormat.Bgra, PixelType.UnsignedByte, outputRaw);
                outputs.Add(outputRaw);
            }
            return ByteUtils.CombineArray(outputs.ToArray());
        }

        public byte[] GetFullRawImageData(int level)
        {
            List<byte[]> outputs = new List<byte[]>();
            for (int i = 0; i < 6; i++)
            {
                byte[] outputRaw = new byte[Width * Height * 5];
                GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + i, level,
                  PixelFormat, PixelType, outputRaw);
                outputs.Add(outputRaw);
            }
            return ByteUtils.CombineArray(outputs.ToArray());
        }

        public byte[] GetImage(int index)
        {
            Bind();
            byte[] output = new byte[Width * Height * 4];

            GL.GetTexImage(TextureTarget.TextureCubeMapPositiveX + index, 0,
                 PixelFormat.Bgra, PixelType.UnsignedByte, output);

            output = SetImageData(output, Width, Height, true, true);

            Unbind();
            return output;
        }

        private byte[] SetImageData(byte[] input, int width, int height, bool flipImage, bool removeAlpha)
        {
            byte[] output = new byte[width * height * 4];
            int stride = width * 4;

            if (flipImage)
            {
                for (int y = 0; y < height; y++)
                {
                    int IOffs = stride * y;
                    int OOffs = stride * (height - 1 - y);

                    for (int x = 0; x < width; x++)
                    {
                        output[OOffs + 0] = (byte)Toolbox.Core.Utils.Clamp((int)input[IOffs + 0], 0, 255);
                        output[OOffs + 1] = (byte)Toolbox.Core.Utils.Clamp((int)input[IOffs + 1], 0, 255);
                        output[OOffs + 2] = (byte)Toolbox.Core.Utils.Clamp((int)input[IOffs + 2], 0, 255);
                        output[OOffs + 3] = (byte)Toolbox.Core.Utils.Clamp((int)(removeAlpha ? (byte)255 : input[IOffs + 3]), 0, 255);

                        IOffs += 4;
                        OOffs += 4;
                    }
                }
            }
            else if (removeAlpha)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int pixelIndex = x + (y * width);
                        output[pixelIndex * 4 + 3] = 255;
                    }
                }
            }
            return output;
        }
    }
}
