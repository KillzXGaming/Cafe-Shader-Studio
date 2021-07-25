using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using CafeStudio.UI;

namespace BfresEditor
{
    public class LightingEditor
    {
        public ColorCorrectionWindow ColorCorrectionWindow;
        public CubemapUintWindow CubemapUintWindow;
        public LightMapEditor LightMapEditor;

        private bool opened = true;

        public LightingEditor()
        {
            ColorCorrectionWindow = new ColorCorrectionWindow();
            CubemapUintWindow = new CubemapUintWindow();
            LightMapEditor = new LightMapEditor();
        }

        public void Render(GLFrameworkEngine.GLContext context)
        {
            if (ImGui.Begin("Lighting Editor", ref opened))
            {
                ImGui.BeginTabBar("Menu1");

                if (ImguiCustomWidgets.BeginTab("Menu1", "Color Correction"))
                {
                    ImGui.EndTabItem();
                }

                if (ImguiCustomWidgets.BeginTab("Menu1", "Environment"))
                {
                    ImGui.EndTabItem();
                }

                if (ImguiCustomWidgets.BeginTab("Menu1", "Light Maps"))
                {
                    LightMapEditor.Render(context);
                    ImGui.EndTabItem();
                }

                if (ImguiCustomWidgets.BeginTab("Menu1", "Cube Maps"))
                {
                    CubemapUintWindow.Render(context);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
/*  
                ImGui.Columns(2);
                if (ImGui.Selectable("Color Correction", ColorCorrectionWindow.IsActive)) {
                    ColorCorrectionWindow.IsActive = true;
                }
                ImGui.NextColumn();

                if (ColorCorrectionWindow.IsActive)
                    ColorCorrectionWindow.Render(context);

                ImGui.NextColumn();*/
            }
            ImGui.End();
        }
    }
}
