using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using AGraphicsLibrary;
using System.ComponentModel;
using GLFrameworkEngine;
using CafeStudio.UI;

namespace BfresEditor
{
    public class ColorCorrectionWindow 
    {
        public bool IsActive;

        float _saturation = LightingEngine.LightSettings.ColorCorrection.Saturation;
        float _brightness = LightingEngine.LightSettings.ColorCorrection.Brightness;
        float _gamma = LightingEngine.LightSettings.ColorCorrection.Gamma;
        float _hue = LightingEngine.LightSettings.ColorCorrection.Hue;

        public void Render(GLContext context)
        {
            var colorCorrection = LightingEngine.LightSettings.ColorCorrection;
            if (_saturation != colorCorrection.Saturation)
                PropertyChanged(context);
            if (_gamma != colorCorrection.Gamma)
                PropertyChanged(context);
            if (_brightness != colorCorrection.Brightness)
                PropertyChanged(context);
            if (_hue != colorCorrection.Hue)
                PropertyChanged(context);

            ImGui.Begin("Color Correction");
            ImGui.SliderFloat("Hue", ref _hue, 0.0f, 6.0f);
            ImGui.SliderFloat("Saturation", ref _saturation, 0.0f, 5.0f);
            ImGui.SliderFloat("Brightness", ref _brightness, 0.0f, 5.0f);
            ImGui.SliderFloat("Gamma", ref _gamma, 0.0f, 5.2f);

            var colorTable = LightingEngine.LightSettings.ColorCorrectionTable;
            if (colorTable != null)
            {
                var render = LUTRender.CreateTextureRender(colorTable.ID,
                    colorTable.Width * colorTable.Depth, colorTable.Height);

                ImGui.Image((IntPtr)render, new System.Numerics.Vector2(16 * 16, 16));
            }

            ImGui.End();
        }

        void PropertyChanged(GLContext context) {

            var colorCorrection = LightingEngine.LightSettings.ColorCorrection;

            colorCorrection.Saturation = _saturation;
            colorCorrection.Gamma = _gamma;
            colorCorrection.Brightness = _brightness;
            colorCorrection.Hue = _hue;

            LightingEngine.LightSettings.UpdateColorCorrection = true;
        }
    }
}
