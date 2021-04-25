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
    public class UserDataDialog
    {
        public List<string> ValuePresets = new List<string>();

        public bool canParse = true;

        public bool LoadDialog(UserData userData, bool dialogOpen)
        {
            bool edited = false;
            if (ImGui.BeginPopupModal("##user_data_dialog", ref dialogOpen))
            {
                if (!canParse)
                    ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1), $"Failed to parse type {userData.Type}!");

                ImGuiHelper.InputFromText("Name", userData, "Name", 200);
                ImGuiHelper.ComboFromEnum<RenderInfoType>("Type", userData, "Type");

                var windowSize = ImGui.GetWindowSize();
                var buffer = Encoding.UTF8.GetBytes(GetDataString(userData));
                if (buffer.Length == 0)
                    buffer = new byte[1];

                if (ImGui.InputText("Values", buffer, (uint)buffer.Length + 1, ImGuiInputTextFlags.Multiline))
                {
                    canParse = true;

                    var text = Encoding.UTF8.GetString(buffer);
                    string[] values = text.Split('\n');

                    try
                    {
                        if (userData.Type == UserDataType.Int32)
                        {
                            int[] data = new int[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = int.Parse(values[i]);
                            userData.SetValue(data);
                        }
                        else if (userData.Type == UserDataType.Byte)
                        {
                            byte[] data = new byte[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = byte.Parse(values[i]);
                            userData.SetValue(data);
                        }
                        else if (userData.Type == UserDataType.Single)
                        {
                            float[] data = new float[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = float.Parse(values[i]);
                            userData.SetValue(data);
                        }
                        else
                        {
                            string[] data = new string[text.Length];
                            for (int i = 0; i < values.Length; i++)
                                data[i] = values[i];
                            userData.SetValue(data);
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
                    dialogOpen = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Ok"))
                {
                    if (canParse && !string.IsNullOrEmpty(userData.Name))
                    {
                        edited = true;
                        dialogOpen = false;
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }
            return edited;
        }

        static string GetDataString(UserData userData, string seperator = "\n")
        {
            if (userData.Type == UserDataType.Byte)
                return string.Join(seperator, userData.GetValueByteArray());
            else if (userData.Type == UserDataType.Int32)
                return string.Join(seperator, userData.GetValueInt32Array());
            else if (userData.Type == UserDataType.Single)
                return string.Join(seperator, userData.GetValueSingleArray());
            else
                return string.Join(seperator, userData.GetValueStringArray());

        }
    }
}
