using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace BfresEditor
{
    public class ShaderSymbolData
    {
        public List<ShaderSymbol> symbols = new List<ShaderSymbol>();

        public void AddSymbol(string name, string symbol, byte flags, byte[] defaultValue = null, int offset = -1) {
            AddSymbol(name, symbol, new byte[] { flags }, defaultValue, offset);
        }

        public void AddSymbol(string name, string symbol, byte[] flags, byte[] defaultValue = null, int offset = -1) {
            symbols.Add(new ShaderSymbol()
            {
                Name = name,
                SymbolName = symbol,
                DefaultValue = defaultValue != null ? defaultValue : new byte[0],
                flags = flags != null ? flags : new byte[0],
                Offset = offset,
            });
        }

        public void Read(FileReader reader, uint Version = 12, bool SkipReading = false)
        {
            var SectionPos = reader.Position;
            uint SectionSize = reader.ReadUInt32();
            uint SectionCount = reader.ReadUInt32();

            if (!SkipReading)
            {
                for (int i = 0; i < SectionCount; i++)
                {
                    ShaderSymbol symbol = new ShaderSymbol();
                    symbol.Read(reader, Version);
                    symbols.Add(symbol);
                }
            }

            reader.Seek(SectionPos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer, uint Version = 12)
        {
            var pos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(symbols.Count);
            for (int i = 0; i < symbols.Count; i++)
                symbols[i].Write(writer);

            SharcCommon.WriteSectionSize(writer, pos);
        }
    }

    public class ShaderSymbol
    {
        public int Offset = -1;
        public string Name { get; set; }
        public byte[] DefaultValue { get; set; }
        public string SymbolName { get; set; }
        public byte[] flags;

        public string DefaultValueString
        {
            get
            {
                return DefaultValueToString();
            }
        }

        private string DefaultValueToString()
        {
            if (DefaultValue != null)
            {
                using (var reader = new FileReader(DefaultValue))
                {
                    if (DefaultValue.Length == 32)
                    {
                        float[] values = reader.ReadSingles(4);
                        return $"{values[0]},{values[1]},{values[2]},{values[3]}" +
                               $"{values[4]},{values[5]},{values[6]},{values[7]}";
                    }
                    if (DefaultValue.Length == 16)
                    {
                        float[] values = reader.ReadSingles(4);
                        return $"{values[0]},{values[1]},{values[2]},{values[3]}";
                    }
                    if (DefaultValue.Length == 12)
                    {
                        float[] values = reader.ReadSingles(3);
                        return $"{values[0]},{values[1]},{values[2]}";
                    }
                    if (DefaultValue.Length == 8)
                    {
                        float[] values = reader.ReadSingles(3);
                        return $"{values[0]},{values[1]}";
                    }
                    if (DefaultValue.Length == 4)
                    {
                        float[] values = reader.ReadSingles(3);
                        return $"{values[0]}}}";
                    }

                    return reader.ReadString(DefaultValue.Length);
                }
            }
            else
            {
                return "";
            }
        }

        public void Read(FileReader reader, uint Version)
        {
            var pos = reader.Position;
            uint SectionSize = reader.ReadUInt32();

            Offset = reader.ReadInt32();
            uint variationNameLength = reader.ReadUInt32();
            uint symbolNameLength = reader.ReadUInt32();
            uint defaultValueLength = reader.ReadUInt32();
            uint variationCount = reader.ReadUInt32();

            Name = reader.ReadString((int)variationNameLength, true);
            SymbolName = reader.ReadString((int)symbolNameLength, true);
            DefaultValue = reader.ReadBytes((int)defaultValueLength);
            flags = reader.ReadBytes((int)variationCount);
            reader.Seek(pos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer)
        {
            var pos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(Offset);
            writer.Write(Name.Length + 1);
            writer.Write(SymbolName.Length + 1);
            writer.Write(DefaultValue.Length);
            writer.Write(flags.Length);
            writer.WriteString(Name);
            writer.WriteString(SymbolName);
            writer.Write(DefaultValue);
            writer.Write(flags);
            SharcCommon.WriteSectionSize(writer, pos);
        }

    }
}
