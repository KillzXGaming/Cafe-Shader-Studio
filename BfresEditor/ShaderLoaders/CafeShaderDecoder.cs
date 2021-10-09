﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.Diagnostics;
using GLFrameworkEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BfresEditor
{
    public class CafeShaderDecoder
    {
        public static Dictionary<string, ShaderInfo> GLShaderPrograms = new Dictionary<string, ShaderInfo>();

        public static ShaderInfo LoadShaderProgram(byte[] vertexShader, byte[] fragmentShader)
        {
            if (!Directory.Exists("GFD/Cache"))
                Directory.CreateDirectory("GFD/Cache");

            var shaderName = GetHashSHA1(ByteUtils.CombineArray(vertexShader, fragmentShader));

            string key = $"{shaderName}";

            if (GLShaderPrograms.ContainsKey(key))
                return GLShaderPrograms[key];

            List<ShaderStage> stages = new List<ShaderStage>();
            stages.Add(new ShaderStage() { Name = shaderName + "VS", Data = vertexShader, Command = "-v" });
            stages.Add(new ShaderStage() { Name = shaderName + "FS", Data = fragmentShader, Command = "-p" });
            var info = DecodeSharcBinary($"GFD", stages);

            //Load the source to opengl
            info.Program = new ShaderProgram(
                            new FragmentShader(File.ReadAllText(info.FragPath)),
                            new VertexShader(File.ReadAllText(info.VertPath)));

            GLShaderPrograms.Add(key, info);
            return GLShaderPrograms[key];
        }

        public static void SetShaderConstants(ShaderProgram shader, int programID, FMAT material)
        {
            //Setup constants
            shader.SetVector4("VS_PUSH.posMulAdd", new Vector4(1, -1, 0, 0));
            shader.SetVector4("VS_PUSH.zSpaceMul", new Vector4(0, 1, 1, 1));
            shader.SetFloat("VS_PUSH.pointSize", 1.0f);

            uint alphaFunction = 7;
            switch (material.BlendState.AlphaFunction)
            {
                case AlphaFunction.Never: alphaFunction = 0; break;
                case AlphaFunction.Less: alphaFunction = 1; break;
                case AlphaFunction.Lequal: alphaFunction = 3; break;
                case AlphaFunction.Greater: alphaFunction = 4; break;
                case AlphaFunction.Gequal: alphaFunction = 6; break;
            }

            if (material.BlendState.AlphaTest)
            {
                GL.Uniform1(GL.GetUniformLocation(programID, "PS_PUSH.alphaFunc"), alphaFunction);
                shader.SetFloat("PS_PUSH.alphaRef", material.BlendState.AlphaValue);
                GL.Uniform1(GL.GetUniformLocation(programID, "PS_PUSH.needsPremultiply"), (uint)0);
            }
            else
            {
                GL.Uniform1(GL.GetUniformLocation(programID, "PS_PUSH.alphaFunc"), (uint)7);
                shader.SetFloat("PS_PUSH.alphaRef", 1.0f);
                GL.Uniform1(GL.GetUniformLocation(programID, "PS_PUSH.needsPremultiply"), (uint)0);
            }
        }

        static ShaderInfo DecodeSharcBinary(string directory, List<ShaderStage> stages)
        {
            var vertextage = stages.FirstOrDefault(x => x.Command == "-v");
            var pixelStage = stages.FirstOrDefault(x => x.Command == "-p");

            ShaderInfo info = new ShaderInfo();
            info.VertPath = $"{directory}/{vertextage.Name}{vertextage.Extension}";
            info.FragPath = $"{directory}/{pixelStage.Name}{pixelStage.Extension}";

            if (File.Exists(info.VertPath) && File.Exists(info.FragPath))
                return info;

            string ex = ConvertStages(stages);
            for (int i = 0; i < stages.Count; i++)
            {
                string outputFilePath = $"{directory}/{stages[i].Name}{stages[i].Extension}";

                if (!File.Exists(outputFilePath))
                {
                    ConvertGLSL($"{directory}/{stages[i].Name}", outputFilePath, stages[i].Extension);

                    string updatedShaderData = RenameBuffers(File.ReadAllText(outputFilePath), stages[i].Command);
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

            return info;
        }


        static string RenameBuffers(string source, string command)
        {
            Dictionary<string, string> uniformConversions = new Dictionary<string, string>();
            if (command == "-v")
            {
                for (int i = 0; i < 20; i++)
                {
                    uniformConversions.Add(
                        $"readonly buffer CBUFFER_DATA_{i}",
                        $"layout (std140) uniform vp_{i}");
                }
            }
            if (command == "-p")
            {
                for (int i = 0; i < 20; i++)
                {
                    uniformConversions.Add(
                        $"readonly buffer CBUFFER_DATA_{i}",
                        $"layout (std140) uniform fp_{i}");
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
                                line = uniform.Value;
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
            {
                if (!File.Exists($"GFD/{stage.Name}"))
                    throw new Exception($"Failed to write stage {stage.Name}!");

                start.Arguments += $"{stage.Command} {AddQuotesIfRequired($"{stage.Name}")} ";
            }
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
            start.Arguments = $"{AddQuotesIfRequired(filePath)} {remapFlags} --no-es --extension GL_ARB_shader_storage_buffer_object --extension GL_ARB_separate_shader_objects --no-420pack-extension --no-support-nonzero-baseinstance --version 440 --output {AddQuotesIfRequired(output)}";
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
