using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using BfresEditor;
using ImGuiNET;
using BfresLibrary;
using System.Reflection;
using CafeStudio.UI;

namespace BfresEditor
{
    public class MaterialParameter
    {
        public static Dictionary<string, string[]> CustomCategory = new Dictionary<string, string[]>();

        static bool drag = true;

        public static void Init()
        {
            CustomCategory.Add("Area", new string[] { "gsys_area_env_index_diffuse", "gsys_area_env_data0", "gsys_area_env_data1" });
            CustomCategory.Add("Shadow", new string[] { "shadow_density", "ao_density" });
            CustomCategory.Add("Edge Lighting", new string[] {
                "gsys_i_color_ratio0", "gsys_edge_ratio0", "gsys_edge_width0", "game_edge_pow",
                "edge_alpha_scale","edge_alpha_width", "edge_alpha_pow", "edge_light_sharpness", });

            CustomCategory.Add("Alpha", new string[] { "transparency", "alphat_out_start", "alphat_out_end", "gsys_alpha_test_ref_value" });
            CustomCategory.Add("Bloom", new string[] { "bloom_intensity", });
            CustomCategory.Add("Bake", new string[] { "d_shadow_bake_l_cancel_rate", "gsys_bake_st0", "gsys_bake_st1",
                "gsys_bake_light_scale", "gsys_bake_light_scale1", "gsys_bake_light_scal2" });
            CustomCategory.Add("Fog", new string[] { "fog_emission_intensity", "fog_emission_effect",
                "fog_edge_power","fog_edge_width", "fog_edge_color", "fog_emission_color", });
            CustomCategory.Add("Emission", new string[] { "emission_intensity", "emission_color", });
            CustomCategory.Add("Specular", new string[] { "specular_aniso_power", "shiny_specular_intensity", "specular_intensity",
            "specular_roughness", "specular_fresnel_i", "specular_fresnel_s", "specular_fresnel_m", "shiny_specular_sharpness",
                "shiny_specular_fresnel", "specular_color", "shiny_specular_color", });
        }

        public static void Reset()
        {
            OriginalValues.Clear();
        }

        static List<int> selectedIndices = new List<int>();

        static bool limitUniformsUsedByShaderCode = true;

        static float columnSize1;
        static float columnSize2;
        static float columnSize3;

        public static void Render(FMAT material)
        {
            if (CustomCategory.Count == 0)
                Init();


            TegraShaderDecoder.ShaderInfo shaderInfo = null;
            if (material.MaterialAsset is BfshaRenderer)
            {
                shaderInfo = ((BfshaRenderer)material.MaterialAsset).GLShaderInfo;
            }

            if (shaderInfo != null)
                ImGui.Checkbox("Display Only Used Uniforms From Shader", ref limitUniformsUsedByShaderCode);

            if (OriginalValues.Count == 0)
            {
                foreach (var param in material.ShaderParams)
                    OriginalValues.Add(param.Key, param.Value.DataValue);
            }


            LoadHeaders();
            if (ImGui.BeginChild("PARAM_LIST"))
            {
                int index = 0;
                foreach (var param in material.ShaderParams.Values)
                {

                    if (limitUniformsUsedByShaderCode && shaderInfo != null &&
                        !shaderInfo.UsedVertexStageUniforms.Contains(param.Name) &&
                        !shaderInfo.UsedPixelStageUniforms.Contains(param.Name))
                        continue;

                    if (material.AnimatedParams.ContainsKey(param.Name))
                    {
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                        LoadParamColumns(material.AnimatedParams[param.Name], index++, true);
                        ImGui.PopStyleColor();
                    }
                    else
                        LoadParamColumns(param, index++);
                }
            }
            ImGui.EndChild();
        }

        static void LoadHeaders()
        {
            ImGui.Columns(3);
            if (ImGui.Selectable("Name"))
            {

            }
            columnSize1 = ImGui.GetColumnWidth();
            ImGui.NextColumn();
            if (ImGui.Selectable("Value"))
            {
            }
            columnSize2 = ImGui.GetColumnWidth();
            ImGui.NextColumn();
            if (ImGui.Selectable("Colors (If Used)"))
            {
            }
            columnSize3 = ImGui.GetColumnWidth();
            ImGui.Separator();
            ImGui.Columns(1);
        }

        static void LoadParamColumns(ShaderParam param, int index, bool animated = false)
        {
            ImGui.Columns(3);

            if (selectedIndices.Contains(index))
            {
                ImGui.Columns(1);
                if (ImGui.CollapsingHeader(param.Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    LoadParamUI(param, $"##{param.Name}", drag);

                    if (OriginalValues[param.Name] != param.DataValue)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button("Reset"))
                        {
                            param.DataValue = OriginalValues[param.Name];
                        }
                    }
                }
                ImGui.Columns(3);
            }
            else
            {
                if (animated)
                {
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0.5f, 0, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
                }

                ImGui.SetColumnWidth(0, columnSize1);
                ImGui.SetColumnWidth(1, columnSize2);
                ImGui.SetColumnWidth(2, columnSize3);

                if (ImGui.Selectable(param.Name, selectedIndices.Contains(index), ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedIndices.Clear();
                    selectedIndices.Add(index);
                }

                ImGui.NextColumn();
                ImGui.Text(GetDataString(param));
                ImGui.NextColumn();

                if (animated)
                {
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                }

                if (param.Type == ShaderParamType.Float4)
                {
                    if (param.Name.Contains("color") || param.Name.Contains("Color"))
                        ImGuiHelper.InputFloatsFromColor4Button("", param, "DataValue", ImGuiColorEditFlags.AlphaPreviewHalf);
                }
                else if (param.Type == ShaderParamType.Float3)
                {
                    if (param.Name.Contains("color") || param.Name.Contains("Color"))
                        ImGuiHelper.InputFloatsFromColor3Button("", param, "DataValue");
                }

                ImGui.NextColumn();

                ImGui.Columns(1);
            }
        }

        static Dictionary<string, object> OriginalValues = new Dictionary<string, object>();

        static void LoadParamUI(ShaderParam param, string label = "", bool drag = false)
        {
            switch (param.Type)
            {
                case ShaderParamType.Float:
                    {
                        ImGuiHelper.InputFromFloat(label, param, "DataValue", drag);
                    }
                    break;
                case ShaderParamType.Float2:
                    {
                        ImGuiHelper.InputFloatsFromVector2(label, param, "DataValue", drag);
                    }
                    break;
                case ShaderParamType.Float3:
                    {
                        if (param.Name.Contains("color") || param.Name.Contains("Color"))
                            ImGuiHelper.InputFloatsFromColor3(label, param, "DataValue");
                        else
                            ImGuiHelper.InputFloatsFromVector3(label, param, "DataValue", drag);
                    }
                    break;
                case ShaderParamType.Float4:
                    {
                        if (param.Name.Contains("color") || param.Name.Contains("Color"))
                            ImGuiHelper.InputFloatsFromColor4(label, param, "DataValue", ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf);
                        else
                            ImGuiHelper.InputFloatsFromVector4(label, param, "DataValue", drag);
                    }
                    break;
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    {
                        TexSrt value = (TexSrt)param.DataValue;
                        bool edited3 = ImGuiHelper.ComboFromEnum<TexSrtMode>("Mode", value, "Mode");
                        var pos = new Vector2(value.Translation.X, value.Translation.Y);
                        var scale = new Vector2(value.Scaling.X, value.Scaling.Y);
                        var rot = value.Rotation;

                        bool edited0 = ImGui.DragFloat2("Scale", ref scale);
                        bool edited1 = ImGui.DragFloat("Rotate", ref rot, 0.1f);
                        bool edited2 = ImGui.DragFloat2("Translate", ref pos);
                        if (edited0 || edited1 || edited2 || edited3)
                        {
                            param.DataValue = new TexSrt()
                            {
                                Mode = value.Mode,
                                Scaling = new Syroot.Maths.Vector2F(scale.X, scale.Y),
                                Translation = new Syroot.Maths.Vector2F(pos.X, pos.Y),
                                Rotation = rot,
                            };
                        }
                    }
                    break;
            }
        }

        static string GetDataString(ShaderParam Param)
        {
            switch (Param.Type)
            {
                case ShaderParamType.Float:
                case ShaderParamType.UInt:
                    return Param.DataValue.ToString();
                case ShaderParamType.Float2:
                case ShaderParamType.Float3:
                case ShaderParamType.Float4:
                    return string.Join(",", (float[])Param.DataValue);
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    {
                        var texSrt = (TexSrt)Param.DataValue;
                        return $"{texSrt.Mode} {texSrt.Scaling.X} {texSrt.Scaling.Y} {texSrt.Rotation} {texSrt.Translation.X} {texSrt.Translation.Y}";
                    }
                default:
                    return Param.DataValue.ToString();
            }
        }
    }
}
