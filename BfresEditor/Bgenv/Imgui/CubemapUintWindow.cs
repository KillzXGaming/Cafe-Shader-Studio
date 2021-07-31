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

namespace BfresEditor
{
    public class CubemapUintWindow 
    {
        public bool IsActive;

        int selectedAreaIndex = 0;
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
            var cubemapEnvParams = LightingEngine.LightSettings.Resources.CubeMapFiles.FirstOrDefault().Value;
            var cubemaps = cubemapEnvParams.CubeMapObjects;

            List<string> areas = new List<string>();
            foreach (var cmap in cubemaps)
                areas.Add(cmap.CubeMapUint.Name);

            var cubemapObj = cubemaps[selectedAreaIndex];
            UpdateSelected(cubemapObj.CubeMapUint);

            bool edited = false;
            edited |= ImGui.Combo("Area", ref selectedAreaIndex, areas.ToArray(), areas.Count);
            edited |= ImGui.InputText("Name", ref name, 0x100);
            edited |= ImGui.Checkbox("Enable", ref enable);
            edited |= ImGui.InputFloat3("Position", ref position);
            edited |= ImGui.SliderFloat("Far", ref zFar, 1.0f, 1000000.0f);
            edited |= ImGui.SliderFloat("Near", ref zNear, 0.0001f, 1.0f);
            edited |= ImGui.InputFloat("Illuminant Dist", ref illuminant_dist);
            edited |= ImGui.InputInt("Num Gaussian Repetition", ref gaussian_repetition_num);
            edited |= ImGui.InputInt("Num Rendering Repetition", ref rendering_repetition_num);
            if (edited)
                PropertyChanged(cubemapObj.CubeMapUint);

            var width = ImGui.GetWindowWidth();

            var cubemap = CubemapManager.CubeMapTexture;
            if (cubemap != null)
            {
                //Convert the cubemap to a usable 2D texture
                var tex = EquirectangularRender.CreateTextureRender(cubemap, 0, 0);
                ImGui.Image((IntPtr)tex.ID, new Vector2(width, width / 3));
            }
            else
                throw new Exception();
        }

        void UpdateSelected(CubeMapUint cubemapUint)
        {
            enable = cubemapUint.Enable;
            name = cubemapUint.Name;
            zFar = cubemapUint.Far;
            zNear = cubemapUint.Near;
            illuminant_dist = cubemapUint.IlluminantDistance;
            gaussian_repetition_num = cubemapUint.Gaussian_Repetition_Num;
            rendering_repetition_num = cubemapUint.Rendering_Repetition_Num;
        }

        void PropertyChanged(CubeMapUint cubemapUint) {
            cubemapUint.Enable = this.enable;
            cubemapUint.Name = this.name;
            cubemapUint.Far = this.zFar;
            cubemapUint.Near = this.zNear;
            cubemapUint.IlluminantDistance = this.illuminant_dist;
            cubemapUint.Gaussian_Repetition_Num = this.gaussian_repetition_num;
            cubemapUint.Rendering_Repetition_Num = this.rendering_repetition_num;
        }
    }
}
