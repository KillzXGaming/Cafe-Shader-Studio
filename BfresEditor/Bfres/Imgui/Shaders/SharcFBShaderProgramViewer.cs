using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using OpenTK.Graphics.OpenGL;
using ImGuiNET;
using System.IO;
using Toolbox.Core.IO;
using CafeStudio.UI;

namespace BfresEditor
{
    public class SharcFBShaderProgramViewer
    {
        static string VertexShaderSource;
        static string FragShaderSource;

        static string vertexShaderPath;
        static string fragmentShaderPath;

        static string selectedStage = "Vertex";
        static MemoryEditor MemoryEditor = new MemoryEditor();

        public static void Render(FMAT material)
        {
            var renderer = material.MaterialAsset as SharcFBRenderer;
            if (renderer.GLShaderInfo == null)
                return;

            if (ImGui.BeginCombo("Stage", selectedStage))
            {
                if (ImGui.Selectable("Vertex"))
                {
                    selectedStage = "Vertex";
                }
                if (ImGui.Selectable("Pixel"))
                {
                    selectedStage = "Pixel";
                }
                ImGui.EndCombo();
            }

            ImGui.BeginTabBar("menu_shader1");
            if (ImguiCustomWidgets.BeginTab("menu_shader1", $"Shader Code"))
            {
                LoadShaderStageCode(material);
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("menu_shader1", "Shader Info"))
            {
                if (ImGui.BeginChild("ShaderInfoC"))
                {
                    LoadShaderInfo(material);
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("menu_shader1", "GX2 Shader Data"))
            {
                var shader = material.MaterialAsset as SharcFBRenderer;
                var program = shader.ShaderModel;

                if (selectedStage == "Vertex")
                {
                    var gx2Shader = program.GetRawVertexShader(shader.BinaryIndex).ToArray();
                    MemoryEditor.Draw(gx2Shader, gx2Shader.Length);
                }
                if (selectedStage == "Pixel")
                {
                    var gx2Shader = program.GetRawPixelShader(shader.BinaryIndex).ToArray();
                    MemoryEditor.Draw(gx2Shader, gx2Shader.Length);
                }
                ImGui.EndTabItem();
            }
        }

        static void LoadShaderInfo(FMAT material)
        {
            var shader = material.MaterialAsset as SharcFBRenderer;
            var program = shader.ShaderModel;

            if (selectedStage == "Vertex") {
                var gx2Shader = (GX2VertexShader)program.GetGX2VertexShader(shader.BinaryIndex);

                if (ImGui.CollapsingHeader("Attributes", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < gx2Shader.Attributes.Count; i++)
                        ImGui.Text($"In {gx2Shader.Attributes[i].Name} Location {gx2Shader.Attributes[i].Location} Location {gx2Shader.Attributes[i].Type}");
                }

                if (ImGui.CollapsingHeader("Uniform Blocks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < gx2Shader.UniformBlocks.Count; i++)
                    {
                        if (ImGui.CollapsingHeader($"Uniforms##{i}"))
                        {
                            ImGui.Text($"{gx2Shader.UniformBlocks[i].Name} Location {gx2Shader.UniformBlocks[i].Offset}");

                            var uniforms = gx2Shader.Uniforms.OrderBy(x => x.Offset).ToList();
                            for (int j = 0; j < uniforms.Count; j++)
                                if (uniforms[j].BlockIndex == i)
                                    ImGui.Text($"{uniforms[j].Name} Type {uniforms[j].Type} offset {uniforms[j].Offset}");
                        }
                    }
                }

                if (ImGui.CollapsingHeader("SHARCFB Attributes", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < program.AttributeVariables.symbols.Count; i++)
                        ImGui.Text($"Name {program.AttributeVariables.symbols[i].Name} Symbol { program.AttributeVariables.symbols[i].SymbolName}");
                }

                if (ImGui.CollapsingHeader("SHARCFB Uniform Blocks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < program.UniformBlocks.symbols.Count; i++)
                        ImGui.Text($"Name {program.UniformBlocks.symbols[i].Name} Symbol { program.UniformBlocks.symbols[i].SymbolName}");
                }
            }
            if (selectedStage == "Pixel") {
                var gx2Shader = (GX2PixelShader)program.GetGX2PixelShader(shader.BinaryIndex);

                if (ImGui.CollapsingHeader("Uniform Blocks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < gx2Shader.UniformBlocks.Count; i++)
                    {
                        ImGui.Text($"{gx2Shader.UniformBlocks[i].Name} Location {gx2Shader.UniformBlocks[i].Offset}");

                        if (ImGui.CollapsingHeader($"Uniforms##{i}"))
                        {
                            var uniforms = gx2Shader.Uniforms.OrderBy(x => x.Offset).ToList();
                            for (int j = 0; j < uniforms.Count; j++)
                                if (uniforms[j].BlockIndex == i)
                                    ImGui.Text($"{uniforms[j].Name} Type {uniforms[j].Type} offset {uniforms[j].Offset}");
                        }
                    }
                }

                if (ImGui.CollapsingHeader("Samplers", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < gx2Shader.Samplers.Count; i++)
                        ImGui.Text($"{gx2Shader.Samplers[i].Name} Location {gx2Shader.Samplers[i].Location} Type {gx2Shader.Samplers[i].Type}");
                }

                if (ImGui.CollapsingHeader("SHARCFB Samplers", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < program.SamplerVariables.symbols.Count; i++)
                        ImGui.Text($"{program.SamplerVariables.symbols[i].Name} Symbol {program.SamplerVariables.symbols[i].SymbolName}");
                }
            }
        }

        static void LoadShaderStageCode(FMAT material)
        {
            var renderer = material.MaterialAsset as SharcFBRenderer;

            if (vertexShaderPath != renderer.GLShaderInfo.VertPath)
            {
                vertexShaderPath = renderer.GLShaderInfo.VertPath;
                VertexShaderSource = System.IO.File.ReadAllText(vertexShaderPath);
            }
            if (fragmentShaderPath != renderer.GLShaderInfo.FragPath)
            {
                fragmentShaderPath = renderer.GLShaderInfo.FragPath;
                FragShaderSource = System.IO.File.ReadAllText(fragmentShaderPath);
            }

            if (ImGui.BeginChild("stage_window"))
            {
                var size = ImGui.GetWindowSize();
                if (selectedStage == "Vertex")
                {
                    ImGui.InputTextMultiline("Vertex", ref VertexShaderSource, 4000, size);
                }
                if (selectedStage == "Pixel")
                {
                    ImGui.InputTextMultiline("Pixel", ref FragShaderSource, 4000, size);
                }
            }
            ImGui.EndChild();
        }
    }
}
