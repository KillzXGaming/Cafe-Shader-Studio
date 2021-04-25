using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using ImGuiNET;
using AGraphicsLibrary;
using System.ComponentModel;
using GLFrameworkEngine;
using CafeStudio.UI;

namespace CafeStudio.UI
{
    public class CubemapUintWindow 
    {
        public bool IsActive;

        int selectedAreaIndex;
        float zFar;
        float zNear;
        Vector3 position;
        string name;
        float illuminant_dist;
        int gaussian_repetition_num;
        int rendering_repetition_num;
        bool enable;

        public void Render(GLContext context)
        {
            var cubemaps = LightingEngine.LightSettings.CubeMaps.CubeMapObjects;

            List<string> areas = new List<string>();
            foreach (var cmap in cubemaps)
                areas.Add(cmap.CubeMapUint.Name);

            var cubemapObj = cubemaps[0];
            UpdateSelected(cubemapObj.CubeMapUint);

            selectedAreaIndex = 0;

            ImGui.Begin("Cubemap Uint");
            ImGui.Combo("Area", ref selectedAreaIndex, areas.ToArray(), areas.Count);
            ImGui.InputText("Name", ref name, 0x100);
            ImGui.Checkbox("Enable", ref enable);
            ImGui.InputFloat3("Position", ref position);
            ImGui.SliderFloat("Far", ref zFar, 1.0f, 1000000.0f);
            ImGui.SliderFloat("Near", ref zNear, 0.0001f, 1.0f);
            ImGui.InputFloat("Illuminant Dist", ref illuminant_dist);
            ImGui.InputInt("Num Gaussian Repetition", ref gaussian_repetition_num);
            ImGui.InputInt("Num Rendering Repetition", ref rendering_repetition_num);

          /*  var cubemap = AGraphicsLibrary.CubeMapGraphics.CubeMapTextureID;
            if (cubemap != null)
            {
                //Convert the cubemap to a usable 2D texture
                var id = EquirectangularRender.CreateTextureRender(
                    cubemap.ID, cubemap is GLTextureCubeArray,
                    0, 0, cubemap.Width * 4, cubemap.Height * 3);
                ImGui.Image((IntPtr)id, new Vector2(cubemap.Width * 4, cubemap.Height * 3));
            }*/

            ImGui.End();
        }

        void UpdateSelected(CubeMapUint cubemapUint)
        {
            enable = cubemapUint.Enable;
            name = cubemapUint.Name;
            zFar = cubemapUint.Far;
            zNear = cubemapUint.Near;
            illuminant_dist = cubemapUint.IlluminantDistance;
            gaussian_repetition_num = (int)cubemapUint.Gaussian_Repetition_Num;
            rendering_repetition_num = (int)cubemapUint.Rendering_Repetition_Num;
        }

        void PropertyChanged(GLContext context) {

        }
    }
}
