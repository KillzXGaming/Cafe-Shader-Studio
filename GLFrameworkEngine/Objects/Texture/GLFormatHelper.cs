using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Toolbox.Core;

namespace GLFrameworkEngine
{
    public class GLFormatHelper
    {
        public static PixelFormatInfo ConvertPixelFormat(TexFormat format)
        {
            if (!PixelFormatList.ContainsKey(format))
                return PixelFormatList[TexFormat.RGBA8_UNORM];

            return PixelFormatList[format];
        }

        public static InternalFormat ConvertCompressedFormat(TexFormat format, bool useSRGB) {
            return InternalFormatList[format];
        }

        public static int CalculateImageSize(int width, int height, InternalFormat format)
        {
            if (format == InternalFormat.Rgba8)
                return width * height * 4;

            int blockSize = blockSizeByFormat[format.ToString()];

            int imageSize = blockSize * (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0);
            return imageSize;
        }

        static readonly Dictionary<TexFormat, PixelFormatInfo> PixelFormatList = new Dictionary<TexFormat, PixelFormatInfo>
        {
            { TexFormat.RG11B10_FLOAT, new PixelFormatInfo(All.R11fG11fB10f, PixelFormat.Rgb, PixelType.UnsignedInt10F11F11FRev) },
            { TexFormat.RGBA8_UNORM, new PixelFormatInfo(All.Rgba, PixelFormat.Bgra, PixelType.UnsignedByte) },
            { TexFormat.RGBA8_SRGB, new PixelFormatInfo(All.SrgbAlpha, PixelFormat.Bgra, PixelType.UnsignedByte) },
            { TexFormat.RGBA32_FLOAT, new PixelFormatInfo(All.Rgba32f, PixelFormat.Rgba, PixelType.Float) },
            { TexFormat.R8_UNORM, new PixelFormatInfo(All.R8, PixelFormat.Red, PixelType.UnsignedByte) },
            { TexFormat.RG8_UNORM, new PixelFormatInfo(All.Rg8, PixelFormat.Rg, PixelType.UnsignedByte) },
            { TexFormat.RG8_SNORM, new PixelFormatInfo(All.Rg8Snorm, PixelFormat.Rg, PixelType.Byte) },
            { TexFormat.RG8_UINT, new PixelFormatInfo(All.Rg8ui, PixelFormat.RgInteger, PixelType.UnsignedByte) },
            { TexFormat.RG8_SINT, new PixelFormatInfo(All.Rg8i, PixelFormat.RgInteger, PixelType.Byte) },
            { TexFormat.RG16_FLOAT, new PixelFormatInfo(All.Rg16, PixelFormat.Rg, PixelType.HalfFloat) },
            { TexFormat.RGB565_UNORM, new PixelFormatInfo( All.Rgb565, PixelFormat.Rgb, PixelType.UnsignedShort565Reversed) },
        };

        static readonly Dictionary<TexFormat, InternalFormat> InternalFormatList = new Dictionary<TexFormat, InternalFormat>
        {
            { TexFormat.BC1_UNORM, InternalFormat.CompressedRgbaS3tcDxt1Ext },
            { TexFormat.BC1_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt1Ext },
            { TexFormat.BC2_UNORM, InternalFormat.CompressedRgbaS3tcDxt3Ext },
            { TexFormat.BC2_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt3Ext },
            { TexFormat.BC3_UNORM, InternalFormat.CompressedRgbaS3tcDxt5Ext },
            { TexFormat.BC3_SRGB, InternalFormat.CompressedSrgbAlphaS3tcDxt5Ext },
            { TexFormat.BC4_UNORM, InternalFormat.CompressedRedRgtc1 },
            { TexFormat.BC4_SNORM, InternalFormat.CompressedSignedRedRgtc1 },
            { TexFormat.BC5_UNORM, InternalFormat.CompressedRgRgtc2 },
            { TexFormat.BC5_SNORM, InternalFormat.CompressedSignedRgRgtc2 },
            { TexFormat.BC6H_UF16, InternalFormat.CompressedRgbBptcUnsignedFloat },
            { TexFormat.BC6H_SF16, InternalFormat.CompressedRgbBptcSignedFloat },
            { TexFormat.BC7_UNORM, InternalFormat.CompressedRgbaBptcUnorm },
            { TexFormat.BC7_SRGB, InternalFormat.CompressedSrgbAlphaBptcUnorm },
        };

        static readonly Dictionary<string, int> blockSizeByFormat = new Dictionary<string, int>
        {
            { "CompressedR11Eac",                       8},
            { "CompressedRedRgtc1",                     8},
            { "CompressedRedRgtc1Ext",                  8},
            { "CompressedRg11Eac",                     16},
            { "CompressedRgRgtc2",                     16},
            { "CompressedRgb8Etc2",                     8},
            { "CompressedRgb8PunchthroughAlpha1Etc2",   8},
            { "CompressedRgbBptcSignedFloat",          16},
            { "CompressedRgbBptcUnsignedFloat",        16},
            { "CompressedRgbS3tcDxt1Ext",               8},
            { "CompressedRgba8Etc2Eac",                16},
            { "CompressedRgbaBptcUnorm",               16},
            { "CompressedRgbaS3tcDxt1Ext",              8},
            { "CompressedRgbaS3tcDxt3Ext",             16},
            { "CompressedRgbaS3tcDxt5Ext",             16},
            { "CompressedSignedR11Eac",                 8},
            { "CompressedSignedRedRgtc1",               8},
            { "CompressedSignedRedRgtc1Ext",            8},
            { "CompressedSignedRg11Eac",               16},
            { "CompressedSignedRgRgtc2",               16},
            { "CompressedSrgb8Alpha8Etc2Eac",          16},
            { "CompressedSrgb8Etc2",                    8},
            { "CompressedSrgb8PunchthroughAlpha1Etc2",  8},
            { "CompressedSrgbAlphaBptcUnorm",          16},
            { "CompressedSrgbAlphaS3tcDxt1Ext",         8},
            { "CompressedSrgbAlphaS3tcDxt3Ext",        16},
            { "CompressedSrgbAlphaS3tcDxt5Ext",        16},
            { "CompressedSrgbS3tcDxt1Ext",              8}
        };

        public class PixelFormatInfo
        {
            public PixelFormat Format { get; set; }
            public PixelInternalFormat InternalFormat { get; set; }
            public PixelType Type { get; set; }

            public PixelFormatInfo(All internalFormat, PixelFormat format, PixelType type) {
                InternalFormat = (PixelInternalFormat)internalFormat;
                Format = format;
                Type = type;
            }
        }
    }
}
