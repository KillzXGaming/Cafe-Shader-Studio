using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using ImGuiNET;
using CafeStudio.UI;
using GLFrameworkEngine;

namespace CafeShaderStudio
{
    public class GlobalSettings
    {
        public CameraSettings Camera { get; set; } = new CameraSettings();
        public ViewerSettings Viewer { get; set; } = new ViewerSettings();
        public BackgroundSettings Background { get; set; } = new BackgroundSettings();
        public GridSettings Grid { get; set; } = new GridSettings();
        public BoneSettings Bones { get; set; } = new BoneSettings();

        private GLContext _context;

        public GlobalSettings() { }

        public GlobalSettings(GLContext context)
        {
            _context = context;
        }


        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static GlobalSettings Load(GLContext context)
        {
            if (!File.Exists("ConfigGlobal.json")) { new GlobalSettings(context).SaveDefaults(); }

            var config = JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText("ConfigGlobal.json"), new
                JsonSerializerSettings()
            {
                //If settings get added, don't alter the defaults
                NullValueHandling = NullValueHandling.Ignore,
            });
            config._context = context;

            config.Reload();
            return config;
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void SaveDefaults()
        {
            Save();
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void Save()
        {
            File.WriteAllText("ConfigGlobal.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            Reload();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void Reload()
        {
            _context.Camera.Mode = Camera.Mode;
            _context.Camera.IsOrthographic = Camera.IsOrthographic;
            _context.Camera.KeyMoveSpeed = Camera.KeyMoveSpeed;
            _context.Camera.PanSpeed = Camera.PanSpeed;
            _context.Camera.ZoomSpeed = Camera.ZoomSpeed;
            _context.Camera.ZNear = Camera.ZNear;
            _context.Camera.ZFar = Camera.ZFar;
            _context.Camera.FovDegrees = Camera.FovDegrees;

            _context.EnableFog = Viewer.DisplayFog;
            _context.EnableBloom = Viewer.DisplayBloom;

            DrawableBackground.Display = Background.Display;
            DrawableBackground.BackgroundTop = Background.TopColor;
            DrawableBackground.BackgroundBottom = Background.BottomColor;

            DrawableFloor.Display = Grid.Display;
            DrawableFloor.GridColor = Grid.Color;
            Toolbox.Core.Runtime.GridSettings.CellSize = Grid.CellSize;
            Toolbox.Core.Runtime.GridSettings.CellAmount = Grid.CellCount;

            Toolbox.Core.Runtime.DisplayBones = Bones.Display;
            Toolbox.Core.Runtime.BonePointSize = Bones.Size;
        }

        public void LoadDefaults()
        {
            Camera.Mode = _context.Camera.Mode;
            Camera.IsOrthographic = _context.Camera.IsOrthographic;
            Camera.KeyMoveSpeed = _context.Camera.KeyMoveSpeed;
            Camera.ZoomSpeed = _context.Camera.ZoomSpeed;
            Camera.PanSpeed = _context.Camera.PanSpeed;
            Camera.ZNear = _context.Camera.ZNear;
            Camera.ZFar = _context.Camera.ZFar;
            Camera.FovDegrees = _context.Camera.FovDegrees;

            Viewer.DisplayBloom = _context.EnableBloom;
            Viewer.DisplayFog = _context.EnableFog;

            Background.Display = DrawableBackground.Display;
            Background.TopColor = DrawableBackground.BackgroundTop;
            Background.BottomColor = DrawableBackground.BackgroundBottom;

            Grid.Display = DrawableFloor.Display;
            Grid.Color = DrawableFloor.GridColor;
            Grid.CellSize = Toolbox.Core.Runtime.GridSettings.CellSize;
            Grid.CellCount = Toolbox.Core.Runtime.GridSettings.CellAmount;

            Bones.Display = Toolbox.Core.Runtime.DisplayBones;
            Bones.Size = Toolbox.Core.Runtime.BonePointSize;
        }

        public class ViewerSettings
        {
            public bool DisplayFog { get; set; } = true;
            public bool DisplayBloom { get; set; } = false;
        }

        public class BackgroundSettings
        {
            public bool Display { get; set; } = true;
            public Vector3 TopColor { get; set; } = new Vector3(0.1f, 0.1f, 0.1f);
            public Vector3 BottomColor { get; set; } = new Vector3(0.2f, 0.2f, 0.2f);
        }

        public class GridSettings
        {
            public bool Display { get; set; } = true;
            public Vector4 Color { get; set; } = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            public int CellCount { get; set; } = 10;
            public float CellSize { get; set; } = 1;
        }

        public class BoneSettings
        {
            public bool Display { get; set; } = false;
            public float Size { get; set; } = 0.1f;
        }

        public class CameraSettings
        {
            public Camera.CameraMode Mode { get; set; } = GLFrameworkEngine.Camera.CameraMode.Inspect;

            public bool IsOrthographic { get; set; } = false;
            public float FovDegrees { get; set; } = 45;

            public float KeyMoveSpeed { get; set; } = 10.0f;
            public float PanSpeed { get; set; } = 1.0f;
            public float ZoomSpeed { get; set; } = 1.0f;

            public float ZNear { get; set; } = 1.0f;
            public float ZFar { get; set; } = 100000.0f;
        }
    }
}
