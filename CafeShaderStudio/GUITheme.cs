using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using ImGuiNET;

namespace CafeShaderStudio
{
    public class ColorTheme
    {
        public virtual Vector4 Text { get; set; }
        public virtual Vector4 WindowBg { get; set; }
        public virtual Vector4 ChildBg { get; set; }
        public virtual Vector4 Border { get; set; }
        public virtual Vector4 PopupBg { get; set; }
        public virtual Vector4 FrameBg { get; set; }
        public virtual Vector4 FrameBgHovered { get; set; }
        public virtual Vector4 FrameBgActive { get; set; }
        public virtual Vector4 TitleBg { get; set; }
        public virtual Vector4 TitleBgActive { get; set; }
        public virtual Vector4 CheckMark { get; set; }
        public virtual Vector4 ButtonActive { get; set; }
        public virtual Vector4 Button{ get; set; }
        public virtual Vector4 Header { get; set; }
        public virtual Vector4 HeaderHovered { get; set; }
        public virtual Vector4 HeaderActive { get; set; }
        public virtual Vector4 SeparatorHovered { get; set; }
        public virtual Vector4 SeparatorActive { get; set; }
        public virtual Vector4 Separator { get; set; }
        public virtual Vector4 Tab { get; set; }
        public virtual Vector4 TabHovered { get; set; }
        public virtual Vector4 TabActive { get; set; }
        public virtual Vector4 TabUnfocused { get; set; }
        public virtual Vector4 TabUnfocusedActive { get; set; }
        public virtual Vector4 DockingPreview { get; set; }
        public virtual Vector4 TextSelectedBg { get; set; }
        public virtual Vector4 NavHighlight { get; set; }

        public static void UpdateTheme(ColorTheme theme)
        {
            ImGui.GetStyle().WindowPadding = new Vector2(2);

            ImGui.GetStyle().Colors[(int)ImGuiCol.Text] = theme.Text;
            ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] = theme.WindowBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg] = theme.ChildBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.Border] = theme.Border;
            ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg] = theme.PopupBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] = theme.FrameBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered] = theme.FrameBgHovered;
            ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive] = theme.FrameBgActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBg] = theme.TitleBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] = theme.TitleBgActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark] = theme.CheckMark;
            ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive] = theme.ButtonActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.Header] = theme.Header;
            ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered] = theme.HeaderHovered;
            ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderActive] = theme.HeaderActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.SeparatorHovered] = theme.SeparatorHovered;
            ImGui.GetStyle().Colors[(int)ImGuiCol.SeparatorActive] = theme.SeparatorActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.Separator] = theme.Separator;
            ImGui.GetStyle().Colors[(int)ImGuiCol.Tab] = theme.Tab;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered] = theme.TabHovered;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive] = theme.TabActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TabUnfocused] = theme.TabUnfocused;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TabUnfocusedActive] = theme.TabUnfocusedActive;
            ImGui.GetStyle().Colors[(int)ImGuiCol.DockingPreview] = theme.DockingPreview;
            ImGui.GetStyle().Colors[(int)ImGuiCol.TextSelectedBg] = theme.TextSelectedBg;
            ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight] = theme.NavHighlight;
            ImGui.GetStyle().Colors[(int)ImGuiCol.Button] = theme.Button;
            ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg] = theme.WindowBg;
        }
    }


    public class DarkTheme : ColorTheme
    {
        public DarkTheme()
        {
            Text = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            WindowBg = new Vector4(0.17f, 0.17f, 0.17f, 0.94f);
            ChildBg = new Vector4(0.30f, 0.30f, 0.30f, 0.00f);
            Border = new Vector4(0.00f, 0.00f, 0.00f, 0.50f);
            PopupBg = new Vector4(0.2f, 0.2f, 0.2f, 0.94f);
            FrameBg = new Vector4(0.09f, 0.09f, 0.09f, 0.40f);
            FrameBgActive = new Vector4(0.42f, 0.42f, 0.42f, 0.67f);
            TitleBg = new Vector4(0.147f, 0.147f, 0.147f, 1.000f);
            TitleBgActive = new Vector4(0.13f, 0.13f, 0.13f, 1.00f);
            CheckMark = new Vector4(0.37f, 0.53f, 0.71f, 1.00f);
            ButtonActive = new Vector4(0.53f, 0.54f, 0.54f, 1.00f);
            Button = new Vector4(0.24f, 0.24f, 0.24f, 1.00f);
            Header = new Vector4(0.37f, 0.37f, 0.37f, 0.31f);
            HeaderHovered = new Vector4(0.46f, 0.46f, 0.46f, 0.80f);
            HeaderActive = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
            SeparatorHovered = new Vector4(0.82f, 0.82f, 0.82f, 0.78f);
            SeparatorActive = new Vector4(0.53f, 0.53f, 0.53f, 1.00f);
            Separator = new Vector4(0.21f, 0.21f, 0.21f, 1.00f);
            Tab = new Vector4(0.16f, 0.16f, 0.16f, 0.86f);
            TabHovered = new Vector4(0.22f, 0.22f, 0.22f, 0.80f);
            TabActive = new Vector4(0.27f, 0.27f, 0.27f, 1.00f);
            TabUnfocused = new Vector4(0.12f, 0.12f, 0.12f, 0.98f);
            TabUnfocusedActive = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
            DockingPreview = new Vector4(0.34f, 0.34f, 0.34f, 0.70f);
            TextSelectedBg = new Vector4(0.24f, 0.45f, 0.68f, 0.35f);
            NavHighlight = new Vector4(0.4f, 0.4f, 0.4f, 1.00f);
        }
    }

    public class UE4Theme : ColorTheme
    {
        public UE4Theme()
        {
            Text = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            WindowBg = new Vector4(0.17f, 0.17f, 0.17f, 0.94f);
            ChildBg = new Vector4(0.30f, 0.30f, 0.30f, 0.00f);
            Border = new Vector4(0.00f, 0.00f, 0.00f, 0.50f);
            PopupBg = new Vector4(0.2f, 0.2f, 0.2f, 0.94f);
            FrameBg = new Vector4(0.09f, 0.09f, 0.09f, 0.40f);
            FrameBgActive = new Vector4(0.42f, 0.42f, 0.42f, 0.67f);
            TitleBg = new Vector4(0.147f, 0.147f, 0.147f, 1.000f);
            TitleBgActive = new Vector4(0.13f, 0.13f, 0.13f, 1.00f);
            CheckMark = new Vector4(0.37f, 0.53f, 0.71f, 1.00f);
            ButtonActive = new Vector4(0.53f, 0.54f, 0.54f, 1.00f);
            Button = new Vector4(0.24f, 0.24f, 0.24f, 1.00f);
            Header = new Vector4(0.37f, 0.37f, 0.37f, 0.31f);
            HeaderHovered = new Vector4(0.46f, 0.46f, 0.46f, 0.80f);
            HeaderActive = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
            SeparatorHovered = new Vector4(0.82f, 0.82f, 0.82f, 0.78f);
            SeparatorActive = new Vector4(0.53f, 0.53f, 0.53f, 1.00f);
            Separator = new Vector4(0.21f, 0.21f, 0.21f, 1.00f);
            Tab = new Vector4(0.16f, 0.16f, 0.16f, 0.86f);
            TabHovered = new Vector4(0.22f, 0.22f, 0.22f, 0.80f);
            TabActive = new Vector4(0.27f, 0.27f, 0.27f, 1.00f);
            TabUnfocused = new Vector4(0.12f, 0.12f, 0.12f, 0.98f);
            TabUnfocusedActive = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
            DockingPreview = new Vector4(0.34f, 0.34f, 0.34f, 0.70f);
            TextSelectedBg = new Vector4(0.24f, 0.45f, 0.68f, 0.35f);
            NavHighlight = new Vector4(0.26f, 0.66f, 1.00f, 1.00f);
        }
    }
}
