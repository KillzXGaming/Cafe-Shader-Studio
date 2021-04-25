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
        public bool HasValidSMOPath = false;

        /// <summary>
        /// Renders the current configuration UI.
        /// </summary>
        public void RenderUI()
        {
            RenderPathUI("Mario Odyssey Path", ref SMOGamePath);
        }

        private void RenderPathUI(string label, ref string path)
        {
            bool clicked = ImGui.Button("  -  ");

            ImGui.SameLine();
            if (!HasValidSMOPath)
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
                var dialog = new FolderBrowserEx.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    path = dialog.SelectedFolder;
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
        }
    }
}
