using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Toolbox.Core;
using System.Diagnostics;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class CafeShaderDecoder
    {
        public static Dictionary<string, ShaderInfo> GLShaderPrograms = new Dictionary<string, ShaderInfo>();

        public static ShaderInfo LoadShaderProgram(string programName,
            GX2VertexShader vertexInfo, GX2PixelShader pixelInfo,
           byte[] vertexShader, byte[] fragmentShader)
        {
            if (!Directory.Exists("GFD/Cache"))
                Directory.CreateDirectory("GFD/Cache");

            var vertShaderName = programName + GetHashSHA1(vertexShader);
            var fragShaderName = programName + GetHashSHA1(fragmentShader);

            string key = $"{vertShaderName}{fragShaderName}";

            if (GLShaderPrograms.ContainsKey(key))
                return GLShaderPrograms[key];

            List<ShaderStage> stages = new List<ShaderStage>();
            stages.Add(new ShaderStage() { Name = vertShaderName, Data = vertexShader, Command = "-v" });
            stages.Add(new ShaderStage() { Name = fragShaderName, Data = fragmentShader, Command = "-p" });

            foreach (var block in vertexInfo.UniformBlocks)
                stages[0].BlockLocations.Add((int)block.Offset);
            foreach (var block in pixelInfo.UniformBlocks)
                stages[1].BlockLocations.Add((int)block.Offset);

            var info = DecodeSharcBinary($"GFD", stages);

            //Load the source to opengl
            info.Program = new ShaderProgram(
                            new FragmentShader(File.ReadAllText(info.FragPath)),
                            new VertexShader(File.ReadAllText(info.VertPath)));

            GLShaderPrograms.Add(key, info);
            return GLShaderPrograms[key];
        }

        static ShaderInfo DecodeSharcBinary(string directory, List<ShaderStage> stages)
        {
            ConvertStages(stages);

            for (int i = 0; i < stages.Count; i++)
            {
                string outputFilePath = $"{directory}/Cache/{stages[i].Name}{stages[i].Extension}";

                if (!File.Exists(outputFilePath))
                {
                    ConvertGLSL($"{directory}/{stages[i].Name}", outputFilePath, stages[i].Extension);
                        
                    string updatedShaderData = RenameBuffers(File.ReadAllText(outputFilePath), stages[i].Command, stages[i].BlockLocations);
                    File.WriteAllText(outputFilePath, updatedShaderData);
                }
            }

            //Cleanup
            foreach (var stage in stages)
            {
                if (File.Exists($"GFD/{stage.Name}"))
                    File.Delete($"GFD/{stage.Name}");
                if (File.Exists($"GFD/{stage.Name}{stage.Extension}.spv"))
                    File.Delete($"GFD/{stage.Name}{stage.Extension}.spv");
            }

            var vertextage = stages.FirstOrDefault(x => x.Command == "-v");
            var pixelStage = stages.FirstOrDefault(x => x.Command == "-p");

            ShaderInfo info = new ShaderInfo();
            info.VertPath = $"{directory}/Cache/{vertextage.Name}{vertextage.Extension}";
            info.FragPath = $"{directory}/Cache/{pixelStage.Name}{pixelStage.Extension}";
            return info;
        }


        static string RenameBuffers(string source, string command, List<int> blockLocations)
        {
            Dictionary<string, string> uniformConversions = new Dictionary<string, string>();
            if (command == "-v")
            {
                foreach (var index in blockLocations)
                {
                    uniformConversions.Add(
                        $"layout(std430) readonly buffer CBUFFER_DATA_{index}",
                        $"layout (std140) uniform vp_{index}");
                }
            }
            if (command == "-p")
            {
                foreach (var index in blockLocations)
                {
                    uniformConversions.Add(
                        $"layout(std430) readonly buffer CBUFFER_DATA_{index}",
                        $"layout (std140) uniform fp_{index}");
                }
            }

            StringBuilder builder = new StringBuilder();
            string line = null;
            using (StringReader reader = new StringReader(source))
            {
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        foreach (var uniform in uniformConversions)
                        {
                            if (line.Contains(uniform.Key))
                                line = line.Replace(uniform.Key, uniform.Value);
                        }
                        if (line.Contains("vec4 values[];"))
                            line = line.Replace("vec4 values[];", "vec4 values[0x1000];");

                        builder.AppendLine(line);
                    }

                } while (line != null);
            }

            return builder.ToString();
        }

        static string ConvertStages(List<ShaderStage> stages)
        {
            foreach (var stage in stages)
                File.WriteAllBytes($"GFD/{stage.Name}", stage.Data);

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "GFD/gx2shader-decompiler.exe";
            start.WorkingDirectory = Path.Combine(Runtime.ExecutableDir, "GFD");
            foreach (var stage in stages)
                start.Arguments += $"{stage.Command} {AddQuotesIfRequired($"{stage.Name}")} ";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    try
                    {
                        return reader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        return "";
                    }
                }
            }
        }

        class ShaderStage
        {
            public string Name { get; set; }
            public string Path { get; set; }

            public byte[] Data { get; set; }
            public string Command = "";

            public List<int> BlockLocations = new List<int>();

            public string Extension
            {
                get
                {
                    if (Command == "-v") return ".vert";
                    if (Command == "-p") return ".frag";
                    return ".geom";
                }
            }
        }

        //Hash algorithm for cached shaders. Make sure to only decompile unique/new shaders
        static string GetHashSHA1(byte[] data)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                return string.Concat(sha1.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }

        static void ConvertGLSL(string path, string output, string extension) {
            SPIRV2GLSL($"{path}{extension}.spv", $"{output}");
        }

        static string SPIRV2GLSL(string filePath, string output)
        {
            string remapFlags = "";

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "GFD/spirv-cross.exe";
            start.WorkingDirectory = Runtime.ExecutableDir;
            start.Arguments = $"{AddQuotesIfRequired(filePath)} {remapFlags} --no-es --extension GL_ARB_shader_storage_buffer_object --no-420pack-extension --no-support-nonzero-baseinstance --version 330 --output {AddQuotesIfRequired(output)}";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    try
                    {
                        return reader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        return "";
                    }
                }
            }
        }

        static string AddQuotesIfRequired(string path)
        {
            return !string.IsNullOrWhiteSpace(path) ?
                path.Contains(" ") && (!path.StartsWith("\"") && !path.EndsWith("\"")) ?
                    "\"" + path + "\"" : path :
                    string.Empty;
        }
    }
}
