using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ImGuiNET;
using System.Numerics;

namespace CafeStudio.UI
{
    public partial class ImGuiHelper
    {
        public static bool InputFromUint(string label, object obj, string properyName, int step = 1, bool drag = true)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (int)((uint)input.GetValue(obj));

            bool edited = drag ? ImGui.DragInt(label, ref inputValue, step) :
                                 ImGui.InputInt(label, ref inputValue, step);
            if (edited)
            {
                input.SetValue(obj, (uint)inputValue);
            }
            return edited;
        }

        public static void InputFromShort(string label, object obj, string properyName, float step = 1)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (int)((short)input.GetValue(obj));

            bool edited = ImGui.DragInt(label, ref inputValue, 1, 0, 255);
            if (edited)
            {
                input.SetValue(obj, (short)inputValue);
            }
        }


        public static bool InputFromByte(string label, object obj, string properyName, float step = 1)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (int)((byte)input.GetValue(obj));

            bool edited = ImGui.DragInt(label, ref inputValue, 1, 0, 255);
            if (edited)
            {
                input.SetValue(obj, (byte)inputValue);
                return true;
            }
            return false;
        }


        public static bool InputFromFloat(string label, object obj, string properyName, bool drag = false, float step = 1)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float)input.GetValue(obj);

            bool edited = false;
            if (drag)
                edited = ImGui.DragFloat(label, ref inputValue, 0.1f);
            else
                edited = ImGui.InputFloat(label, ref inputValue, step);

            if (edited)
            {
                input.SetValue(obj, inputValue);
            }
            return edited;
        }

        public static bool InputFromVector2(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = input.GetValue(obj);

            var vec = new Vector2(0);
            if (inputValue is Syroot.Maths.Vector2F)
                vec = new Vector2(
                    ((Syroot.Maths.Vector2F)inputValue).X,
                    ((Syroot.Maths.Vector2F)inputValue).Y);

            bool edited = false;
            if (drag)
                edited = ImGui.DragFloat2(label, ref vec, 0.1f);
            else
                edited = ImGui.InputFloat2(label, ref vec);

            if (edited)
            {
                input.SetValue(obj, new Syroot.Maths.Vector2F(vec.X, vec.Y));
            }
            return edited;
        }

        public static void InputFloatsFromVector2(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector2(inputValue[0], inputValue[1]);

            bool edited = false;
            if (drag)
                edited = ImGui.DragFloat2(label, ref vec, 0.1f);
            else
                edited = ImGui.InputFloat2(label, ref vec);

            if (edited)
            {
                input.SetValue(obj, new float[2] { vec.X, vec.Y });
            }
        }

        public static void InputFloatsFromVector3(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector3(inputValue[0], inputValue[1], inputValue[2]);

            bool edited = false;
            if (drag)
                edited = ImGui.DragFloat3(label, ref vec, 0.1f);
            else
                edited = ImGui.InputFloat3(label, ref vec);

            if (edited)
            {
                input.SetValue(obj, new float[3] { vec.X, vec.Y, vec.Z });
            }
        }

        public static void InputFloatsFromColor3Button(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector4(inputValue[0], inputValue[1], inputValue[2], 1.0f);

            float size = ImGui.GetFontSize();
            if (ImGui.ColorButton(label, vec, ImGuiColorEditFlags.None, new Vector2(size, size)))
            {
                input.SetValue(obj, new float[3] { vec.X, vec.Y, vec.Z });
            }
        }

        public static void InputFloatsFromColor4Button(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector4(inputValue[0], inputValue[1], inputValue[2], inputValue[3]);

            float size = ImGui.GetFontSize();
            if (ImGui.ColorButton(label, vec, ImGuiColorEditFlags.None, new Vector2(size, size)))
            {
                input.SetValue(obj, new float[4] { vec.X, vec.Y, vec.Z, vec.W });
            }
        }

        public static void InputFloatsFromVector4(string label, object obj, string properyName, bool drag = false)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector4(inputValue[0], inputValue[1], inputValue[2], inputValue[3]);

            bool edited = false;
            if (drag)
                edited = ImGui.DragFloat4(label, ref vec, 0.1f);
            else
                edited = ImGui.InputFloat4(label, ref vec);

            if (edited)
            {
                input.SetValue(obj, new float[4] { vec.X, vec.Y, vec.Z, vec.W });
            }
        }


        public static void InputFloatsFromColor3(string label, object obj, string properyName)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector3(inputValue[0], inputValue[1], inputValue[2]);
            if (ImGui.ColorEdit3(label, ref vec, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float))
            {
                input.SetValue(obj, new float[3] { vec.X, vec.Y, vec.Z });
            }
        }

        public static void InputFloatsFromColor4(string label, object obj, string properyName)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (float[])input.GetValue(obj);
            var vec = new Vector4(inputValue[0], inputValue[1], inputValue[2], inputValue[3]);
            if (ImGui.ColorEdit4(label, ref vec, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float))
            {
                input.SetValue(obj, new float[4] { vec.X, vec.Y, vec.Z, vec.W });
            }
        }

        public static bool InputFromText(string label, object obj, string properyName, int bufferLength,
            ImGuiInputTextFlags exflags = ImGuiInputTextFlags.None)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (string)input.GetValue(obj);

            var flags = exflags | ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll;
            if (ImGui.InputText(label, ref inputValue, (uint)bufferLength + 1, flags))
            {
                UndoStack.Add(new UndoStringOperation("Text Changed", obj, properyName, inputValue));
                input.SetValue(obj, inputValue);
                return true;
            }

            return false;
        }

        public static bool InputFromBoolean(string label, object obj, string properyName)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (bool)input.GetValue(obj);

            if (ImGui.Checkbox(label, ref inputValue))
            {
                input.SetValue(obj, inputValue);
                return true;
            }
            return false;
        }

        public static bool ComboFromEnum<T>(string label, object obj, string properyName, ImGuiComboFlags flags = ImGuiComboFlags.None)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = input.GetValue(obj);

            bool edited = false;
            if (ImGui.BeginCombo(label, inputValue.ToString(), flags))
            {
                var values = Enum.GetValues(typeof(T));
                foreach (var val in values)
                {
                    bool isSelected = inputValue == val;
                    if (ImGui.Selectable(val.ToString(), isSelected))
                    {
                        input.SetValue(obj, val);
                        edited = true;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            return edited;
        }

    }
}
