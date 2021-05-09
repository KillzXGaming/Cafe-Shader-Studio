using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfshaLibrary;
using ImGuiNET;
using CafeStudio.UI;

namespace BfresEditor
{
    public class GX2ShaderGUI
    {
        static string VertexShaderSource;
        static string FragShaderSource;

        static string selectedStage = "Vertex";

        ShaderInfo ShaderInfo;

        public void OnLoad(GX2VertexShader vertexShader, GX2PixelShader pixelShader) {
            ShaderInfo = CafeShaderDecoder.LoadShaderProgram(vertexShader.DataBytes, pixelShader.DataBytes);

            VertexShaderSource = System.IO.File.ReadAllText(ShaderInfo.VertPath);
            FragShaderSource = System.IO.File.ReadAllText(ShaderInfo.FragPath);

            if (VertexShaderSource == null) VertexShaderSource = "";
            if (FragShaderSource == null) FragShaderSource = "";
        }

        public void Render(GX2VertexShader vertexShader, GX2PixelShader pixelShader)
        {
            if (string.IsNullOrEmpty(VertexShaderSource))
                OnLoad(vertexShader, pixelShader);

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
                LoadShaderStageCode();
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("menu_shader1", "Shader Info"))
            {
                if (ImGui.BeginChild("ShaderInfoC"))
                {
                    LoadShaderInfo(vertexShader, pixelShader);
                }
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
        }

        static void LoadShaderInfo(GX2VertexShader vertexShader, GX2PixelShader pixelShader)
        {
            if (selectedStage == "Vertex")
            {
                if (ImGui.CollapsingHeader("Attributes", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < vertexShader.Attributes.Count; i++)
                        ImGui.Text($"In {vertexShader.Attributes[i].Name} Location {vertexShader.Attributes[i].Location} Location {vertexShader.Attributes[i].Type}");
                }

                if (ImGui.CollapsingHeader("Uniform Blocks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < vertexShader.UniformBlocks.Count; i++)
                    {
                        if (ImGui.CollapsingHeader($"Uniforms##{i}"))
                        {
                            ImGui.Text($"{vertexShader.UniformBlocks[i].Name} Location {vertexShader.UniformBlocks[i].Offset}");

                            var uniforms = vertexShader.Uniforms.OrderBy(x => x.Offset).ToList();
                            for (int j = 0; j < uniforms.Count; j++)
                                if (uniforms[j].BlockIndex == i)
                                    ImGui.Text($"{uniforms[j].Name} Type {uniforms[j].Type} offset {uniforms[j].Offset}");
                        }
                    }
                }
            }
            if (selectedStage == "Pixel")
            {
                if (ImGui.CollapsingHeader("Uniform Blocks", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < pixelShader.UniformBlocks.Count; i++)
                    {
                        ImGui.Text($"{pixelShader.UniformBlocks[i].Name} Location {pixelShader.UniformBlocks[i].Offset}");

                        if (ImGui.CollapsingHeader($"Uniforms##{i}"))
                        {
                            var uniforms = pixelShader.Uniforms.OrderBy(x => x.Offset).ToList();
                            for (int j = 0; j < uniforms.Count; j++)
                                if (uniforms[j].BlockIndex == i)
                                    ImGui.Text($"{uniforms[j].Name} Type {uniforms[j].Type} offset {uniforms[j].Offset}");
                        }
                    }
                }
                if (ImGui.CollapsingHeader("Samplers", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    for (int i = 0; i < pixelShader.Samplers.Count; i++)
                    {
                        ImGui.Text($"{pixelShader.Samplers[i].Name} Location {pixelShader.Samplers[i].Location} {pixelShader.Samplers[i].Type}");
                    }
                }
            }
        }

        static void LoadShaderStageCode()
        {
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
