using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BfresLibrary;
using Toolbox.Core;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresExternalFile : IRenamableNode, /*IExportReplaceNode,*/ IPropertyUI
    {
        private ExternalFile ExternalFile;

        public string Name { get; set; }

        public BfresExternalFile(ExternalFile file, string name) {
            ExternalFile = file;
            Name = name;
        }

        public Type GetTypeUI() => typeof(MemoryEditor);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (MemoryEditor)uiInstance;
            editor.Draw(ExternalFile.Data, ExternalFile.Data.Length);
        }

        #region events

        public void Renamed(string text) {
            this.Name = text;
        }

        public string GetRenameText() => this.Name;

        public FileFilter[] ReplaceFilter => new FileFilter[]
        {
          new FileFilter(".bin", "Raw Binary"),
        };

        public FileFilter[] ExportFilter => new FileFilter[]
        {
          new FileFilter(".bin", "Raw Binary"),
        };

        public void Replace(string fileName) {
            ExternalFile.Data = File.ReadAllBytes(fileName);
        }

        public void Export(string fileName) {
            File.WriteAllBytes(fileName, ExternalFile.Data);
        }

        #endregion
    }
}
