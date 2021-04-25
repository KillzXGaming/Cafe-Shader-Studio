﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using CurveEditorLibrary;
using Toolbox.Core.Animations;

namespace CafeStudio.UI
{
    public class TimelineWindow
    {
        AnimationPlayer AnimationPlayer { get; set; }
        AnimationControl CurveEditor { get; set; }
        private bool _mouseDown;
        private bool onEnter = false;

        public void OnLoad()
        {
            AnimationPlayer = new AnimationPlayer();
            CurveEditor = new AnimationControl();

            CurveEditor.OnLoad();
            CurveEditor.SetShaderParameter("backgroundColor", System.Drawing.Color.FromArgb(40, 40, 40, 40));
            CurveEditor.BackColor = System.Drawing.Color.FromArgb(40, 40, 40, 40);
            AnimationPlayer.OnFrameChanged += delegate
            {
                CurveEditor.CurrentFrame = (int)AnimationPlayer.CurrentFrame;
            };

            CurveEditor.OnFrameChanged += delegate {
                AnimationPlayer.SetFrame(CurveEditor.CurrentFrame);
            };
            CurveEditor.OnFrameCountChanged += delegate {
                AnimationPlayer.FrameCount = CurveEditor.FrameCount;
            };
        }

        public void ClearAnimations() {
            AnimationPlayer.CurrentAnimations.Clear();
            Reset();
        }

        public void Reset() {
            AnimationPlayer.ResetModels();
        }

        public void AddAnimation(STAnimation animation, bool reset = true) {
            AnimationPlayer.AddAnimation(animation, "", reset);
            AnimationPlayer.SetFrame(0);

            //Todo, high frame counts can cause freeze issues atm
           // if (AnimationPlayer.FrameCount < 500)
              //  CurveEditor.SetFrameRange(AnimationPlayer.FrameCount);
        }

        public void Render()
        {
            if (AnimationPlayer.IsPlaying)
                AnimationPlayer.UpdateAnimationFrame();

            if (!AnimationPlayer.IsPlaying)
            {
                if (ImGui.Button("Play")) {
                    AnimationPlayer.Play();
                }
            }
            else
            {
                if (ImGui.Button("Stop")) {
                    AnimationPlayer.Pause();
                }
            }

            if (ImGui.BeginChild("timeline_child1"))
            {
                DrawCurveTimeline();
            }
            ImGui.EndChild();
        }

        private void DrawCurveTimeline()
        {
            var size = ImGui.GetWindowSize();
            var pos = ImGui.GetCursorPos();
            var viewerSize = size - pos;
            if (CurveEditor.Width != viewerSize.X || CurveEditor.Height != viewerSize.Y)
            {
                CurveEditor.Width = (int)viewerSize.X;
                CurveEditor.Height = (int)viewerSize.Y;
                CurveEditor.Resize();
            }

            if (CurveEditor.FrameCount != AnimationPlayer.FrameCount)
                CurveEditor.FrameCount = (int)AnimationPlayer.FrameCount;

            var backgroundColor = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
            CurveEditor.SetShaderParameter("backgroundColor", new OpenTK.Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f));

            if (ImGui.IsWindowHovered() && ImGui.IsWindowFocused() || _mouseDown)
                UpdateCurveEvents();
            else
                onEnter = true;

            CurveEditor.Render();
            var id = CurveEditor.GetTextureID();
            ImGui.Image((IntPtr)id, viewerSize,
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0));

            ImGui.SetCursorPos(pos);
            CurveEditor.DrawText();
        }

        private float previousMouseWheel;

        private void UpdateCurveEvents()
        {
            var mouseInfo = ImGuiHelper.CreateMouseState();

            bool controlDown = ImGui.GetIO().KeyCtrl;
            bool shiftDown = ImGui.GetIO().KeyShift;

            if (onEnter)
            {
                CurveEditor.ResetMouse(mouseInfo);
                onEnter = false;
            }

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                CurveEditor.OnMouseDown(mouseInfo);
                previousMouseWheel = 0;
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                CurveEditor.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (previousMouseWheel == 0)
                previousMouseWheel = mouseInfo.WheelPrecise;

            mouseInfo.Delta = mouseInfo.WheelPrecise - previousMouseWheel;
            previousMouseWheel = mouseInfo.WheelPrecise;

            //  if (_mouseDown)
            CurveEditor.OnMouseMove(mouseInfo);
            CurveEditor.OnMouseWheel(mouseInfo, controlDown, shiftDown);
        }
    }
}