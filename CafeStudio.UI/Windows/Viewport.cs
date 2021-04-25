using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ImGuiNET;
using GLFrameworkEngine;
using OpenTK.Input;
using Toolbox.Core;
using System.Runtime.InteropServices;

namespace CafeStudio.UI
{
    public class Viewport
    {
        public Pipeline Pipeline { get; set; }

        private bool _mouseDown = false;
        private bool ForceUpdate = false;
        private string shadingMode = "Default";
        private List<string> shadingModes = new List<string>() { "Default" };

        private string selectedModel = "All Models";

        private TimelineWindow parentWindow;

        public Viewport(TimelineWindow window) {
            Pipeline = new Pipeline();
            parentWindow = window;
        }

        public void OnLoad() {
            Pipeline.InitBuffers();
        }

        public void AddFile(IRenderableFile bfres) {
            Pipeline.AddFile(bfres);
        }

        public void AddFile(GenericRenderer render) {
            Pipeline.AddFile(render);
        }

        public void Update() {
            ForceUpdate = true;
        }

        private IPickable DragDroppedModel;

        public void Render()
        {
            //Menu
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("View Setting"))
                {
                    if (ImGui.BeginMenu("Background"))
                    {
                        ImGui.Checkbox("Display", ref DrawableBackground.Display);
                        ImGui.ColorEdit3("Color Top", ref DrawableBackground.BackgroundTop);
                        ImGui.ColorEdit3("Color Bottom", ref DrawableBackground.BackgroundBottom);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Grid"))
                    {
                        ImGui.Checkbox("Display", ref DrawableFloor.Display);
                        ImGui.ColorEdit4("Grid Color", ref DrawableFloor.GridColor);
                        ImGui.InputInt("Grid Cell Count", ref Toolbox.Core.Runtime.GridSettings.CellAmount);
                        ImGui.InputFloat("Grid Cell Size", ref Toolbox.Core.Runtime.GridSettings.CellSize);
                        ImGui.EndMenu();
                    }

                    // ImGui.Checkbox("VSync", ref Toolbox.Core.Runtime.EnableVSync);
                    ImGui.Checkbox("Wireframe", ref Toolbox.Core.Runtime.RenderSettings.Wireframe);
                    ImGui.Checkbox("WireframeOverlay", ref Toolbox.Core.Runtime.RenderSettings.WireframeOverlay);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu($"Shading [{Runtime.DebugRendering}]"))
                {
                    foreach (var mode in Enum.GetValues(typeof(Runtime.DebugRender))) {
                        bool isSelected = (Runtime.DebugRender)mode == Runtime.DebugRendering;
                        if (ImGui.Selectable(mode.ToString(), isSelected)) {
                            Runtime.DebugRendering = (Runtime.DebugRender)mode;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Camera")) {

                    if (ImGui.Button("Reset Transform")) {
                        Pipeline._context.Camera.Translation = new OpenTK.Vector3(0, 1, -5);
                        Pipeline._context.Camera.RotationX = 0;
                        Pipeline._context.Camera.RotationY = 0;
                        Pipeline._context.Camera.UpdateTransform();
                    }
                    ImGuiHelper.InputFromBoolean("Orthographic", Pipeline._context.Camera, "IsOrthographic");

                    ImGuiHelper.InputFromFloat("Fov (Degrees)", Pipeline._context.Camera, "FovDegrees", true, 1f);
                    if (Pipeline._context.Camera.FovDegrees != 45) {
                        ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.FovDegrees = 45; }
                    }

                    ImGuiHelper.InputFromFloat("ZFar", Pipeline._context.Camera, "ZFar", true, 1f);
                    if (Pipeline._context.Camera.ZFar != 100000.0f) {
                        ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZFar = 100000.0f; }
                    }

                    ImGuiHelper.InputFromFloat("ZNear", Pipeline._context.Camera, "ZNear", true, 0.1f);
                    if (Pipeline._context.Camera.ZNear != 0.1f) {
                        ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZNear = 0.1f; }
                    }

                    ImGuiHelper.InputFromFloat("Zoom Speed", Pipeline._context.Camera, "ZoomSpeed", true, 0.1f);
                    if (Pipeline._context.Camera.ZoomSpeed != 1.0f) {
                        ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZoomSpeed = 1.0f; }
                    }

                    ImGuiHelper.InputFromFloat("Pan Speed", Pipeline._context.Camera, "PanSpeed", true, 0.1f);
                    if (Pipeline._context.Camera.PanSpeed != 1.0f) {
                        ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.PanSpeed = 1.0f; }
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Reset Animations"))
                {
                    parentWindow.Reset();
                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();

                var menuBG = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
                ImGui.PushStyleColor(ImGuiCol.WindowBg, menuBG);
                ImGui.PushStyleColor(ImGuiCol.ChildBg, menuBG);
                if (ImGui.BeginChild("viewport_menu2", new System.Numerics.Vector2(350, 22)))
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Active Model(s)");
                    ImGui.SameLine();
                    if (ImGui.BeginCombo("##model_select", selectedModel))
                    {
                        bool isSelected = "All Models" == selectedModel;
                        if (ImGui.Selectable("All Models", isSelected))
                        {
                            selectedModel = "All Models";
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();

                        foreach (var file in Pipeline.Files)
                        {
                            string name = file.Renderer.Name;
                            isSelected = name == selectedModel;

                            if (ImGui.Selectable(name, isSelected))
                            {
                                selectedModel = name;
                            }
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
            }

            //Make sure the entire viewport is within a child window to have accurate mouse and window sizing relative to the space it uses.
            if (ImGui.BeginChild("viewport_child1"))
            {
                var size = ImGui.GetWindowSize();
                if (Pipeline.Width != (int)size.X || Pipeline.Height != (int)size.Y)
                {
                    Pipeline.Width = (int)size.X;
                    Pipeline.Height = (int)size.Y;
                    Pipeline.OnResize();
                }

                Pipeline.RenderScene();

                if (ImGui.IsWindowFocused() && ImGui.IsWindowHovered() || ForceUpdate || _mouseDown)
                {
                    ForceUpdate = false;

                    if (!onEnter) {
                        Pipeline.ResetPrevious();
                        onEnter = true;
                    }

                    //Only update scene when necessary
                    UpdateCamera();
                }
                else
                {
                    onEnter = false;

                    //Reset drag/dropped model data if mouse leaves the viewport during a drag event
                    if (DragDroppedModel != null) {
                        DragDroppedModel.DragDroppedOnLeave();
                        DragDroppedModel = null;
                    }
                }

                var id = Pipeline.GetViewportTexture();
                ImGui.Image((IntPtr)id, size, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));

                if (ImGui.BeginDragDropTarget())
                {
                    ImGuiPayloadPtr outlinerDrop = ImGui.AcceptDragDropPayload("OUTLINER_ITEM",
                        ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery);

                    if (outlinerDrop.IsValid())
                    {
                        //Drag/drop things onto meshes
                        var mouseInfo = CreateMouseState();
                        var picked = Pipeline.GetPickedObject(mouseInfo);
                        //Picking object changed.
                        if (DragDroppedModel != picked) {
                            //Set exit drop event for previous model
                            if (DragDroppedModel != null)
                                DragDroppedModel.DragDroppedOnLeave();

                            DragDroppedModel = picked;

                            //Model has changed so call the enter event
                            if (picked != null)
                                picked.DragDroppedOnEnter();
                        }

                        if (picked != null) {
                            //Set the drag/drop event
                            var node = Outliner.GetDragDropNode();
                            picked.DragDropped(node.Tag);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
            }
            ImGui.EndChild();
        }

        private bool onEnter = false;

        private void UpdateCamera()
        {
            var mouseInfo = CreateMouseState();
            var keyInfo = CreateKeyState();

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                Pipeline.OnMouseDown(mouseInfo, keyInfo);
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                Pipeline.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (_mouseDown)
                Pipeline.OnMouseMove(mouseInfo, keyInfo);
            if (ImGuiController.ApplicationHasFocus)
                Pipeline.OnMouseWheel(mouseInfo, keyInfo);

            Pipeline._context.Camera.Controller.KeyPress(keyInfo);
        }

        private KeyEventInfo CreateKeyState()
        {
            var keyInfo = new KeyEventInfo();
            keyInfo.KeyShift = ImGui.GetIO().KeyShift;
            keyInfo.KeyCtrl = ImGui.GetIO().KeyCtrl;
            keyInfo.KeyAlt = ImGui.GetIO().KeyAlt;
            return keyInfo;
        }

        private MouseEventInfo CreateMouseState()
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
            var windowPos = ImGui.GetWindowPos();

            var pos = ImGui.GetIO().MousePos;
            pos = new System.Numerics.Vector2(pos.X - windowPos.X, pos.Y - windowPos.Y);

            if (ImGui.IsMousePosValid())
                mouseInfo.Position = new Point((int)pos.X, (int)pos.Y);
            else
                mouseInfo.HasValue = false;

            return mouseInfo;
        }
    }
}
