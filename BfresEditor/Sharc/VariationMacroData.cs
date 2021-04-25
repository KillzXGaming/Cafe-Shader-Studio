using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace BfresEditor
{
    public class VariationMacroData
    {
        public List<VariationMacro> macros = new List<VariationMacro>();

        public string TryGetValue(string name)
        {
            for (int i = 0; i < macros.Count; i++)
                if (macros[i].Name == name)
                    return macros[i].Value;
            return "";
        }

        public void AddMacro(string name, string value) {
            macros.Add(new VariationMacro() { Name = name, Value = value });
        }

        public void Read(FileReader reader, uint Version = 12)
        {
            var SectionPos = reader.Position;
            uint SectionSize = reader.ReadUInt32();
            uint SectionCount = reader.ReadUInt32();

            for (int i = 0; i < SectionCount; i++)
            {
                VariationMacro variation = new VariationMacro();
                variation.Read(reader, Version);
                macros.Add(variation);
            }
            reader.Seek(SectionPos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer, uint Version = 12)
        {
            long pos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(macros.Count);

            for (int i = 0; i < macros.Count; i++)
                macros[i].Write(writer, Version);

            SharcCommon.WriteSectionSize(writer, pos);
        }
    }

    public class VariationMacro
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public void Read(FileReader reader, uint Version)
        {
            var pos = reader.Position;
            uint SectionSize = reader.ReadUInt32();

            uint NameLength = reader.ReadUInt32();
            uint ValueLength = reader.ReadUInt32();
            Name = reader.ReadString((int)NameLength, true);
            Value = reader.ReadString((int)ValueLength, true);
            reader.Seek(pos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer, uint Version)
        {
            var pos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(Name.Length + 1);
            writer.Write(Value.Length + 1);
            writer.WriteString(Name);
            writer.WriteString(Value);
            SharcCommon.WriteSectionSize(writer, pos);
        }
    }
}
