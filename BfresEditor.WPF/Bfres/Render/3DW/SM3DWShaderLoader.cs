using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using BfresEditor;
using Toolbox.Core.IO;
using Toolbox.Core;

namespace BfresEditor
{
    public class SM3DWShaderLoader
    {
        public static string GamePath = @"G:\NX\3DW\10028600EBDA000\Romfs";

        public static BfshaLibrary.BfshaFile LoadShader(string archive)
        {
            foreach (var file in GlobalShaderCache.ShaderFiles.Values) {
                if (file is BfshaLibrary.BfshaFile) {
                    if (((BfshaLibrary.BfshaFile)file).Name == archive)
                    {
                        return (BfshaLibrary.BfshaFile)file;
                    }
                }
            }

           return TryLoadPath($"{GamePath}\\ShaderData", archive);
        }

        private static BfshaLibrary.BfshaFile TryLoadPath(string folder, string fileName)
        {
            string outputPath = $"GlobalShaders\\{fileName}.bfsha";
            if (GlobalShaderCache.ShaderFiles.ContainsKey(outputPath))
                return (BfshaLibrary.BfshaFile)GlobalShaderCache.ShaderFiles[outputPath];

            //Load cached file to disk if exist
            if (System.IO.File.Exists(outputPath)) {
                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }

            //Load from game folder instead if not cached 
            if (System.IO.File.Exists($"{folder}\\{fileName}.szs")) {
                if (!Directory.Exists("GlobalShaders"))
                    Directory.CreateDirectory("GlobalShaders");

                //Cache the file and save to disk
                var sarc = STFileLoader.OpenFileFormat($"{folder}\\{fileName}.szs") as IArchiveFile;
                var file = sarc.Files.FirstOrDefault(x => x.FileName == $"{fileName}.bfsha");
                file.FileData.SaveToFile(outputPath);

                var bfsha = new BfshaLibrary.BfshaFile(outputPath);
                GlobalShaderCache.ShaderFiles.Add(outputPath, bfsha);
                return bfsha;
            }
            return null;
        }
    }
}
