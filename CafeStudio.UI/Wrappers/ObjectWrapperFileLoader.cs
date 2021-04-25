using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.IO;
using Toolbox.Core;
using Toolbox.Core.ViewModels;

namespace CafeStudio.UI
{
    public class ObjectWrapperFileLoader
    {
        public static NodeBase OpenFormat(IFileFormat fileFormat) {
            if (fileFormat is IArchiveFile)
                return LoadArchiveFormat((IArchiveFile)fileFormat);
            if (fileFormat is NodeBase)
                return fileFormat as NodeBase;
            else
                return null;
        }

        static NodeBase LoadArchiveFormat(IArchiveFile archiveFile)
        {
            IFileFormat fileFormat = (IFileFormat)archiveFile;

            NodeBase root = new NodeBase(fileFormat.FileInfo.FileName);
            root.Tag = fileFormat;

            var hiearchyNode = CreateObjectHiearchy(root, archiveFile);
           // return hiearchyNode;

            if (hiearchyNode.Children.Count == 1 && hiearchyNode.Children[0].Children.Count > 0)
            {
                hiearchyNode = hiearchyNode.Children[0];
                hiearchyNode.Tag = fileFormat;
            }

            return new ArchiveFileWrapper(hiearchyNode);
        }

        static NodeBase CreateObjectHiearchy(NodeBase parent, IArchiveFile archiveFile)
        {
            // build a TreeNode collection from the file list
            foreach (var file in archiveFile.Files)
            {
                string[] paths = file.FileName.Split('/');
                ProcessTree(parent, file, paths, 0);
            }
            return parent;
        }

        static void ProcessTree(NodeBase parent, ArchiveFileInfo file, string[] paths, int index)
        {
            string currentPath = paths[index];
            if (paths.Length - 1 == index)
            {
                var fileNode = new NodeBase(currentPath);
                string ext = Toolbox.Core.Utils.GetExtension(currentPath);
                fileNode.Tag = file;

                parent.AddChild(fileNode);
                return;
            }

            var node = FindFolderNode(parent, currentPath);
            if (node == null)
            {
                node = new NodeBase(currentPath);
                parent.AddChild(node);
            }

            ProcessTree(node, file, paths, index + 1);
        }

        private static NodeBase FindFolderNode(NodeBase parent, string path)
        {
            NodeBase node = null;
            foreach (var child in parent.Children.ToArray()) {
                if (child.Header.Equals(path)) {
                    node = (NodeBase)child;
                    break;
                }
            }
            return node;
        }
    }
}
