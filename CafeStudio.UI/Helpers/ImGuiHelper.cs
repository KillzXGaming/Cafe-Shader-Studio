using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using ImGuiNET;
using System.Numerics;
using GLFrameworkEngine;
using System.Reflection;

namespace CafeStudio.UI
{
    public partial class ImGuiHelper
    {
        public class PropertyInfo
        {
            public bool CanDrag = false;

            public float Speed = 1.0f;
        }

        public static void IncrementCursorPosX(float amount) {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + amount);
        }

        public static void IncrementCursorPosY(float amount) {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + amount);
        }

        public static void DisplayFramebufferImage(int id, int width, int height) {
            DisplayFramebufferImage(id, new Vector2(width, height));
        }

        public static void DisplayFramebufferImage(int id, Vector2 size) {
            ImGui.Image((IntPtr)id, size, new Vector2(0, 1), new Vector2(1, 0));
        }
    }
}
