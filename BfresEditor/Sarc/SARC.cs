﻿using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.IO;

namespace CafeLibrary
{
    public class SARC : IFileFormat, IArchiveFile
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "SARC" };
        public string[] Extension { get; set; } = new string[] { "*.sarc" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = false;
        public bool CanRenameFiles { get; set; } = false;
        public bool CanReplaceFiles { get; set; } = false;
        public bool CanDeleteFiles { get; set; } = false;

        public List<ArchiveFileInfo> files = new List<ArchiveFileInfo>();
        public IEnumerable<ArchiveFileInfo> Files => files;

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "SARC");
            }
        }

        private SarcData SarcData;

        public void Load(System.IO.Stream stream) {
            files.Clear();

            FileInfo.KeepOpen = true;
            SarcData = SARC_Parser.UnpackRamN(stream);
            foreach (var file in SarcData.Files)
            {
                var fileEntry = new FileEntry();
                fileEntry.FileName = file.Key;
                if (SarcData.HashOnly)
                {
                    fileEntry.FileName = SARC_Parser.TryGetNameFromHashTable(file.Key);
                    fileEntry.HashName = file.Key;
                }
                fileEntry.SetData(file.Value);
                files.Add(fileEntry);

                if (fileEntry.FileName.EndsWith(".bfres"))
                    fileEntry.OpenFileFormatOnLoad = true;
            }
        }

        public void ClearFiles() { files.Clear(); }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Add(new FileEntry()
            {
                FileData = archiveFileInfo.FileData,
                FileName = archiveFileInfo.FileName,
            });
            return true;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            files.Remove((FileEntry)archiveFileInfo);
            return true;
        }

        public void Save(System.IO.Stream stream)
        {
            //Reload our sarc data from the files and save active files
            SarcData.Files.Clear();
            foreach (FileEntry file in files)
            {
                file.SaveFileFormat();

                if (SarcData.HashOnly)
                    SarcData.Files.Add(file.HashName, file.FileData);
                else
                    SarcData.Files.Add(file.FileName, file.FileData);
            }

            //Save data to stream
            var saved = SARC_Parser.PackN(SarcData);
            using (var writer = new FileWriter(stream)) {
                writer.Write(saved.Item2);
            }

            //Save alignment to compression type yaz0
            if (FileInfo.Compression != null && FileInfo.Compression is Yaz0) {
                ((Yaz0)FileInfo.Compression).Alignment = saved.Item1;
            }
        }

        public class FileEntry : ArchiveFileInfo
        {
            /// <summary>
            /// The hash calculated in hex format turned into a string used for hash only files.
            /// </summary>
            public string HashName
            {
                get
                {
                    if (hashName == null)
                        hashName = SARC_Parser.NameHash(FileName).ToString("X8");

                    return hashName;
                }
                set { hashName = value; }
            }
            private string hashName;
        }
    }
}
