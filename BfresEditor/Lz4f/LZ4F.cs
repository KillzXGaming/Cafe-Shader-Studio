using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace CompressionLibrary
{
    public class LZ4F : ICompressionFormat
    {
        public string[] Description { get; set; } = new string[] { "LZ4F Compression" };
        public string[] Extension { get; set; } = new string[] { "*.cmp", "*.lz4f" };

        public override string ToString() { return "LZ4F"; }

        public bool Identify(Stream stream, string fileName)
        {
            if (stream.Length < 12) return false;

            using (var reader = new FileReader(stream, true))
            {
                uint DecompressedSize = reader.ReadUInt32();
                uint magicCheck = reader.ReadUInt32();

                bool LZ4FDefault = magicCheck == 0x184D2204;

                return LZ4FDefault;
            }
        }

        public bool CanCompress { get; } = true;

        public Stream Decompress(Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                reader.Position = 0;
                int OuSize = reader.ReadInt32();
                int InSize = (int)stream.Length - 4;
                var dec = Decompress(reader.getSection(4, InSize));
                return new MemoryStream(dec);
            }
        }

        public Stream Compress(Stream stream)
        {
            var mem = new MemoryStream();
            using (var writer = new System.IO.BinaryWriter(mem))
            {
                writer.Write((uint)stream.Length);
                byte[] buffer = LZ4.Frame.LZ4Frame.Compress(stream,
                    LZ4.Frame.LZ4MaxBlockSize.MB1, true, true, false, true, false);

                writer.Write(buffer, 0, buffer.Length);
            }
            return mem;
        }

        public static byte[] Decompress(byte[] data)
        {
            return LZ4.Frame.LZ4Frame.Decompress(new MemoryStream(data));

           /* using (MemoryStream mem = new MemoryStream())
            {
                using (var source = LZ4Stream.Decode(new MemoryStream(data)))
                {
                    source.CopyTo(mem);
                }
                return mem.ToArray();
            }*/
        }
    
    }
}