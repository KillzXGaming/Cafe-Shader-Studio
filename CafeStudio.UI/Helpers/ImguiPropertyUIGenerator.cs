using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using ImGuiNET;
using System.Numerics;
using System.Reflection;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public partial class ImGuiHelper
    {
        public static void LoadProperties(object obj, EventHandler propertyChanged = null)
        {
            Dictionary<string, bool> categories = new Dictionary<string, bool>();

            var style = ImGui.GetStyle();
            var frameSize = style.FramePadding;
            var itemSpacing = style.ItemSpacing;

            style.ItemSpacing = (new Vector2(itemSpacing.X, 2));
            style.FramePadding = (new Vector2(frameSize.X, 2));

            var properties = obj.GetType().GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                var categoryAttribute = properties[i].GetCustomAttribute<CategoryAttribute>();
                var descAttribute = properties[i].GetCustomAttribute<DescriptionAttribute>();
                var displayAttribute = properties[i].GetCustomAttribute<DisplayNameAttribute>();
                var browsableAttribute = properties[i].GetCustomAttribute<BrowsableAttribute>();
                var readonlyAttribute = properties[i].GetCustomAttribute<ReadOnlyAttribute>();

                if (browsableAttribute != null && !browsableAttribute.Browsable)
                    continue;

                string label = displayAttribute != null ? displayAttribute.DisplayName : properties[i].Name;
                string desc = descAttribute != null ? descAttribute.Description : properties[i].Name;
                bool readOnly = !properties[i].CanWrite;

                if (readonlyAttribute != null && readonlyAttribute.IsReadOnly)
                    readOnly = true;

                string category = "Properties";

                if (categoryAttribute != null)
                    category = categoryAttribute.Category;

                if (!categories.ContainsKey(category))
                {
                    bool open = ImGui.CollapsingHeader(category, ImGuiTreeNodeFlags.DefaultOpen);
                    categories.Add(category, open);
                }
                //Check for category expansion on collapsed header
                if (categories[category])
                {
                    bool valueChanged = SetPropertyUI(properties[i], obj, label, desc, readOnly);
                    if (valueChanged)
                        propertyChanged?.Invoke(properties[i].Name, EventArgs.Empty);
                }
            }

            style.FramePadding = frameSize;
            style.ItemSpacing = itemSpacing;

            ImGui.Columns(1);
        }

        static bool SetPropertyUI(System.Reflection.PropertyInfo property,
            object obj, string label, string desc, bool readOnly)
        {
            bool valueChanged = false;

            ImGui.Columns(2);
            ImGui.Text(label);
            ImGui.NextColumn();

            var flags = ImGuiInputTextFlags.None;
            if (readOnly)
                flags |= ImGuiInputTextFlags.ReadOnly;

            label = $"##{property.Name}_{property.PropertyType}";

            if (readOnly)
            {
                var disabled = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, disabled);
            }

            if (property.PropertyType.IsEnum)
            {
                var inputValue = property.GetValue(obj);
                var type = property.PropertyType;
                if (ImGui.BeginCombo(label, inputValue.ToString(), ImGuiComboFlags.NoArrowButton))
                {
                    if (!readOnly)
                    {
                        var values = Enum.GetValues(type);
                        foreach (var val in values)
                        {
                            bool isSelected = inputValue == val;
                            if (ImGui.Selectable(val.ToString(), isSelected))
                            {
                                property.SetValue(obj, val);
                                valueChanged = true;
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                    }

                    ImGui.EndCombo();
                }
            }
            if (property.PropertyType == typeof(string))
            {
                var inputValue = (string)property.GetValue(obj);
                if (string.IsNullOrEmpty(inputValue))
                    inputValue = " ";

                var data = Encoding.UTF8.GetBytes(inputValue);

                if (ImGui.InputText(label, data, (uint)data.Length + 1, flags))
                {
                    property.SetValue(obj, Encoding.UTF8.GetString(data));
                    valueChanged = true;
                }
            }
            if (property.PropertyType == typeof(float))
            {
                var inputValue = (float)property.GetValue(obj);
                if (ImGui.InputFloat(label, ref inputValue, 0, 0, "", flags))
                {
                    property.SetValue(obj, (float)inputValue);
                    valueChanged = true;
                }
            }
            if (property.PropertyType == typeof(uint))
            {
                var inputValue = (int)(uint)property.GetValue(obj);
                if (ImGui.InputInt(label, ref inputValue, 0, 0, flags))
                {
                    property.SetValue(obj, (uint)inputValue);
                    valueChanged = true;
                }
            }
            if (property.PropertyType == typeof(int))
            {
                var inputValue = (int)property.GetValue(obj);
                if (ImGui.InputInt(label, ref inputValue, 0, 0, flags))
                {
                    property.SetValue(obj, (int)inputValue);
                    valueChanged = true;
                }
            }
            if (property.PropertyType == typeof(bool))
            {
                var inputValue = (bool)property.GetValue(obj);
                if (ImGui.Checkbox(label, ref inputValue))
                {
                    property.SetValue(obj, (bool)inputValue);
                    valueChanged = true;
                }
            }

            if (readOnly)
            {
                ImGui.PopStyleColor();
            }

            ImGui.NextColumn();

            ImGui.Columns(1);

            return valueChanged;
        }

    }
}
