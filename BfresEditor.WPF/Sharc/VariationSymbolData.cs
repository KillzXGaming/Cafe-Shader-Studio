using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;

namespace BfresEditor
{
    public class VariationSymbolData
    {
        public void AddSymbol(string name, string symbol, string value)
        {
            symbols.Add(new VariationSymbol()
            {
                Name = name,
                SymbolName = symbol,
                Values = new List<string>() { value }
            });
        }

        public void AddSymbol(string name, string symbol, string[] values)
        {
            symbols.Add(new VariationSymbol()
            {
                Name = name,
                SymbolName = symbol,
                Values = new List<string>(values)
            });
        }

        public List<VariationSymbol> symbols = new List<VariationSymbol>();
        public void Read(FileReader reader)
        {
            var SectionPos = reader.Position;
            uint SectionSize = reader.ReadUInt32();
            uint SectionCount = reader.ReadUInt32();

            for (int i = 0; i < SectionCount; i++)
            {
                VariationSymbol symbol = new VariationSymbol();
                symbol.Read(reader);
                symbols.Add(symbol);
            }
            reader.Seek(SectionPos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer)
        {
            var pos = writer.Position;
            writer.Write(uint.MaxValue);
            writer.Write(symbols.Count);
            for (int i = 0; i < symbols.Count; i++)
                symbols[i].Write(writer);

            SharcCommon.WriteSectionSize(writer, pos);
        }
    }
    public class VariationSymbol
    {
        public string Name { get; set; }
        public List<string> Values { get; set; }
        public string SymbolName { get; set; }

        public void Read(FileReader reader)
        {
            var pos = reader.Position;
            uint SectionSize = reader.ReadUInt32();
            uint macroNameLength = reader.ReadUInt32();
            uint valueLength = reader.ReadUInt32();
            uint symbolNameLength = reader.ReadUInt32();
            Name = reader.ReadString((int)macroNameLength, true);
            Values = reader.ReadStrings((int)valueLength, Syroot.BinaryData.BinaryStringFormat.ZeroTerminated, Encoding.UTF8).ToList();
            SymbolName = reader.ReadString((int)symbolNameLength, true);
            reader.Seek(pos + SectionSize, System.IO.SeekOrigin.Begin);
        }

        public void Write(FileWriter writer)
        {   
            var pos = writer.Position;
            writer.Write(Name.Length + 1);
            writer.Write(Values.Count + 1);
            writer.Write(SymbolName.Length + 1);
            writer.WriteString(Name);
            for (int i = 0; i < Values.Count; i++)
                writer.WriteString(Values[i]);
            writer.WriteString(SymbolName);
            SharcCommon.WriteSectionSize(writer, pos);
        }
    }
}
