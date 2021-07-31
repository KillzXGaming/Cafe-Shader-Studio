using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using AGraphicsLibrary;
using GLFrameworkEngine;

namespace BfresEditor
{
    public class ProbeDebugger
    {
        public static bool DEBUG_MODE = false;

        public static Vector3 Position = new Vector3(-1123.112f, 2249.845f, 1601.477f);

        public static bool Generated = false;

        public static bool ForceUpdate = false;

        public static float[] probeData;

        public static GLTexture DiffuseProbeTexture;

        public static void DrawWindow()
        {
            if (!ImGui.Begin("Probe Debugger"))
                return;

            if (Generated)
                ImGui.Text("Probe found in current location!");
            else
                ImGui.Text("No probes in current location!");

            bool edited = ImGui.DragFloat3("Position", ref Position);
            if (edited)
            {
                ProbeDebugDrawer.UpdateVisibleProbes(new OpenTK.Vector3(Position.X, Position.Y, Position.Z));
                ForceUpdate = true;
            }
            var cubemap = DiffuseProbeTexture;
            if (cubemap != null)
            {
                //Convert the cubemap to a usable 2D texture
                var tex = EquirectangularRender.CreateTextureRender(cubemap, 0, 1, true);
                ImGui.Image((IntPtr)tex.ID, new Vector2(512, 512 / 3));
            }

            if (Generated)
            {
                ImGui.Text($"SH0 {probeData[0]} {probeData[1]} {probeData[2]} {probeData[3]}");
                ImGui.Text($"SH1 {probeData[4]} {probeData[5]} {probeData[6]} {probeData[7]}");
                ImGui.Text($"SH2 {probeData[8]} {probeData[9]} {probeData[10]} {probeData[11]}");
                ImGui.Text($"SH3 {probeData[12]} {probeData[13]} {probeData[14]} {probeData[15]}");
                ImGui.Text($"SH4 {probeData[16]} {probeData[17]} {probeData[18]} {probeData[19]}");
                ImGui.Text($"SH5 {probeData[20]} {probeData[21]} {probeData[22]} {probeData[23]}");
                ImGui.Text($"SH6 {probeData[24]} {probeData[25]} {probeData[26]}");
            }

            ImGui.End();
        }
    }
}
