using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;

namespace CafeStudio.UI
{
    public class ArchiveFileWrapper : NodeBase
    {
        public IArchiveFile ArchiveFile;

        public ArchiveFileWrapper(NodeBase node)
        {
            ArchiveFile = (IArchiveFile)node.Tag;

            Header = node.Header;
            Tag = node.Tag;

           /* if (node.Children.Count > 0) {
                Children.Add(new NodeBase("dummy"));
            }*/

            Children.Clear();
            foreach (var c in node.Children)
                Children.Add(new ArchiveHiearchy(ArchiveFile, c));
        }
    }
}
