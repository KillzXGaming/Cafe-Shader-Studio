using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using BfresLibrary;
using BfresEditor;
using CafeStudio.UI;

namespace BfresEditor
{
    public class RenderInfoDialog
    {
        public List<string> ValuePresets = new List<string>();

        public bool canParse = true;

        public object originalData;
        public string originalName;

        public void OnLoad(FMAT material, RenderInfo renderInfo) {
            originalData = renderInfo.Data;
            originalName = renderInfo.Name;
            ValuePresets = RenderInfoEnums.FindRenderInfoPresets(material, renderInfo.Name).ToList();
        }

        public void Revert(RenderInfo renderInfo)
        {
            renderInfo.Data = originalData;
            renderInfo.Name = originalName;
        }

        public void LoadDialog(RenderInfo renderInfo, bool dialogOpen, EventHandler onDialogClosed)
        {
            if (ImGui.BeginPopupModal("##render_info_dialog", ref dialogOpen))
            {
                if (!canParse)
                    ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1), $"Failed to parse type {renderInfo.Type}!");

                ImGuiHelper.InputFromText("Name", renderInfo, "Name", 200);

                if (ValuePresets.Count > 0 && renderInfo.Type == RenderInfoType.String)
                {
                    string value = renderInfo.GetValueStrings()[0];
                    if (ImGui.BeginCombo("Presets", ""))
                    {
                        foreach (var val in ValuePresets)
                        {
                            bool isSelected = val == value;
                            if (ImGui.Selectable(val, isSelected)) {
                                renderInfo.SetValue(new string[1] { val });
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }

                ImGuiHelper.ComboFromEnum<RenderInfoType>("Type", renderInfo, "Type");

                var windowSize = ImGui.GetWindowSize();
                var buffer = Encoding.UTF8.GetBytes(GetDataString(renderInfo));

                if (ImGui.InputText("Values", buffer, (uint)0x2048, ImGuiInputTextFlags.Multiline))
                {
                    canParse = true;

                    var text = Encoding.UTF8.GetString(buffer);
                    string[] values = text.Split('\n');

                    try
                    {
                        if (renderInfo.Type == RenderInfoType.Int32)
                        {
                            int[] data = new int[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = int.Parse(values[i]);
                            renderInfo.SetValue(data);
                        }
                        else if (renderInfo.Type == RenderInfoType.Single)
                        {
                            float[] data = new float[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = float.Parse(values[i]);
                            renderInfo.SetValue(data);
                        }
                        else
                        {
                            string[] data = new string[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = values[i];
                            renderInfo.SetValue(data);
                        }
                    }
                    catch
                    {
                        canParse = false;
                    }
                }

                ImGui.SetCursorPos(new System.Numerics.Vector2(windowSize.X - 110, windowSize.Y - 28));
                if (ImGui.Button("Cancel"))
                {
                    Revert(renderInfo);
                    dialogOpen = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Ok"))
                {
                    if (canParse)
                    {
                        onDialogClosed?.Invoke(this, EventArgs.Empty);
                        dialogOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }
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
