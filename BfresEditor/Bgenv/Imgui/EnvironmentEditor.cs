using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using AGraphicsLibrary;
using ImGuiNET;
using CafeStudio.UI;
using Toolbox.Core;

namespace BfresEditor
{
    public class EnvironmentEditor
    {
        public static bool RenderProperies(HemisphereLight hemi, string id)
        {
            bool edited = false;

            edited |= ImGuiHelper.InputFromBoolean($"Enable{id}", hemi, "Enable");

            edited |= EditColor($"Sky Color", $"{id}_0", hemi, "SkyColor");
            edited |= EditColor($"Ground Color", $"{id}_1", hemi, "GroundColor");

            edited |= ImGuiHelper.InputFromFloat($"Intensity{id}", hemi, "Intensity");

            edited |= EditVector3($"Direction{id}", hemi, "Direction");

            return edited;
        }

        public static bool RenderProperies(DirectionalLight dirLight, string id)
        {
            bool edited = false;

            edited |= ImGuiHelper.InputFromBoolean($"Enable{id}", dirLight, "Enable");

            edited |= EditColor($"Diffuse Color", $"{id}_0", dirLight, "DiffuseColor");
            edited |= EditColor($"Backside Color", $"{id}_1", dirLight, "BacksideColor");

            edited |= ImGuiHelper.InputFromFloat($"Intensity{id}", dirLight, "Intensity");

            edited |= EditVector3($"Direction{id}", dirLight, "Direction");

            return edited;
        }

        public static bool RenderProperies(AmbientLight ambLight, string id)
        {
            bool edited = false;

            return edited;
        }

        static bool EditColor(string label, string id, object obj, string properyName)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (STColor)input.GetValue(obj);
            var color = new Vector4(inputValue.R, inputValue.G, inputValue.B, inputValue.A);

            ImGui.Columns(2);

            var flags = ImGuiColorEditFlags.HDR;

            if (ImGui.ColorButton($"colorBtn{id}", color, flags, new Vector2(200, 22)))
            {
                ImGui.OpenPopup($"colorPicker{id}");
            }

            ImGui.NextColumn();
            ImGui.Text(label);

            ImGui.Columns(1);

            bool edited = false;
            if (ImGui.BeginPopup($"colorPicker{id}"))
            {
                if (ImGui.ColorPicker4("##picker", ref color, flags 
                    | ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB
                    | ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.DisplayHSV))
                {
                    input.SetValue(obj, new STColor()
                    {
                        R = color.X,
                        G = color.Y,
                        B = color.Z,
                        A = color.W,
                    });
                    edited = true;
                }
                ImGui.EndPopup();
            }
            return edited;
        }

        static bool EditVector3(string label, object obj, string properyName)
        {
            var input = obj.GetType().GetProperty(properyName);
            var inputValue = (Syroot.Maths.Vector3F)input.GetValue(obj);
            var vec3 = new Vector3(inputValue.X, inputValue.Y, inputValue.Z);

            bool edited = ImGui.InputFloat3(label, ref vec3);
            if (edited)
            {
                input.SetValue(obj, new Syroot.Maths.Vector3F(vec3.X, vec3.Y, vec3.Z));
            }
            return edited;
        }
    }
}
