using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Toolbox.Core;
using GLFrameworkEngine;
using System.Numerics;

namespace CafeStudio.UI
{
    public class ImageEditor
    {
        ImageEditorViewport ImageCanvas;

        public static ImageEditor imageEditor = new ImageEditor();

        public STGenericTexture ActiveTexture { get; set; }

        public static bool UseChannelComponents = true;

        public bool DisplayAlpha = true;

        string selectedBackground = "Checkerboard";

        int currentArrayLevel = 0;
        int currentMipLevel = 0;

        private void Init() {
            ImageCanvas = new ImageEditorViewport();
            ImageCanvas.OnLoad();
            ImageCanvas.Camera.Zoom = 150;
        }

        public static void LoadEditor(STGenericTexture texture) {
            imageEditor.Render(texture);
        }

        public void Render(STGenericTexture texture) {
            if (ImageCanvas == null)
                Init();

            var size = ImGui.GetWindowSize();

            ActiveTexture = texture;

            var menuSize = new Vector2(22, 22);
            var propertyWindowSize = new Vector2(size.X, size.Y / 2 - 20);
            var canvasWindowSize = new Vector2(size.X, size.Y / 2 - 20);

            if (ImGui.BeginChild("##IMAGE_TABMENU", propertyWindowSize, true)) {

                ImGui.BeginTabBar("image_menu");
                if (ImguiCustomWidgets.BeginTab("image_menu", "Properties")) {
                    ImGuiHelper.LoadProperties(ActiveTexture.DisplayProperties, ActiveTexture.DisplayPropertiesChanged);
                    ImGui.EndTabItem();
                }
                if (ImguiCustomWidgets.BeginTab("image_menu", "Channels"))
                {
                    ImGui.EndTabItem();
                }
                if (ImguiCustomWidgets.BeginTab("image_menu", "User Data"))
                {
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("CANVAS_WINDOW", canvasWindowSize, false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Edit"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("View"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Image"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Adjustments"))
                    {
                        ImGui.EndMenu();
                    }
                    ImGui.PushItemWidth(150);
                    if (ImGui.BeginCombo("##imageCB", selectedBackground))
                    {
                        if (ImGui.Selectable("Checkerboard")) { selectedBackground = "Checkerboard"; };
                        if (ImGui.Selectable("Black")) { selectedBackground = "Black"; };
                        if (ImGui.Selectable("White")) { selectedBackground = "White"; };
                        if (ImGui.Selectable("Custom")) 
                        {
                            selectedBackground = "White"; 
                        };

                        ImGui.EndMenu();
                    }
                    ImGui.PopItemWidth();

                    ImGui.EndMenuBar();
                }

                //Make icon buttons invisible aside from the icon itself.
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4());
                {
                    //Draw icon bar
                    ImGui.ImageButton((IntPtr)IconManager.GetTextureIcon("SAVE_BUTTON"), menuSize);
                    ImGui.SameLine();
                    ImGui.ImageButton((IntPtr)IconManager.GetTextureIcon("IMG_EDIT_BUTTON"), menuSize);
                    ImGui.SameLine();
                    ImguiCustomWidgets.ImageButtonToggle(
                        IconManager.GetTextureIcon("IMG_ALPHA_BUTTON"),
                        IconManager.GetTextureIcon("IMG_NOALPHA_BUTTON"), ref DisplayAlpha, menuSize);

                }
                ImGui.PopStyleColor();

                //Draw the array and mip level counter buttons
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Array Level " + $"{currentArrayLevel} / {texture.ArrayCount - 1}");
                ImGui.SameLine();
                if (ImGui.Button("<", menuSize)) { AdjustArrayLevel(-1); }
                ImGui.SameLine();
                if (ImGui.Button(">", menuSize)) { AdjustArrayLevel(1); }
                ImGui.SameLine();

                ImGui.Text("Mip Level " + $"{currentMipLevel} / {texture.MipCount - 1}");
                ImGui.SameLine();
                if (ImGui.Button("<", menuSize)) { AdjustMipLevel(-1); }
                ImGui.SameLine();
                if (ImGui.Button(">", menuSize)) { AdjustMipLevel(1); }

                //Draw the main image canvas
                DrawImageCanvas(canvasWindowSize);
            }
            ImGui.EndChild();

         /*   if (ImGui.BeginMenuBar())
            {

            }*/
        }

        private void DrawImageCanvas(Vector2 size)
        {
            ImageCanvas.DisplayAlpha = DisplayAlpha;
            ImageCanvas.ActiveTexture = ActiveTexture;
            ImageCanvas.Render((int)size.X, (int)size.Y);
        }

        private void AdjustArrayLevel(int increment)
        {
            if (increment < 0 && currentArrayLevel > 0)
                currentArrayLevel--;
            if (increment > 0 && currentArrayLevel < ActiveTexture.ArrayCount - 1)
                currentArrayLevel++;
        }

        private void AdjustMipLevel(int increment)
        {
            if (increment < 0 && currentMipLevel > 0)
                currentMipLevel--;
            if (increment > 0 && currentMipLevel < ActiveTexture.MipCount - 1)
                currentMipLevel++;
        }
    }
}
