using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;
using ImGuiNET;
using CafeStudio.UI;

namespace CafeShaderStudio
{
    public class Config
    {
        public string SMOGamePath = "";
        public string SM3DWGamePath = "";
        public string MK8DGamePath = "";

        [JsonIgnore]
        public bool HasValidSMOPath = false;
        [JsonIgnore]
        public bool HasValidSM3DWPath = false;
        [JsonIgnore]
        public bool HasValidMK8DPath = false;

        /// <summary>
        /// Renders the current configuration UI.
        /// </summary>
        public void RenderUI()
        {
            RenderPathUI("Super Mario Odyssey Path", ref SMOGamePath, HasValidSMOPath);
            RenderPathUI("Super Mario 3DW Path", ref SM3DWGamePath, HasValidSM3DWPath);
            RenderPathUI("Mario Kart 8 Deluxe Path", ref MK8DGamePath, HasValidMK8DPath);
        }

        private void RenderPathUI(string label, ref string path, bool isValid)
        {
            bool clicked = ImGui.Button($"  -  #{label}");

            ImGui.SameLine();
            if (!isValid)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                ImGui.InputText(label, ref path, 500, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopStyleColor();
            }

            if (clicked)
            {
                var dialog = new ImguiFolderDialog();
                if (dialog.ShowDialog()) {
                    path = dialog.SelectedPath;
                    Save();
                }
            }
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static Config Load() {
            if (!File.Exists("Config.json")) { new Config().Save(); }

            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("Config.json"));
            config.Reload();
            return config;
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void Save() {
           File.WriteAllText("Config.json", JsonConvert.SerializeObject(this));
            Reload();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void Reload()
        {
            RedStarLibrary.GlobalSettings.GamePath = SMOGamePath;
            HasValidSMOPath = Directory.Exists($"{SMOGamePath}\\ShaderData");

            BfresEditor.SM3DWShaderLoader.GamePath = SM3DWGamePath;
            HasValidSM3DWPath = Directory.Exists($"{SM3DWGamePath}\\ShaderData");

            TrackStudioLibrary.Turbo.GlobalSettingsMK8.MarioKartDX8Path = MK8DGamePath;
            HasValidMK8DPath = File.Exists($"{SMOGamePath}\\Data\\objflow.byaml");
        }
    }
}
