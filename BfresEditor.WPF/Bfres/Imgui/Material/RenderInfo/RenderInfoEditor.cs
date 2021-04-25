using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;
using BfresLibrary;

namespace BfresEditor
{
    public class RenderInfoEditor
    {
        static List<int> SelectedIndices = new List<int>();
        static bool dialogOpen = false;

        static RenderInfoDialog ActiveDialog = new RenderInfoDialog();

        public static void Render(FMAT material)
        {
            RenderHeader();

            if (ImGui.BeginChild("RENDER_INFO_LIST"))
            {
                int index = 0;
                foreach (var renderInfo in material.Material.RenderInfos.Values)
                {
                    ImGui.Columns(2);
                    if (ImGui.Selectable(renderInfo.Name, SelectedIndices.Contains(index)))
                    {
                        SelectedIndices.Clear();
                        SelectedIndices.Add(index);
                    }
                    ImGui.NextColumn();
                    ImGui.Text(GetDataString(renderInfo, ","));
                    ImGui.NextColumn();

                    if (dialogOpen && SelectedIndices.Contains(index))
                        ActiveDialog.LoadDialog(renderInfo, dialogOpen, (o, e) =>
                        {
                            material.UpdateRenderState();
                            foreach (FSHP mesh in material.GetMappedMeshes())
                                mesh.ReloadShader();
                        });

                    if (SelectedIndices.Contains(index) && ImGui.IsMouseDoubleClicked(0)) {
                        dialogOpen = true;
                        ActiveDialog.OnLoad(material, renderInfo);
                        ImGui.OpenPopup("##render_info_dialog");
                    }

                    index++;

                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();
        }

        static void ReloadMaterial(FMAT mat)
        {
            mat.UpdateRenderState();
            foreach (FSHP mesh in mat.GetMappedMeshes()) {
                mesh.ReloadShader();
            }
        }

        static void RenderHeader()
        {
            ImGui.Columns(2);
            if (ImGui.Selectable("Name"))
            {

            }
            ImGui.NextColumn();
            if (ImGui.Selectable("Value"))
            {

            }
            ImGui.Separator();
            ImGui.Columns(1);
        }

        static string GetDataString(RenderInfo renderInfo, string seperator = "\n")
        {
            if (renderInfo.Type == RenderInfoType.Int32)
                return string.Join(seperator, renderInfo.GetValueInt32s());
            else if (renderInfo.Type == RenderInfoType.Single)
                return string.Join(seperator, renderInfo.GetValueSingles());
            else
                return string.Join(seperator, renderInfo.GetValueStrings());
        }
    }
}
