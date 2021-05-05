using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace BfresEditor
{
    public class SHARCFB : IFileFormat, IShaderFile
    {
        public bool CanSave { get; set; } = false;

        public string[] Description { get; set; } = new string[] { "SHARCFB" };
        public string[] Extension { get; set; } = new string[] { "*.sharcfb" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "BAHS");
            }
        }

        public List<ShaderProgram> Programs = new List<ShaderProgram>();

        public List<BinaryData> Binaries = new List<BinaryData>();

        public uint Version;
        public uint ByteOrderMark;

        public string Name;

        public Stream Stream { get; set; }

        public SHARCFB() { }

        public SHARCFB(string fileName)
        {
            Read(fileName);
        }

        public SHARCFB(System.IO.Stream stream)
        {
            Stream = stream;
            Read(stream);
        }

        public void Load(Stream stream) {
            Read(stream);
        }

        public void Save(Stream stream) {
            Write(stream);
        }

        public void Read(string fileName)
        {
            Read(new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read));
        }

        public void Write(string fileName)
        {
            Write(new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write));
        }

        public void Read(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true))
            {
                reader.ReadSignature(4, "BAHS");
                Version = reader.ReadUInt32();
                uint fileSize = reader.ReadUInt32();
                ByteOrderMark = reader.ReadUInt32();
                reader.ReadUInt32();
                uint nameLength = reader.ReadUInt32();
                Name = reader.ReadString((int)nameLength, true);

                var pos = reader.Position;

                uint binarySectionSize = reader.ReadUInt32();
                uint numBinaries = reader.ReadUInt32();

                for (int i = 0; i < numBinaries; i++)
                {
                    BinaryData binaryData = new BinaryData();
                    binaryData.Read(reader, Version);
                    Binaries.Add(binaryData);
                }

                reader.Seek(binarySectionSize + pos, System.IO.SeekOrigin.Begin);

                uint programSectionSize = reader.ReadUInt32();
                uint numPrograms = reader.ReadUInt32();

                for (int i = 0; i < numPrograms; i++)
                {
                    ShaderProgram programData = new ShaderProgram(this);
                    programData.Read(reader, Version);
                    Programs.Add(programData);
                }

                reader.Seek(binarySectionSize + pos, System.IO.SeekOrigin.Begin);
            }
        }

        public void Write(System.IO.Stream stream)
        {
            using (var writer = new FileWriter(stream))
            {
                writer.SetByteOrder(ByteOrderMark != 1);
                writer.WriteSignature("AAHS");
                writer.Write(Version);
                writer.Write(uint.MaxValue); //fileSize
                writer.Write(ByteOrderMark);
                writer.Write(0);
                writer.Write(Name.Length);
                writer.WriteString(Name);
            }
        }

        public class ShaderProgram
        {
            public string Name { get; set; }

            public uint Kind { get; set; }
            public int BaseIndex { get; set; }

            public VariationSymbolData VariationMacroData = new VariationSymbolData();
            public VariationSymbolData VariationDefaults = new VariationSymbolData();
            public ShaderSymbolData UniformVariables = new ShaderSymbolData();
            public ShaderSymbolData UniformBlocks = new ShaderSymbolData();
            public ShaderSymbolData SamplerVariables = new ShaderSymbolData();
            public ShaderSymbolData AttributeVariables = new ShaderSymbolData();

            public GX2Shader GetGX2Shader(ShaderType type)
            {
                if (type == ShaderType.GX2VertexShader)
                    return ParentFile.Binaries[BaseIndex].ShaderData;
                else if (type == ShaderType.GX2PixelShader)
                    return ParentFile.Binaries[BaseIndex + 1].ShaderData;
                else
                    throw new Exception($"Unsupported GX2 type! {type}");
            }

            public GX2VertexShader GetGX2VertexShader(int variationIndex) {
                return (GX2VertexShader)ParentFile.Binaries[variationIndex].ShaderData;
            }

            public GX2PixelShader GetGX2PixelShader(int variationIndex) {
                return (GX2PixelShader)ParentFile.Binaries[variationIndex + 1].ShaderData;
            }

            public Stream GetRawVertexShader(int variationIndex)
            {
                return ParentFile.Binaries[variationIndex].Data;
            }

            public Stream GetRawPixelShader(int variationIndex)
            {
                return ParentFile.Binaries[variationIndex + 1].Data;
            }

            public SHARCFB ParentFile;

            public ShaderProgram(SHARCFB shader)
            {
                ParentFile = shader;
            }

            public int GetVariationIndex(Dictionary<string, string> options)
            {
                int index = 0;
                foreach (var variation in VariationMacroData.symbols)
                {
                    if (!options.ContainsKey(variation.Name))
                        continue;

                    if (!variation.Values.Contains(options[variation.Name]))
                        throw new Exception($"Invalid option setting on {variation.Name}! {options[variation.Name]}. Valid choices: {string.Join(",", variation.Values.ToArray())}");

                    index *= variation.Values.Count;
                    index += variation.Values.IndexOf(options[variation.Name]);

                    Console.WriteLine($"Choices {variation.Name} {string.Join(",", variation.Values.ToArray())} {options[variation.Name]}");
                }

                if (HasGeometryShader())
                    return BaseIndex + index * 3;
                return BaseIndex + index * 2;
            }

            public void Read(FileReader reader, uint version)
            {
                var pos = reader.Position;

                uint SectionSize = reader.ReadUInt32();
                uint NameLength = reader.ReadUInt32();
                Kind = reader.ReadUInt32();
                BaseIndex = reader.ReadInt32();

                Name = reader.ReadString((int)NameLength, true);

                if (version >= 9)
                {
                    VariationMacroData.Read(reader);
                    VariationDefaults.Read(reader);
                    UniformVariables.Read(reader);
                    uint size = reader.ReadUInt32();
                    reader.SeekBegin(reader.Position - 4 + size);
                    UniformBlocks.Read(reader);
                    SamplerVariables.Read(reader);
                    AttributeVariables.Read(reader);
                }
                else
                {
                    VariationMacroData.Read(reader);
                    VariationDefaults.Read(reader);
                    UniformVariables.Read(reader);
                    UniformBlocks.Read(reader);
                    SamplerVariables.Read(reader);
                    AttributeVariables.Read(reader);
                }

                reader.Seek(SectionSize + pos, System.IO.SeekOrigin.Begin);
            }

            public bool HasGeometryShader()
            {
                return (Kind & 4) != 0;
            }

            public bool HasPixelShader()
            {
                return (Kind & 2) != 0;
            }

            public bool HasVertexShader()
            {
                return (Kind & 1) != 0;
            }

            public enum ShaderType
            {
                GX2VertexShader,
                GX2PixelShader,
                GX2GeometryShader,
            }
        }

        public class BinaryData
        {
            public ShaderType Type;
            public Stream Data { get; set; }

            public byte[] DataBytes
            {
                get
                {
                    using (var binaryReader = new FileReader(Data, true)) {
                        return binaryReader.ReadBytes((int)binaryReader.Length);
                    }
                }
            }

            private GX2Shader gx2Shader;

            public GX2Shader ShaderData
            {
                get { return GetGX2Shader(); }
            }

            public uint Offset;

            public void Read(FileReader reader, uint version)
            {
                var pos = reader.Position;

                uint SectionSize = reader.ReadUInt32();
                Type = reader.ReadEnum<ShaderType>(true);
                Offset = reader.ReadUInt32();
                uint BinarySize = reader.ReadUInt32();
                Data = new SubStream(reader.BaseStream, reader.Position, BinarySize);

                reader.Seek(SectionSize + pos, System.IO.SeekOrigin.Begin);
            }

            public GX2Shader GetGX2Shader()
            {
                if (gx2Shader != null)
                    return gx2Shader;

                using (var binaryReader = new FileReader(Data))
                {
                    switch (Type)
                    {
                        case ShaderType.GX2VertexShader:
                            gx2Shader = new GX2VertexShader(binaryReader, 0);
                            break;
                        case ShaderType.GX2PixelShader:
                            gx2Shader = new GX2PixelShader(binaryReader, 0);
                            break;
                    }
                }
                return gx2Shader;
            }

            public void Write(FileWriter writer)
            {
                var pos = writer.Position;

                writer.Write(uint.MaxValue);
                writer.Write(Type, true);
                writer.Write(Offset);
                ShaderData.Write(writer);
                SharcCommon.WriteSectionSize(writer, pos);
            }

            public enum ShaderType
            {
                GX2VertexShader,
                GX2PixelShader,
                GX2GeometryShader,
            }
        }
    }
}
