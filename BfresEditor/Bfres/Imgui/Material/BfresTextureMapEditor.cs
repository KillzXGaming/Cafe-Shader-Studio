using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;
using Toolbox.Core;
using BfresLibrary;
using BfresLibrary.GX2;
using System.Numerics;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresTextureMapEditor
    {
        static List<int> SelectedIndices = new List<int>();

        static bool dialogOpened = false;

        public static void Render(FMAT material, UVViewport UVViewport, bool onLoad)
        {
            float width = ImGui.GetWindowWidth();
            if (ImGui.BeginChild("TEXTURE_MAP_LIST", new System.Numerics.Vector2(width, 100)))
            {
                int index = 0;
                foreach (var texMap in material.TextureMaps)
                {
                    var tex = SearchTextureInstance(texMap.Name);
                    if (tex != null)
                    {
                        if (tex.RenderableTex == null)
                            tex.LoadRenderableTexture();

                        if (tex.RenderableTex != null)
                            IconManager.LoadTexture(tex.Name, tex);
                        else
                            IconManager.LoadIcon("TEXTURE");
                    }
                    else
                    {
                        IconManager.LoadIcon("TEXTURE");
                    }
                    ImGui.SameLine();

                    if (dialogOpened && SelectedIndices.Contains(index))
                    {
                        if (TextureSelectionDialog.Render(texMap.Name, ref dialogOpened)) {
                            var input = TextureSelectionDialog.OutputName;
                            //Only undo matching textures, not typed names
                            if (tex != null)
                            {
                                UndoStack.Add(new UndoStringOperation($"Texture Map",
                                     texMap, "Name", input));
                            }
 
                            texMap.Name = input;
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable(texMap.Name, SelectedIndices.Contains(index)))
                        {
                            SelectedIndices.Clear();
                            SelectedIndices.Add(index);
                        }
                        if (ImGui.IsItemClicked() && ImGui.IsMouseDoubleClicked(0))
                        {
                            dialogOpened = true;
                        }
                    }

                    index++;
                }
            }
            ImGui.EndChild();

            if (material.TextureMaps.Count == 0)
                return;

            //Make sure there is atleast 1 selection used
            if (SelectedIndices.Count == 0)
                SelectedIndices.Add(0);

            if (ImGui.BeginChild("SAMPLER_DATA"))
            {
                SelectSampler(material, UVViewport, SelectedIndices.FirstOrDefault(), onLoad);
            }
            ImGui.EndChild();
        }

        static void SelectSampler(FMAT material, UVViewport UVViewport, int index, bool onLoad)
        {
            var materialData = material.Material;

            var sampler = materialData.Samplers[index].TexSampler;
            var texMapSel = materialData.TextureRefs[index];

            //Texture map info
            if (ImGui.CollapsingHeader("Texture Info", ImGuiTreeNodeFlags.DefaultOpen)) {
                ImGuiHelper.InputFromText("Name", texMapSel, "Name", 200);
            }

            var width = ImGui.GetWindowWidth();

            //A UV preview window drawn using opengl
            if (ImGui.CollapsingHeader("Preview", ImGuiTreeNodeFlags.DefaultOpen)) {

                if (ImGui.BeginChild("uv_viewport1", new Vector2(width, 150)))
                {
                    if (onLoad)
                    {
                        var meshes = material.GetMappedMeshes();

                        UVViewport.ActiveObjects.Clear();
                        foreach (FSHP mesh in meshes)
                            UVViewport.ActiveObjects.Add(mesh);
                    }

                    UVViewport.ActiveTextureMap = material.TextureMaps[index];
                    UVViewport.Render((int)width, 150);
                }
                ImGui.EndChild();
            }

            if (ImGui.BeginChild("sampler_properties"))
            {
                LoadProperties(sampler);
                material.ReloadTextureMap(index);
                ImGui.EndChild();
            }
        }

        static void LoadProperties(TexSampler sampler) {
            var flags = ImGuiTreeNodeFlags.DefaultOpen;
            if (ImGui.CollapsingHeader("Wrap Mode", flags))
            {
                ImGuiHelper.ComboFromEnum<GX2TexClamp>("Wrap X", sampler, "ClampX");
                ImGuiHelper.ComboFromEnum<GX2TexClamp>("Wrap Y", sampler, "ClampY");
                ImGuiHelper.ComboFromEnum<GX2TexClamp>("Wrap Z", sampler, "ClampZ");
            }
            if (ImGui.CollapsingHeader("Filter", flags))
            {
                ImGuiHelper.ComboFromEnum<GX2TexXYFilterType>("Mag Filter", sampler, "MagFilter");
                ImGuiHelper.ComboFromEnum<GX2TexXYFilterType>("Min Filter", sampler, "MinFilter");
                ImGuiHelper.ComboFromEnum<GX2TexZFilterType>("Z Filter", sampler, "ZFilter");
                ImGuiHelper.ComboFromEnum<GX2TexMipFilterType>("Mip Filter", sampler, "MipFilter");
                ImGuiHelper.ComboFromEnum<GX2TexAnisoRatio>("Anisotropic Ratio", sampler, "MaxAnisotropicRatio");
            }
            if (ImGui.CollapsingHeader("Mip LOD", flags))
            {
                ImGuiHelper.InputFromFloat("Lod Min", sampler, "MinLod", false, 1);
                ImGuiHelper.InputFromFloat("Lod Max", sampler, "MaxLod", false, 1);
                ImGuiHelper.InputFromFloat("Lod Bias", sampler, "LodBias", false, 1);
            }
            if (ImGui.CollapsingHeader("Depth", flags))
            {
                ImGuiHelper.InputFromBoolean("Depth Enabled", sampler, "DepthCompareEnabled");
                ImGuiHelper.ComboFromEnum<GX2CompareFunction>("Depth Compare", sampler, "DepthCompareFunc");
            }
            if (ImGui.CollapsingHeader("Border", flags))
            {
                ImGuiHelper.ComboFromEnum<GX2TexBorderType>("Border Type", sampler, "BorderType");
            }
        }

        static STGenericTexture SearchTextureInstance(string name)
        {
            foreach (var cache in GLFrameworkEngine.DataCache.ModelCache.Values) {
                if (cache is BfresRender) {
                    if (((BfresRender)cache).Textures.ContainsKey(name))
                        return ((BfresRender)cache).Textures[name];
                }
            }
            return null;
        }
    }
}
