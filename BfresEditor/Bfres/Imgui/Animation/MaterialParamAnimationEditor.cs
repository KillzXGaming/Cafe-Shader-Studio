using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresEditor;
using ImGuiNET;
using System.Numerics;
using Toolbox.Core.Animations;
using BfresLibrary;
using CafeStudio.UI;

namespace BfresEditor
{
    public class MaterialParamAnimationEditor
    {
        static string SelectedMaterial = "";
        static string SelectedParam = "";
        static bool isColor = false;
        static bool showInterpolated = true;

        public void LoadEditor(BfresMaterialAnim anim)
        {
            if (string.IsNullOrEmpty(SelectedMaterial))
                SelectedMaterial = anim.AnimGroups.FirstOrDefault().Name;

            if (ImGui.CollapsingHeader("Header"))
            {
                ImGuiHelper.InputFromText("Name", anim, "Name", 200);
                ImGuiHelper.InputFromFloat("FrameCount", anim, "FrameCount");
                ImGuiHelper.InputFromBoolean("Loop", anim, "Loop");
            }

            if (ImGui.BeginCombo("Material", SelectedMaterial))
            {
                foreach (var group in anim.AnimGroups)
                {
                    bool isSelected = group.Name == SelectedMaterial;
                    if (ImGui.Selectable(group.Name) || isSelected) {
                        SelectedMaterial = group.Name;
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            foreach (var group in anim.AnimGroups)
            {
                bool isSelected = group.Name == SelectedMaterial;
                if (isSelected)
                    RenderMaterial(anim, (BfresMaterialAnim.MaterialAnimGroup)group);
            }
        }

        public static void RenderMaterial(BfresMaterialAnim anim, 
            BfresMaterialAnim.MaterialAnimGroup group)
        {
            if (string.IsNullOrEmpty(SelectedParam))
                SelectedParam = group.SubAnimGroups.FirstOrDefault().Name;

            if (ImGui.BeginCombo("Params", SelectedParam))
            {
                foreach (var subgroup in group.SubAnimGroups)
                {
                    if (subgroup is BfresMaterialAnim.ParamAnimGroup)
                    {
                        bool isSelected = subgroup.Name == SelectedParam;
                        if (ImGui.Selectable(subgroup.Name) || isSelected) {
                            SelectedParam = subgroup.Name;
                            UpdateParamInfo(anim, subgroup);
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
      
            foreach (var subgroup in group.SubAnimGroups)
            {
                bool isSelected = subgroup.Name == SelectedParam;
                if (isSelected)
                    RenderParamEditor(anim, (BfresMaterialAnim.ParamAnimGroup)subgroup);
            }
        }

        class ParamProperties
        {
            public BfresLibrary.ShaderParamType ParamType { get; set; }
        }

        static ParamProperties ParamProperty = new ParamProperties();

        public static void RenderParamEditor(BfresMaterialAnim anim, 
            BfresMaterialAnim.ParamAnimGroup paramGroup)
        {
            if (ImGui.CollapsingHeader("Track Data", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.ComboFromEnum<BfresLibrary.ShaderParamType>("Param Type", ParamProperty, "ParamType");

                ImGui.Checkbox("Show Full Interpolation", ref showInterpolated);

                if (ParamProperty.ParamType == ShaderParamType.Float3 ||
                    ParamProperty.ParamType == ShaderParamType.Float4)
                {
                    ImGui.Checkbox("Is Color", ref isColor);
                }
            }
            RenderColorParamEditor(anim, paramGroup);
        }

        static void UpdateParamInfo(BfresMaterialAnim anim, STAnimGroup paramGroup)
        {
            var materials = anim.GetMaterials(SelectedMaterial);
            if (materials.Count > 0)
            {
                var mat = materials.FirstOrDefault();
                if (mat.ShaderParams.ContainsKey(paramGroup.Name))
                    ParamProperty.ParamType = mat.ShaderParams[paramGroup.Name].Type;
            }
            if (paramGroup.Name.Contains("color") || paramGroup.Name.Contains("Color"))
                isColor = true;
        }

        static float floatInsert1 = new float();
        static Vector2 floatInsert2 = new Vector2();
        static Vector3 floatInsert3 = new Vector3();
        static Vector4 floatInsert4 = new Vector4();
        static TextureSRT texSRTtInsert = new TextureSRT();

        static int frameInsert;
        static List<int> selectedFrames = new List<int>();
        static bool shownPopup = false;
        static bool deleteSelected = false;

        public static void RenderColorParamEditor(BfresMaterialAnim anim, BfresMaterialAnim.ParamAnimGroup paramGroup) {
            var tracks = paramGroup.GetTracks();

            if (ImGui.Button("Insert Key")) {
                var insertedValue = GetInsertValue();
                //Insert current key and data type used
                InsertKey(paramGroup, ParamProperty.ParamType, frameInsert, insertedValue);
                //Reload frame view
                anim.SetFrame(anim.Frame);
            }

            ImGui.SameLine();
            if (ImGui.Button("Remove Key")) {
                deleteSelected = true;  
            }

            bool isTexSRT = ParamProperty.ParamType == ShaderParamType.TexSrt ||
                ParamProperty.ParamType == ShaderParamType.TexSrtEx;

            //Do headers
            if (isTexSRT)
            {
                ImGui.Columns(7);
                ImGui.Text("Frame");
                ImGui.NextColumn();
                ImGui.Text("Mode");
                ImGui.NextColumn();
                ImGui.Text("Scale X");
                ImGui.NextColumn();
                ImGui.Text("Scale Y");
                ImGui.NextColumn();
                ImGui.Text("Rotate");
                ImGui.NextColumn();
                ImGui.Text("Translate X");
                ImGui.NextColumn();
                ImGui.Text("Translate Y");
                ImGui.NextColumn();

                ImGui.Separator();
            }
            else
            {
                ImGui.Columns(2);
                ImGui.Text("Frame");
                ImGui.NextColumn();
                ImGui.Text("Value");
                ImGui.NextColumn();
                ImGui.Separator();
            }

            RenderInsertPanel(anim, paramGroup);

            ImGui.Separator();

            ImGui.Columns(1);

            ImGui.BeginChild("##PARAM_ANIM");

            for (int i = 0; i < anim.FrameCount; i++)
            {
                if (!showInterpolated && !tracks.Any(x => x.IsKeyed(i)))
                    continue;

                if (isTexSRT)
                    ImGui.Columns(7);
                else
                    ImGui.Columns(2);

                if (ImGui.Selectable(i.ToString(), selectedFrames.Contains(i)))
                {
                    if (!OpenTK.Input.Keyboard.GetState().IsKeyDown(OpenTK.Input.Key.ShiftLeft))
                        selectedFrames.Clear();
                    selectedFrames.Add(i);
                }

                if (selectedFrames.Contains(i) && !shownPopup)
                {
                    if (ImGui.BeginPopupContextItem("##KEY_POPUP", ImGuiPopupFlags.MouseButtonRight)) {
                        shownPopup = true;

                        if (ImGui.Selectable("Delete")) {
                            deleteSelected = true;
                        }
                        ImGui.EndPopup();
                    }
                }

                ImGui.NextColumn();

                var editedValue = DisplayParam($"##FRAME{i}", GetDataValue(tracks, i));
                if (editedValue != null)
                {
                    InsertKey(paramGroup, ParamProperty.ParamType, i, editedValue);

                    //Reload frame view
                    anim.SetFrame(anim.Frame);
                }

                ImGui.NextColumn();
            }

            if (deleteSelected)
            {
                foreach (var frame in selectedFrames) {
                    paramGroup.RemoveKey(frame);
                }
                deleteSelected = false;
            }

            shownPopup = false;

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        static void RenderInsertPanel(BfresMaterialAnim anim, BfresMaterialAnim.ParamAnimGroup paramGroup)
        {
            ImGui.InputInt("Frame", ref frameInsert, 0);
            ImGui.NextColumn();

            //Display the value to insert into the key list if the insert button gets presse
            switch (ParamProperty.ParamType)
            {
                case ShaderParamType.Float:
                    DisplayParam("##INSERT", floatInsert1);
                    break;
                case ShaderParamType.Float2:
                    DisplayParam("##INSERT", floatInsert2);
                    break;
                case ShaderParamType.Float3:
                    var editedValue = DisplayParam("##INSERT", floatInsert3);
                    if (editedValue != null)
                        floatInsert3 = (Vector3)editedValue;
                    break;
                case ShaderParamType.Float4:
                    DisplayParam("##INSERT", floatInsert4);
                    break;
                case ShaderParamType.TexSrtEx:
                case ShaderParamType.TexSrt:
                    DisplayParam("##INSERT", texSRTtInsert);
                    break;
            }
        }

        static object DisplayParam(string key, object value)
        {
            switch (ParamProperty.ParamType)
            {
                case ShaderParamType.Float:
                    {
                        float output = (float)value;
                        if (ImGui.DragFloat(key, ref output))
                            return output;
                        break;
                    }
                case ShaderParamType.Float2:
                    {
                        Vector2 output = (Vector2)value;
                        if (ImGui.DragFloat2(key, ref output))
                            return output;
                        break;
                    }
                case ShaderParamType.Float3:
                    {
                        Vector3 output = (Vector3)value;
                        if (isColor)
                        {
                            if (ImGui.ColorEdit3(key, ref output, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float))
                                return output;
                        }
                        else
                        {
                            if (ImGui.DragFloat3(key, ref output))
                                return output;
                        }
                        break;
                    }
                case ShaderParamType.Float4:
                    {
                        Vector4 output = (Vector4)value;
                        if (isColor)
                        {
                            if (ImGui.ColorEdit4(key, ref output, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Float))
                                return output;
                        }
                        else
                        {
                            if (ImGui.DragFloat4(key, ref output))
                                return output;
                        }
                        break;
                    }
                case ShaderParamType.TexSrtEx:
                case ShaderParamType.TexSrt:
                    {
                        TextureSRT output = (TextureSRT)value;

                        //6 columns
                        bool edited = false;
                        edited = edited || ImGuiHelper.ComboFromEnum<TexSrtMode>(key+"C0", output, "Mode", ImGuiComboFlags.NoArrowButton);
                        ImGui.NextColumn();
                        edited = edited || ImGuiHelper.InputFromFloat(key + "C1", output, "ScaleX", true, 0);
                        ImGui.NextColumn();
                        edited = edited || ImGuiHelper.InputFromFloat(key + "C2", output, "ScaleY", true, 0);
                        ImGui.NextColumn();
                        edited = edited || ImGuiHelper.InputFromFloat(key + "C3", output, "Rotate", true, 0);
                        ImGui.NextColumn();
                        edited = edited || ImGuiHelper.InputFromFloat(key + "C4", output, "TranslateX", true, 0);
                        ImGui.NextColumn();
                        edited = edited || ImGuiHelper.InputFromFloat(key + "C5", output, "TranslateY", true, 0);

                        if (edited)
                            return output;
                        break;
                    }
            }
            return null;
        }

        static object GetInsertValue()
        {
            switch (ParamProperty.ParamType)
            {
                case ShaderParamType.Float: return floatInsert1;
                case ShaderParamType.Float2: return floatInsert2;
                case ShaderParamType.Float3: return floatInsert3;
                case ShaderParamType.Float4: return floatInsert4;
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx: 
                    return texSRTtInsert;
            }
            return null;
        }

        static object GetDataValue(List<STAnimationTrack> tracks, int frame)
        {
            switch (ParamProperty.ParamType)
            {
                case ShaderParamType.Float:
                    {
                        float output = 0;
                        foreach (var track in tracks)
                        {
                            if (track.Name == "0") output = track.GetFrameValue(frame);
                        }
                        return output;
                    }
                case ShaderParamType.Float2:
                    {
                        Vector2 output = new Vector2();
                        foreach (var track in tracks)
                        {
                            if (track.Name == "0") output.X = track.GetFrameValue(frame);
                            if (track.Name == "4") output.Y = track.GetFrameValue(frame);
                        }
                        return output;
                    }
                case ShaderParamType.Float3:
                    {
                        Vector3 output = new Vector3();
                        foreach (var track in tracks)
                        {
                            if (track.Name == "0") output.X = track.GetFrameValue(frame);
                            if (track.Name == "4") output.Y = track.GetFrameValue(frame);
                            if (track.Name == "8") output.Z = track.GetFrameValue(frame);
                        }
                        return output;
                    }
                case ShaderParamType.Float4:
                    {
                        Vector4 output = new Vector4();
                        foreach (var track in tracks)
                        {
                            if (track.Name == "0") output.X = track.GetFrameValue(frame);
                            if (track.Name == "4") output.Y = track.GetFrameValue(frame);
                            if (track.Name == "8") output.Z = track.GetFrameValue(frame);
                            if (track.Name == "C") output.W = track.GetFrameValue(frame);
                        }
                        return output;
                    }
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    {
                        var texSRT = new TextureSRT();
                        foreach (var track in tracks)
                        {
                            if (track.Name == "0") texSRT.Mode = (TexSrtMode)track.GetFrameValue(frame);
                            if (track.Name == "4") texSRT.ScaleX = track.GetFrameValue(frame);
                            if (track.Name == "8") texSRT.ScaleY = track.GetFrameValue(frame);
                            if (track.Name == "C") texSRT.Rotate = track.GetFrameValue(frame);
                            if (track.Name == "10") texSRT.TranslateX = track.GetFrameValue(frame);
                            if (track.Name == "14") texSRT.TranslateY = track.GetFrameValue(frame);
                        }
                        return texSRT;
                    }
            }
            return null;
        }

        static void InsertKey(BfresMaterialAnim.ParamAnimGroup group, ShaderParamType type, float frame, object value)
        {
            //This method will insert tracks to the total amount of possible values for that parameter
            //Any unecessary values will be optimized later
            switch (type)
            {
                case ShaderParamType.TexSrt:
                case ShaderParamType.TexSrtEx:
                    {
                        var input = (TextureSRT)value;
                        group.InsertKey(frame, 0, (int)input.Mode, 0, 0);
                        group.InsertKey(frame, 4, input.ScaleX, 0, 0);
                        group.InsertKey(frame, 8, input.ScaleY, 0, 0);
                        group.InsertKey(frame, 12, input.Rotate, 0, 0);
                        group.InsertKey(frame, 16, input.TranslateX, 0, 0);
                        group.InsertKey(frame, 20, input.TranslateY, 0, 0);
                    }
                    break;
                case ShaderParamType.Float:
                    {
                        var input = (float)value;
                        group.InsertKey(frame, 0, input, 0, 0);
                    }
                    break;
                case ShaderParamType.Float2:
                    {
                        var input = (Vector2)value;
                        group.InsertKey(frame, 0, input.X, 0, 0);
                        group.InsertKey(frame, 4, input.Y, 0, 0);
                    }
                    break;
                case ShaderParamType.Float3:
                    {
                        var input = (Vector3)value;
                        group.InsertKey(frame, 0, input.X, 0, 0);
                        group.InsertKey(frame, 4, input.Y, 0, 0);
                        group.InsertKey(frame, 8, input.Z, 0, 0);
                    }
                    break;
                case ShaderParamType.Float4:
                    {
                        var input = (Vector4)value;
                        group.InsertKey(frame, 0, input.X, 0, 0);
                        group.InsertKey(frame, 4, input.Y, 0, 0);
                        group.InsertKey(frame, 8, input.Z, 0, 0);
                        group.InsertKey(frame, 12, input.W, 0, 0);
                    }
                    break;
            }
        }

        public class TextureSRT
        {
            public TexSrtMode Mode { get; set; }
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public float Rotate { get; set; }
            public float TranslateX { get; set; }
            public float TranslateY { get; set; }
        }
    }
}
