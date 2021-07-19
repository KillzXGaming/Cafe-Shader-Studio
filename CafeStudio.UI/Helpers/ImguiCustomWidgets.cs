using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Numerics;

namespace CafeStudio.UI
{
    public class ImguiCustomWidgets
    {
        static Dictionary<string, string> selectedTabMenus = new Dictionary<string, string>();

        public static TransformOutput Transform(OpenTK.Vector3 position, OpenTK.Vector3 rotation, OpenTK.Vector3 scale)
        {
            //To system numerics to use in imgui
            return Transform(new Vector3(position.X, position.Y, position.Z),
                             new Vector3(rotation.X, rotation.Y, rotation.Z),
                             new Vector3(scale.X, scale.Y, scale.Z));
        }

        public static TransformOutput Transform(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            bool edited0 = ImGui.InputFloat3("Translate", ref position);
            bool edited1 = ImGui.InputFloat3("Rotation", ref rotation);
            bool edited2 = ImGui.InputFloat3("Scale", ref scale);
            return new TransformOutput()
            {
                Position = position,
                Rotation = rotation,
                Scale = scale,
                Edited = edited0 | edited1 | edited2,
            };
        }

        public class TransformOutput
        {
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale;

            public bool Edited;
        }

        public static bool BeginTab(string menuKey, string text)
        {
            //Keep track of multiple loaded menus
            if (!selectedTabMenus.ContainsKey(menuKey))
                selectedTabMenus.Add(menuKey, "");

            var disabled = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            var normal = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            if (selectedTabMenus[menuKey] == text)
                ImGui.PushStyleColor(ImGuiCol.Text, normal);
            else
                ImGui.PushStyleColor(ImGuiCol.Text, disabled);

            bool active = ImGui.BeginTabItem(text);
            if (active) { selectedTabMenus[menuKey] = text; }

            ImGui.PopStyleColor();
            return active;
        }

        public static void DragHorizontalSeperator(string name, float height, float width, float delta, float padding)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
            bool done = false;

            ImGui.InvisibleButton(name + "...hseperator", new Vector2(-1, padding));
            if (ImGui.IsItemActive()) {
                delta = ImGui.GetMouseDragDelta().Y;
                done = true;
            }
            ImGui.Text(name);
            ImGui.SameLine();
            ImGui.InvisibleButton(name + "...hseperator2", new Vector2(1, 13));
            if (ImGui.IsItemActive()) {
                delta = ImGui.GetMouseDragDelta().Y;
                done = true;
            }
            if (!done) {
                height = height + delta;
                delta = 0;
            }
            ImGui.PopStyleVar();
        }

        public static bool ImageButtonToggle(int imageTrue, int imageFalse, ref bool isValue, Vector2 size)
        {
            var ptr = (IntPtr)(isValue ? imageTrue : imageFalse);
            if (ImGui.ImageButton(ptr, size)) {
                if (isValue)
                    isValue = false;
                else
                    isValue = true;

                return true;
            }
            return false;
        }

        public unsafe bool CustomTreeNode(string label)
        {
            var style = ImGui.GetStyle();
            var storage = ImGui.GetStateStorage();

            uint id = ImGui.GetID(label);
            int opened = storage.GetInt(id, 0);
            float x = ImGui.GetCursorPosX();
            ImGui.BeginGroup();
            if (ImGui.InvisibleButton(label, new Vector2(-1, ImGui.GetFontSize() + style.FramePadding.Y * 2)))
            {
                opened = storage.GetInt(id, 0);
              //  opened = p_opened == p_opened;
            }
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            if (hovered || active)
            {
                var col = ImGui.GetStyle().Colors[(int)(active ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered)];
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.ColorConvertFloat4ToU32(col));
            }
            ImGui.SameLine();
            ImGui.ColorButton("color_btn", opened == 1 ? new Vector4(1,1,1,1) : new Vector4(1, 0, 0, 1));
            ImGui.SameLine();
            ImGui.Text(label);
            ImGui.EndGroup();
            if (opened == 1)
                ImGui.TreePush(label);
            return opened != 0;
        }

        public static bool PathSelector(string label, ref string path, bool isValid = true)
        {
            if (!System.IO.Directory.Exists(path))
                isValid = false;

            bool clicked = ImGui.Button($"  -  ##{label}");

            ImGui.SameLine();
            if (!isValid)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }

            if (clicked)
            {
                var dialog = new ImguiFolderDialog();
                if (dialog.ShowDialog())
                {
                    path = dialog.SelectedPath;
                    return true;
                }
            }
            return false;
        }
    }
}
