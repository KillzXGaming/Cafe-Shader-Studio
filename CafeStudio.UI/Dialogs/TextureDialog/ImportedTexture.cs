using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.Imaging;
using System.Drawing;

namespace CafeStudio.UI
{
    public class ImportedTexture
    {
        public TexFormat TargetOutput { get; set; }

        public string Name { get; set; }

        public string SourceFilePath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int MipCount { get; set; }

        //Cache the encoded data so it can be applied when dialog is finished.
        //This prevents re encoding again saving time.
        private byte[] EncodeCache = new byte[0];

        private byte[] rgba;

        public ImportedTexture(string fileName) {
            Name = Path.GetFileNameWithoutExtension(fileName);
            SourceFilePath = fileName;
            TargetOutput = TexFormat.RGBA8_UNORM;

            var bitmap = new Bitmap(SourceFilePath);
            Width = bitmap.Width;
            Height = bitmap.Height;

            var rgbaImage = BitmapExtension.SwapBlueRedChannels(bitmap);
            rgba = BitmapExtension.ImageToByte(rgbaImage);

            bitmap.Dispose();
        }

        private byte[] GetSourceInBytes()
        {
            return rgba;
        }

        public byte[] DecodeTexture()
        {
            //Get the encoded data and turn back into raw rgba data for previewing purposes
            var source = EncodeCache;
            byte[] decoded = new byte[Width * Height * 4];

            foreach (var decoder in FileManager.GetTextureDecoders())
            {
                if (decoder.Decode(TargetOutput, source, Width, Height, out decoded))
                    return decoded;
            }
            return decoded;
        }

        public byte[] EncodeTexture()
        {
            //Get the source file's RGBA data
            var source = GetSourceInBytes();

            foreach (var decoder in FileManager.GetTextureDecoders()) {
                if (decoder.CanEncode(TargetOutput)) {
                    decoder.Encode(TargetOutput, source, Width, Height, out EncodeCache);
                    if (EncodeCache == null)
                        throw new Exception($"Failed to encode image! {TargetOutput}");

                    break;
                }
            }
            return EncodeCache;
        }
    }
}
