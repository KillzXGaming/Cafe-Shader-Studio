using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CafeStudio.UI;
using GLFrameworkEngine;
using ImGuiNET;

namespace CafeShaderStudio
{
    public class AnimationWindow
    {
        private string selectedFile;

        private void Update(IRenderableFile file) {
            selectedFile = $"{file.Renderer.Name}_{file.Renderer.ID}";
        }

        public void Render(List<IRenderableFile> files)
        {
            if (string.IsNullOrEmpty(selectedFile) && files.Count > 0)
            {
                var file = files.FirstOrDefault();
                Update(file);
            }
            if (ImGui.BeginCombo("Files", selectedFile))
            {
                foreach (var file in files) {
                    string name = $"{file.Renderer.Name}_{file.Renderer.ID}";
                    bool isSelected = selectedFile == name;
                    if (ImGui.Selectable(name, isSelected)) {
                        selectedFile = name;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }
    }
}
