using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class SHARC
    {
        public Dictionary<string, UniformBlock> Blocks = new Dictionary<string, UniformBlock>();

        public string Name;
        public uint Version = 11;
        public List<SourceData> SourceDatas = new List<SourceData>();
        public List<ShaderProgram> ShaderPrograms = new List<ShaderProgram>();

        public uint ByteOrderMark = 1;

        public static SHARC Load(Stream stream)
        {
            SHARC shader = new SHARC();
            shader.Read(stream);
            return shader;
        }

        public void Read(string fileName) {
            Read(new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read));
        }

        public void Write(string fileName) {
            Write(new System.IO.FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write));
        }

        public void Read(System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream))
            {
                reader.ReadSignature(4, "AAHS");
                Version = reader.ReadUInt32();
                uint fileSize = reader.ReadUInt32();
                ByteOrderMark = reader.ReadUInt32();
                uint nameLength = reader.ReadUInt32();
                Name = reader.ReadString((int)nameLength, true);

                var startPos = reader.Position;

                uint SourceArrayOffset = reader.ReadUInt32();
                uint ProgramCount = reader.ReadUInt32();
                for (int i = 0; i < ProgramCount; i++)
                    ShaderPrograms.Add(new ShaderProgram(this, reader, Version));

                reader.Seek(startPos + SourceArrayOffset, System.IO.SeekOrigin.Begin);

                uint SourceSecSize = reader.ReadUInt32();
                uint SourceFileCount = reader.ReadUInt32();
                for (int i = 0; i < SourceFileCount; i++)
                    SourceDatas.Add(new SourceData(reader, Version));
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
                writer.Write(Name.Length + 1);
                writer.WriteString(Name);
                long programStartPos = writer.Position;
                writer.Write(uint.MaxValue);
                writer.Write(ShaderPrograms.Count);
                for (int i = 0; i < ShaderPrograms.Count; i++)
                {
                    long pos = writer.Position;
                    ShaderPrograms[i].Write(writer, Version);
                    SharcCommon.WriteSectionSize(writer, pos);
                }

                SharcCommon.WriteSectionSize(writer, programStartPos);

                long sourceArrayStart = writer.Position;
                writer.Write(uint.MaxValue);
                writer.Write(SourceDatas.Count);
                for (int i = 0; i < SourceDatas.Count; i++)
                {
                    long pos = writer.Position;
                    SourceDatas[i].Write(writer, Version);
                    SharcCommon.WriteSectionSize(writer, pos);
                }

                SharcCommon.WriteSectionSize(writer, sourceArrayStart);

                using (writer.TemporarySeek(8, System.IO.SeekOrigin.Begin))
                {
                    writer.Write((uint)writer.BaseStream.Length);
                };
            }
        }

        public class ShaderProgram
        {
            public string Name { get; set; }

            public VariationMacroData variationVertexMacroData = new VariationMacroData();
            public VariationMacroData variationFragmenMacroData = new VariationMacroData();
            public VariationMacroData variationGeometryMacroData = new VariationMacroData();
            public VariationMacroData variationComputeMacroData = new VariationMacroData();

            public VariationSymbolData variationSymbolData = new VariationSymbolData();
            public VariationSymbolData variationSymbolDataFull = new VariationSymbolData();

            public ShaderSymbolData UniformVariables = new ShaderSymbolData();
            public ShaderSymbolData UniformBlocks = new ShaderSymbolData();
            public ShaderSymbolData SamplerVariables = new ShaderSymbolData();
            public ShaderSymbolData AttributeVariables = new ShaderSymbolData();

            public int VertexShaderIndex = -1;
            public int FragmentShaderIndex = -1;
            public int GeoemetryShaderIndex = -1;

            public SHARC ParentSharc;

            public ShaderProgram() { }

            public ShaderProgram(SHARC sharc, FileReader reader, uint version)
            {
                ParentSharc = sharc;
                var pos = reader.Position;

                uint SectionSize = reader.ReadUInt32();
                uint NameLength = reader.ReadUInt32();
                VertexShaderIndex = reader.ReadInt32();
                FragmentShaderIndex = reader.ReadInt32();
                GeoemetryShaderIndex = reader.ReadInt32();
                Name = reader.ReadString((int)NameLength, true);

                variationVertexMacroData.Read(reader, version);
                variationFragmenMacroData.Read(reader, version);
                variationGeometryMacroData.Read(reader, version);
                if (version >= 13)
                    variationComputeMacroData.Read(reader, version);

                variationSymbolData.Read(reader);

                if (version >= 11)
                    variationSymbolDataFull.Read(reader);

                if (version <= 12)
                {
                    UniformVariables.Read(reader);
                    if (version >= 11)
                        UniformBlocks.Read(reader, version);
                    SamplerVariables.Read(reader);
                    AttributeVariables.Read(reader);
                }

                reader.Seek(SectionSize + pos, System.IO.SeekOrigin.Begin);
            }

            public void Write(FileWriter writer, uint version)
            {
                writer.Write(uint.MaxValue);
                writer.Write(Name.Length + 1);
                writer.Write(VertexShaderIndex);
                writer.Write(FragmentShaderIndex);
                writer.Write(GeoemetryShaderIndex);
                writer.WriteString(Name);

                variationVertexMacroData.Write(writer, version);
                variationFragmenMacroData.Write(writer, version);
                variationGeometryMacroData.Write(writer, version);

                if (version >= 13)
                    variationComputeMacroData.Write(writer, version);

                variationSymbolData.Write(writer);

                if (version >= 11)
                    variationSymbolDataFull.Write(writer);

                if (version <= 12)
                {
                    UniformVariables.Write(writer);

                    if (version >= 11)
                        UniformBlocks.Write(writer, version);

                    SamplerVariables.Write(writer);
                    AttributeVariables.Write(writer);
                }
            }
        }
    }

    public class SourceData
    {
        public string Code;

        public string Name { get; set; }

        public SourceData() { }

        public SourceData(FileReader reader, uint version)
        {
            var pos = reader.Position;

            uint SectioSize = reader.ReadUInt32();
            uint FileNameLength = reader.ReadUInt32();
            uint CodeLength = reader.ReadUInt32();
            uint CodeLength2 = reader.ReadUInt32();
            Name = reader.ReadString((int)FileNameLength, true);
            byte[] data = reader.ReadBytes((int)CodeLength);
            Code = Encoding.GetEncoding("shift_jis").GetString(data);

            reader.Seek(SectioSize + pos, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer, uint version)
        {
            var data = Encoding.GetEncoding("shift_jis").GetBytes(Code);

            writer.Write(uint.MaxValue);
            writer.Write(Name.Length + 1);
            writer.Write(data.Length);
            writer.Write(data.Length);
            writer.WriteString(Name);
            writer.Write(data);
        }
    }
}