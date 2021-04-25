using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;

namespace BfresEditor
{
    public class MaterialOptions
    {
        static List<int> SelectedIndices = new List<int>();

        static Dictionary<string, string> LoadedOptions = new Dictionary<string, string>();

        public static void Render(FMAT material)
        {
            LoadedOptions.Clear();
            foreach (var op in material.ShaderOptions)
                LoadedOptions.Add(op.Key, op.Value);

            bool isValid = true;

            var meshes = material.GetMappedMeshes();
            foreach (FSHP mesh in meshes)
            {
                if (!mesh.HasValidShader)
                    isValid = false;
            }

            if (!isValid)
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0, 0.0f, 1), "Invalid Option Combination!");
            else
                ImGui.Text("");

            //ImGui.TextColored(new System.Numerics.Vector4(0.2f, 1, 0.3f, 1), "Valid Option Combination!");

            RenderHeader(material);

            if (ImGui.BeginChild("OPTION_LIST"))
            {
                ImGui.Columns(2);

                int index = 0;
                foreach (var option in LoadedOptions)
                {
                    if (SelectedIndices.Contains(index))
                    {
                        if (ImGui.CollapsingHeader(option.Key, ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            string value = option.Value;
                            if (ImGui.InputText("", ref value, 100))
                            {
                                material.ShaderOptions[option.Key] = value;
                                foreach (FSHP mesh in meshes)
                                    mesh.ReloadShader();
                            }
                        }
                    }
                    else if (ImGui.Selectable(option.Key, SelectedIndices.Contains(index)))
                    {
                        SelectedIndices.Clear();
                        SelectedIndices.Add(index);
                    }
                    ImGui.NextColumn();
                    ImGui.Text(option.Value);
                    ImGui.NextColumn();
                    index++;
                }
            }
            ImGui.EndChild();

            ImGui.Columns(1);
        }

        static void RenderHeader(FMAT material)
        {
            ImGui.Columns(2);
            if (ImGui.Selectable("Name"))
            {
                LoadedOptions.Clear();
                foreach (var op in material.ShaderOptions.OrderBy(x => x.Key))
                    LoadedOptions.Add(op.Key, op.Value);
            }
            ImGui.NextColumn();
            if (ImGui.Selectable("Value"))
            {
                LoadedOptions.Clear();
                foreach (var op in material.ShaderOptions.OrderBy(x => x.Value))
                    LoadedOptions.Add(op.Key, op.Value);
            }
            ImGui.Separator();
            ImGui.Columns(1);
        }
    }
}
