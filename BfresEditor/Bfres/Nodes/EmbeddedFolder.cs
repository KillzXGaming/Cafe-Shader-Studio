using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;

namespace BfresEditor
{
    public class EmbeddedFolder : SubSectionBase
    {
        public override string Header => "Embedded Files";

        public EmbeddedFolder(BFRES bfres, ResFile resFile, ResDict<ExternalFile> resDict)
        {
            foreach (var file in resDict)
            {
                var node = new BfresNodeBase(file.Key);
                node.Tag = new BfresExternalFile(file.Value, file.Key);
                AddChild(node);
            }
        }
    }
}
