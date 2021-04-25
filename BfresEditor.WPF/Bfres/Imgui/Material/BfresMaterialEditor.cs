using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresMaterialEditor
    {
        static FMAT activeMaterial;

        static bool onLoad = false;

        static string selectedTab;

        static UVViewport UVViewport = null;

        public void Init()
        {
            UVViewport = new UVViewport();
            UVViewport.Camera.Zoom = 30;
            UVViewport.OnLoad();
        }

        public void LoadEditor(FMAT material) {
            LoadEditorMenus(material);
        }

        public void LoadEditorMenus(FMAT material)
        {
            if (UVViewport == null)
                Init();

            if (activeMaterial != material)
            {
                onLoad = true;
                UVViewport.Reset();
            }

            activeMaterial = material;

            if (ImGui.CollapsingHeader("Material Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.InputFromText("Name", material, "Name", 200);
                ImGuiHelper.InputFromText("ShaderArchive", material, "ShaderArchive", 200);
                ImGuiHelper.InputFromText("ShaderModel", material, "ShaderModel", 200);
                ImGuiHelper.InputFromBoolean("Visible", material.Material, "Visible");
            }

            if (ImGui.BeginChild("##MATERIAL_EDITOR"))
            {
                ImGui.BeginTabBar("Menu1");
 
                if (ImguiCustomWidgets.BeginTab("Menu1", "Texture Maps"))
                {
                    BfresTextureMapEditor.Render(material, UVViewport, onLoad);
                    ImGui.EndTabItem();
                }

                if (ImguiCustomWidgets.BeginTab("Menu1", "Parameters"))
                {
                    MaterialParameter.Render(material);
                    ImGui.EndTabItem();
                }
                if (ImguiCustomWidgets.BeginTab("Menu1", "Render Info"))
                {
                    RenderInfoEditor.Render(material);
                    ImGui.EndTabItem();
                }
                if (ImguiCustomWidgets.BeginTab("Menu1", "Options"))
                {
                    MaterialOptions.Render(material);
                    ImGui.EndTabItem();
                }

                if (!material.ParentFile.ResFile.IsPlatformSwitch)
                {
                    if (ImguiCustomWidgets.BeginTab("Menu1", "Render State")) {
                        RenderStateEditor.Render(material);
                        ImGui.EndTabItem();
                    }
                }

                if (ImguiCustomWidgets.BeginTab("Menu1", "User Data"))
                {
                    UserDataInfoEditor.Render(material.Material.UserData);
                    ImGui.EndTabItem();
                }

                if (material.MaterialAsset is BfshaRenderer)
                {
                    if (ImguiCustomWidgets.BeginTab("Menu1", "Shader Data")) {
                        ShaderProgramViewer.Render(material);
                        ImGui.EndTabItem();
                    }
                }
                
                ImGui.EndTabBar();
            }
            ImGui.EndChild();

            onLoad = false;
        }
    }
}
