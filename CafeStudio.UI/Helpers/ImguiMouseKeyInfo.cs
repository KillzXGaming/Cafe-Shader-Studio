using ImGuiNET;
using System.Drawing;
using GLFrameworkEngine;
using OpenTK.Input;

namespace CafeStudio.UI
{
    public partial class ImGuiHelper
    {
        public static MouseEventInfo CreateMouseState()
        {
            var mouseInfo = new MouseEventInfo();

            //Prepare info
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                mouseInfo.RightButton = ButtonState.Pressed;
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                mouseInfo.LeftButton = ButtonState.Pressed;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                mouseInfo.RightButton = ButtonState.Released;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                mouseInfo.LeftButton = ButtonState.Released;

            MouseState mouseState = Mouse.GetState();
            mouseInfo.WheelPrecise = mouseState.WheelPrecise;

            //Construct relative position
            //-22 for titlebar size
            var windowPos = ImGui.GetWindowPos();

            var pos = ImGui.GetIO().MousePos;
            pos = new System.Numerics.Vector2(pos.X - windowPos.X, pos.Y - windowPos.Y - 22);

            if (ImGui.IsMousePosValid())
                mouseInfo.Position = new Point((int)pos.X, (int)pos.Y);
            else
                mouseInfo.HasValue = false;

            return mouseInfo;
        }
    }
}
