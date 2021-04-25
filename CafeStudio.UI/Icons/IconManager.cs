using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GLFrameworkEngine;
using ImGuiNET;

namespace CafeStudio.UI
{
    public class IconManager
    {
        static Dictionary<string, int> Icons = new Dictionary<string, int>();

        public const char MATERIAL_MASK_ICON = '\ue067';
        public const char MATERIAL_TRANSLUCENT_ICON = '\ue067';
        public const char MATERIAL_OPAQUE_ICON = '\ue067';
        public const char FOLDER_ICON = '\ue067';
        public const char FILE_ICON = '\ue061';
        public const char EYE_ON_ICON = '\ue05b';
        public const char EYE_OFF_ICON = '\ue05a';
        public const char MODEL_ICON = '\ue025';
        public const char MESH_ICON = '\ue00a';
        public const char X_ICON = '\ue00a';
        public const char Y_ICON = '\ue00a';
        public const char Z_ICON = '\ue00a';
        public const char W_ICON = '\ue00a';
        public const char ANIMATION_ICON = '\ue00a';

        public static int ICON_SIZE = 18;

        static bool init = false;

        static void InitTextures() {
            Icons.Add("SAVE_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.Save).ID);
            Icons.Add("CHECKERBOARD", GLTexture2D.FromBitmap(Properties.Resources.CheckerBackground).ID);
            Icons.Add("IMG_EDIT_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.Edit).ID);
            Icons.Add("IMG_ALPHA_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.AlphaIcon).ID);
            Icons.Add("IMG_NOALPHA_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.AlphaIconDisabled).ID);
            Icons.Add("TEXTURE", GLTexture2D.FromBitmap(Properties.Resources.Texture).ID);

            init = true;
        }

        public static void DrawIcon(char icon)
        {
            Vector4 color = new Vector4(1.0f);

            if (icon == FOLDER_ICON) color = new Vector4(0.921f, 0.78f, 0.376f, 1.0f);
            if (icon == MESH_ICON) color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            if (icon == MODEL_ICON) color = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(icon.ToString());
            ImGui.PopStyleColor();
        }

        public static int GetTextureIcon(string key)
        {
            if (!init)
                InitTextures();

            return Icons[key];
        }

        public static void PushFontScale(float scale)
        {
            ImGui.GetFont().Scale = scale;
            ImGui.PushFont(ImGui.GetFont());
        }

        public static void PopFontScale()
        {
            ImGui.GetFont().Scale = 1.0f;
            ImGui.PopFont();
        }

        public static void LoadTexture(string key, Toolbox.Core.STGenericTexture texture, int width, int height)
        {
            if (!Icons.ContainsKey(key)) {
                var iconID = IconRender.CreateTextureRender(texture, width, height);
                Icons.Add(key, iconID);
            }

            LoadImage(Icons[key], width, height);
        }

        public static void LoadTexture(string key, Toolbox.Core.STGenericTexture texture)
        {
            if (!Icons.ContainsKey(key))
            {
                var iconID = IconRender.CreateTextureRender(texture, ICON_SIZE, ICON_SIZE);
                if (iconID == -1) {
                    ImGui.Image((IntPtr)Icons["TEXTURE"], new System.Numerics.Vector2(ICON_SIZE, ICON_SIZE));
                    return;
                }

                Icons.Add(key, iconID);
            }

            LoadImage(Icons[key], ICON_SIZE, ICON_SIZE);
        }

        public static void LoadIcon(string key) {
            if (!init)
                InitTextures();

            if (Icons.ContainsKey(key))
                LoadImage(Icons[key], ICON_SIZE, ICON_SIZE);
        }

        static void LoadImage(int id, int width, int height) {
            ImGui.Image((IntPtr)id, new System.Numerics.Vector2(width, height));
        }
    }
}
