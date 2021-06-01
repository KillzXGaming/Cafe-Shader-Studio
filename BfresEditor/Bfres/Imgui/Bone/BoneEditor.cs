using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using BfresEditor;
using ImGuiNET;
using BfresLibrary;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BoneEditor
    {
        public void LoadEditor(BfresBone bone) {
            ImGui.BeginTabBar("bone_tab");
            if (ImguiCustomWidgets.BeginTab("bone_tab", "Bone Data"))
            {
                LoadBoneTab(bone);
                ImGui.EndTabItem();
            }
            if (ImguiCustomWidgets.BeginTab("bone_tab", "User Data"))
            {
                UserDataInfoEditor.Render(bone.BoneData.UserData);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        static void LoadBoneTab(BfresBone bone)
        {
            var boneData = bone.BoneData;
            if (ImGui.CollapsingHeader("Bone Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.InputFromBoolean("Visible", bone, "Visible");
                ImGuiHelper.InputFromText("Name", bone, "Name", 200);

                int index = bone.Index;
                ImGui.SameLine(); ImGui.InputInt("Index", ref index);
                var parent = bone.Parent as BfresBone;
                if (parent != null)
                {
                    var parentIndex = parent.Index;
                    ImGuiHelper.InputFromText("Parent", parent, "Name", 200, ImGuiInputTextFlags.ReadOnly);
                    ImGui.SameLine(); ImGui.InputInt("Index", ref parentIndex);
                }
            }
            if (ImGui.CollapsingHeader("Bone Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var transform = ImguiCustomWidgets.Transform(bone.Position, bone.EulerRotation, bone.Scale);
                if (transform.Edited)
                {
                    bone.Position = new OpenTK.Vector3(
                        transform.Position.X, transform.Position.Y, transform.Position.Z);
                    bone.Scale = new OpenTK.Vector3(
                       transform.Scale.X, transform.Scale.Y, transform.Scale.Z);
                    bone.EulerRotation = new OpenTK.Vector3(
                       transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z);
                    bone.UpdateBfresTransform();
                    bone.UpdateTransform();
                }
            }
            if (ImGui.CollapsingHeader("Billboard", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.ComboFromEnum<BoneFlagsBillboard>("Type", boneData, "FlagsBillboard");
                ImGuiHelper.InputFromShort("Index", boneData, "BillboardIndex");

            }
            if (ImGui.CollapsingHeader("Matrix Indices", ImGuiTreeNodeFlags.DefaultOpen))
            {
                int smoothIndex = boneData.SmoothMatrixIndex;
                int rigidIndex = boneData.RigidMatrixIndex;

                ImGui.InputInt("Smooth Index", ref smoothIndex);
                ImGui.InputInt("Rigid Index", ref rigidIndex);
            }
            if (ImGui.CollapsingHeader("Transform Modes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                ImGuiHelper.InputFromBoolean("Identity", boneData, "TransformIdentity");
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGuiHelper.InputFromBoolean("Rotate Translate Zero", boneData, "TransformRotateTranslateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale One", boneData, "TransformScaleOne");
                ImGui.NextColumn();

                ImGuiHelper.InputFromBoolean("Rotate Zero", boneData, "TransformRotateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale Uniform", boneData, "TransformScaleUniform");
                ImGui.NextColumn();

                ImGuiHelper.InputFromBoolean("Translate Zero", boneData, "TransformTranslateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale Volume One", boneData, "TransformScaleVolumeOne");
                ImGui.NextColumn();

                ImGui.Columns(1);
            }
            if (ImGui.CollapsingHeader("Hierarchy Transform Modes", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                ImGuiHelper.InputFromBoolean("Identity", boneData, "TransformCumulativeIdentity");
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGuiHelper.InputFromBoolean("Rotate Translate Zero", boneData, "TransformCumulativeRotateTranslateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale One", boneData, "TransformCumulativeScaleOne");
                ImGui.NextColumn();

                ImGuiHelper.InputFromBoolean("Rotate Zero", boneData, "TransformCumulativeRotateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale Uniform", boneData, "TransformCumulativeScaleUniform");
                ImGui.NextColumn();


                ImGuiHelper.InputFromBoolean("Translate Zero", boneData, "TransformCumulativeTranslateZero");
                ImGui.NextColumn();
                ImGuiHelper.InputFromBoolean("Scale Volume One", boneData, "TransformCumulativeScaleVolumeOne");
                ImGui.NextColumn();

                ImGui.Columns(1);
            }
        }
    }
}
