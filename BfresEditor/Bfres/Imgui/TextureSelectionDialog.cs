using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using BfresEditor;
using Toolbox.Core;
using CafeStudio.UI;

namespace BfresEditor
{
    public class TextureSelectionDialog
    {
        public static string OutputName = "";

        static bool popupOpened = false;

        public static bool Render(string input, ref bool dialogOpened)
        {
            if (string.IsNullOrEmpty(OutputName))
                OutputName = input;

            bool hasInput = false;
            if (ImGui.BeginCombo("Selected", OutputName))
            {
                popupOpened = true;

                byte[] data = Encoding.UTF8.GetBytes(OutputName);
                if (ImGui.InputText("Name", data, 200)) {
                    OutputName = Encoding.UTF8.GetString(data);
                    hasInput = true;
                }
                foreach (var model in GLFrameworkEngine.DataCache.ModelCache.Values)
                {
                    if (model is BfresRender)
                    {
                        var bfres = model as BfresRender;
                        foreach (var tex in bfres.Textures.Values)
                        {
                            bool isSelected = OutputName == tex.Name;

                            IconManager.LoadTexture(tex.Name, tex);
                            ImGui.SameLine();

                            if (ImGui.Selectable(tex.Name, isSelected)) {
                                OutputName = tex.Name;
                                hasInput = true;
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                    }
                }
                ImGui.EndCombo();
            }
            else if (popupOpened)
            {
                dialogOpened = false;
                popupOpened = false;
            }
            return hasInput;
        }
    }
}
