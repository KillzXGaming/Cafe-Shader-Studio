using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;

namespace CafeStudio.UI
{
    public class ArchiveHiearchy : NodeBase
    {
        public string ArchiveEditor = "Hex Preview";

        public ArchiveFileInfo ArchiveFileInfo;
        public IArchiveFile ArchiveFile;

        public bool IsFile => ArchiveFileInfo != null;

        public ArchiveHiearchy(IArchiveFile archiveFile, NodeBase node)
        {
            if (node.Tag is ArchiveFileInfo) {
                ArchiveFileInfo = (ArchiveFileInfo)node.Tag;
            }

            Header = node.Header;
            ArchiveFile = archiveFile;
            Tag = null;

            foreach (var c in node.Children)
                Children.Add(new ArchiveHiearchy(ArchiveFile, c));
        }

        public void OpenFileFormat() {
            if (!IsFile)
                return;

            var fileFormat = ArchiveFileInfo.OpenFile();
            if (fileFormat == null)
                return;

            this.Tag = fileFormat;
            ArchiveEditor = "File Editor";

            WorkspaceWindow.ActiveWorkspace.ActiveFileFormat = fileFormat;
            WorkspaceWindow.ActiveWorkspace.AddDrawable(fileFormat);

           var wrapper = ObjectWrapperFileLoader.OpenFormat(fileFormat);
            if (wrapper == null)
                return;

            this.Children.Clear();
            foreach (var node in wrapper.Children)
                this.Children.Add(node); 
        }
    }
}
