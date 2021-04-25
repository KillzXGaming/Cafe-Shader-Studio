using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace CafeStudio.UI
{
    public class LightingEditor
    {
        public ColorCorrectionWindow ColorCorrectionWindow;
        public CubemapUintWindow CubemapUintWindow;

        private bool opened = true;

        public LightingEditor()
        {
            ColorCorrectionWindow = new ColorCorrectionWindow();
            CubemapUintWindow = new CubemapUintWindow();
        }

        public void Render(GLFrameworkEngine.GLContext context)
        {
            if (ImGui.Begin("Lighting Editor", ref opened))
            {
                ImGui.Columns(2);
                if (ImGui.Selectable("Color Correction", ColorCorrectionWindow.IsActive)) {
                    ColorCorrectionWindow.IsActive = true;
                }
                ImGui.NextColumn();

                if (ColorCorrectionWindow.IsActive)
                    ColorCorrectionWindow.Render(context);

                ImGui.NextColumn();
            }
            ImGui.End();
        }
    }
}
