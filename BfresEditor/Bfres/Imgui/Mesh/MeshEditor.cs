using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using CafeStudio.UI;

namespace BfresEditor
{
    public class MeshEditor
    {
        public void Render(FSHP shape)
        {
            var model = shape.ParentModel;

            if (ImGui.CollapsingHeader("Shape Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.InputFromText("Name", shape, "Name", 200);

                //Doesn't need a direct property bind as property is readonly
                var skinCount = (int)shape.VertexSkinCount;
                ImGui.InputInt("Vertex Skin Count", ref skinCount, 0, 0, ImGuiInputTextFlags.ReadOnly);
                LoadLODSelector(shape);
            }
            if (ImGui.CollapsingHeader("Material Info", ImGuiTreeNodeFlags.DefaultOpen)) {
                LoadMaterialSelector(model, shape);
            }
            if (ImGui.CollapsingHeader("Bone Info", ImGuiTreeNodeFlags.DefaultOpen)) {
                LoadBoneSelector(model, shape);
            }
            if (ImGui.CollapsingHeader("Vertex Buffer Info", ImGuiTreeNodeFlags.DefaultOpen))
            {

            }
            if (ImGui.CollapsingHeader("LOD Info"))
            {

            }
            if (ImGui.CollapsingHeader("Shape Morph Info"))
            {

            }
        }

        static void LoadMaterialSelector(FMDL model, FSHP shape)
        {
            string selectedMaterial = model.Materials[shape.Shape.MaterialIndex].Name;
            if (ImGui.BeginCombo("Material", selectedMaterial))
            {
                for (int i = 0; i < model.Materials.Count; i++)
                {
                    bool isSelected = model.Materials[i].Name == selectedMaterial;
                    if (ImGui.Selectable(model.Materials[i].Name, isSelected))
                    {
                        shape.Shape.MaterialIndex = (ushort)i;
                        shape.Material = (FMAT)model.Materials[i];
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        static void LoadBoneSelector(FMDL model, FSHP shape)
        {
            string selecteBone = model.Skeleton.Bones[shape.BoneIndex].Name;
            if (ImGui.BeginCombo("Bone", selecteBone))
            {
                for (int i = 0; i < model.Skeleton.Bones.Count; i++)
                {
                    bool isSelected = model.Skeleton.Bones[i].Name == selecteBone;
                    if (ImGui.Selectable(model.Skeleton.Bones[i].Name, isSelected)) {
                        shape.BoneIndex = (ushort)i;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }

        static void LoadLODSelector(FSHP shape)
        {
            var displayLOD = shape.DisplayLOD;
            if (ImGui.BeginCombo("Display LOD", $"Mesh{displayLOD}"))
            {
                for (int i = 0; i < shape.PolygonGroups.Count; i++)
                {
                    bool isSelected = displayLOD == i;
                    if (ImGui.Selectable($"Mesh{i}", isSelected)) {
                        shape.DisplayLOD = i;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
        }
    }
}
