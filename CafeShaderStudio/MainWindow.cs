using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using ImGuiNET;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using BfresEditor;
using GLFrameworkEngine;
using System.Numerics;
using OpenTK.Input;
using AGraphicsLibrary;
using Toolbox.Core;
using Toolbox.Core.IO;
using CafeStudio.UI;

namespace CafeShaderStudio
{
    public class MainWindow : GameWindow
    {
        ImGuiController _controller;
        Config _config;

        TimelineWindow TimelineWindow { get; set; }
        Outliner Outliner { get; set; }
        PropertyWindow PropertyWindow { get; set; }
        Pipeline Pipeline;
        string status = "";
        string selectedModel = "All Models";
        float status_delay = 0.5f;
        float status_start = 0.0f;

        bool initGlobalShaders = false;

        private List<string> recentFiles = new List<string>();
        private const int MaxRecentFileCount = 20;

        public MainWindow(GraphicsMode gMode) : base(1600, 900, gMode,
                                    "Cafe Shader Studio",
                                    GameWindowFlags.Default,
                                    DisplayDevice.Default,
                                    0, 0, GraphicsContextFlags.ForwardCompatible)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);

            _config = Config.Load();
            TimelineWindow = new TimelineWindow();
            Outliner = new Outliner();
            Pipeline = new Pipeline();
            PropertyWindow = new PropertyWindow();

            status = $"Loading global shaders...";
        }

        private void TryLoadPartInfo()
        {
            var actor = new RedStarLibrary.ActorBase();
            actor.LoadActor($"{RedStarLibrary.GlobalSettings.GamePath}\\ObjectData\\Mario.szs");
            LoadActorFile(actor);
            LoadAnimations(actor);
        }

        private void LoadAnimations(RedStarLibrary.ActorBase actor)
        {
            Dictionary<string, RedStarLibrary.AnimationSet> animationSets = new Dictionary<string, RedStarLibrary.AnimationSet>();
            LoadAnimationFile(actor, $"{RedStarLibrary.GlobalSettings.GamePath}\\ObjectData\\PlayerAnimation.szs", animationSets);
            foreach (var file in actor.PartActors.Values)
            {
                if (file.InitModel != null && file.InitModel.ExternalAnimationFile != null) {
                    LoadAnimationFile(file, $"{RedStarLibrary.GlobalSettings.GamePath}\\ObjectData\\{file.InitModel.ExternalAnimationFile}.szs", animationSets);
                }
            }

            BfresNodeBase node = new BfresNodeBase("AnimationList");
            foreach (var file in animationSets)
            {
                BfresNodeBase n = new BfresNodeBase(file.Key);
                n.Tag = file.Value;
                node.AddChild(n);
            }
            Outliner.Nodes.Add(node);
        }

        private void LoadAnimationFile(RedStarLibrary.ActorBase actor, string fileName, Dictionary<string, RedStarLibrary.AnimationSet> animationSets)
        {
            var animArchive = (IArchiveFile)STFileLoader.OpenFileFormat(fileName);
            foreach (var file in animArchive.Files)
            {
                if (file.FileName.EndsWith(".bfres"))
                {
                    var bfres = file.OpenFile() as BFRES;
                    foreach (var anim in bfres.SkeletalAnimations)
                    {
                        if (!animationSets.ContainsKey(anim.Name))
                            animationSets.Add(anim.Name, new RedStarLibrary.AnimationSet());

                        var model = actor.ModelFile.Renderer.Models[0] as BfresModelAsset;
                        anim.SkeletonOverride = model.ModelData.Skeleton;

                        animationSets[anim.Name].Animations.Add(anim);
                    }
                    foreach (var anim in bfres.VisibilityAnimations)
                    {
                        if (!animationSets.ContainsKey(anim.Name))
                            animationSets.Add(anim.Name, new RedStarLibrary.AnimationSet());

                        animationSets[anim.Name].Animations.Add(anim);
                    }
                    foreach (var anim in bfres.MaterialAnimations)
                    {
                        if (!animationSets.ContainsKey(anim.Name))
                            animationSets.Add(anim.Name, new RedStarLibrary.AnimationSet());

                        animationSets[anim.Name].Animations.Add(anim);
                    }
                }
            }
        }

        private void LoadActorFile(RedStarLibrary.ActorBase actor)
        {
            AddDrawable(actor.ModelFile as IFileFormat);

            Outliner.ActiveFileFormat = actor.ModelFile as IFileFormat;

            actor.InitModelFile();
            actor.InitActorPartList();

            if (actor.TextureArchive != null)
                AddDrawable(actor.TextureArchive as IFileFormat);

            foreach (var part in actor.PartActors.Values)
                LoadActorFile(part);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _controller = new ImGuiController(Width, Height);

            //Set the current theme instance
            ColorTheme.UpdateTheme(new DarkTheme());

            //Disable the docking buttons
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;

            //Enable docking support
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            //Enable up/down key navigation
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            //Only move via the title bar instead of the whole window
            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

            //Init rendering data
            TimelineWindow.OnLoad();
            Pipeline.InitBuffers();

            InitDock();
            LoadRecentList();

            RenderTools.Init();

            ReloadGlobalShaders();

            var Thread2 = new Thread((ThreadStart)(() =>
            {
                //Init plugins
                Toolbox.Core.FileManager.GetFileFormats();
            }));
            Thread2.Start();
        }

        private void ReloadGlobalShaders()
        {
            var Thread = new Thread((ThreadStart)(() =>
            {
                if (!Directory.Exists("GlobalShaders"))
                    Directory.CreateDirectory("GlobalShaders");

                foreach (var file in Directory.GetFiles("GlobalShaders"))
                {
                    if (!GlobalShaderCache.ShaderFiles.ContainsKey(file))
                        GlobalShaderCache.ShaderFiles.Add(file, new BfshaLibrary.BfshaFile(file));
                }

                initGlobalShaders = true;
                status = $"";
            }));
            Thread.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Tell ImGui of the new size
            _controller.WindowResized(Width, Height);
        }

        public void LoadFileFormat(string fileName)
        {
            if (!initGlobalShaders) 
                return;

            if (fileName.EndsWith(".byaml"))
            {
                MapLoader.LoadMuunt(fileName);

                foreach (var render in MapLoader.Renders)
                    AddDrawable(render);
                return;
            }

            if (!WorkspaceWindow.AddToActiveWorkspace)
                ClearWorkspace();

            string name = System.IO.Path.GetFileName(fileName);
            status = $"Loading file {name}";

            var fileFormat = STFileLoader.OpenFileFormat(fileName);
            if (fileFormat == null)
            {
                status_start = (float)ImGui.GetTime();
                status_delay = 3.0f;
                status = $"File Format not supported for {name}.";
                return;
            }

            if (!recentFiles.Contains(fileName))
                SaveRecentFile(fileName);

            Outliner.ActiveFileFormat = fileFormat;

            if (fileFormat is IRenderableFile)
                AddDrawable(fileFormat);
            if (fileFormat is IArchiveFile)
            {
                foreach (var file in (((IArchiveFile)fileFormat).Files))
                {
                    if (file.FileName.EndsWith(".bfres"))
                    {
                        var bfres = file.OpenFile();
                        AddDrawable(bfres);
                    }
                }
            }

            string dir = System.IO.Path.GetDirectoryName(fileName);
            TryLoadCourseDir(dir);

            status = "";
        }

        public void AddDrawable(IFileFormat format)
        {
            var wrappers = ObjectWrapperFileLoader.OpenFormat(format);
            if (wrappers != null)
                Outliner.Nodes.Add(wrappers);

            var modelRender = format as IRenderableFile;
            modelRender.Renderer.ID = DataCache.ModelCache.Values.Count.ToString();
            DataCache.ModelCache.Add(modelRender.Renderer.ID.ToString(), modelRender.Renderer);
            Pipeline.AddFile(modelRender);
        }

        public void AddDrawable(GenericRenderer render)
        {
            Pipeline.AddFile(render);

            if (!DataCache.ModelCache.ContainsKey(render.Name))
                DataCache.ModelCache.Add(render.Name, render);
        }

        private void TryLoadCourseDir(string folder)
        {
            if (System.IO.File.Exists($"{folder}\\course_muunt.byaml"))
            {
                MapLoader.LoadSkybox($"{folder}\\course_muunt.byaml");

                foreach (var render in MapLoader.Renders)
                    AddDrawable(render);
            }
            if (System.IO.File.Exists($"{folder}\\course.bgenv"))
            {
                var archive = (IArchiveFile)STFileLoader.OpenFileFormat($"{folder}\\course.bgenv");

                LightingEngine lightingEngine = new LightingEngine();
                lightingEngine.LoadArchive(archive.Files.ToList());
                LightingEngine.LightSettings = lightingEngine;
                LightingEngine.LightSettings.UpdateColorCorrectionTable();

                //Create a list of models that can render onto cubemaps
                List<GenericRenderer> cubemapRenderModels = new List<GenericRenderer>();
                foreach (BfresRender model in Pipeline.SceneObjects)
                    if (model.IsSkybox) //Only load skybox (VR) map objects. Todo this should be improved.
                        cubemapRenderModels.Add(model);

                //Load the main models (which in this case would be the course model)
                foreach (var model in Pipeline.Files)
                    cubemapRenderModels.Add(model.Renderer);

                //Generate cubemaps in the scene.
                LightingEngine.LightSettings.UpdateCubemap(cubemapRenderModels);
                //Generate light maps (area based lighting from directional and hemi lighting)
                for (int i = 0; i < 8; i++)
                    LightingEngine.LightSettings.UpdateLightmap(Pipeline._context, i);
            }
            if (System.IO.File.Exists($"{folder}\\course_bglpbd.szs"))
            {
                //Todo handle probe lighting (they alter lightmaps for map objects)
                //ProbeMapManager.Prepare(EveryFileExplorer.YAZ0.Decompress($"{dir}\\course_bglpbd.szs"));
                //  DataCache.ModelCache.Add(bfres.Renderer.Name, bfres.Renderer)   ;
            }
        }

        float font_scale = 1.0f;
        bool fullscreen = true;
        bool p_open = true;
        ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;

        private uint dock_id;
        private unsafe ImGuiWindowClass* window_class;

        private unsafe void InitDock()
        {
            uint windowId = ImGui.GetID($"###window_main");

            var nativeConfig = ImGuiNative.ImGuiWindowClass_ImGuiWindowClass();
            (*nativeConfig).ClassId = windowId;
            (*nativeConfig).DockingAllowUnclassed = 0;
            this.window_class = nativeConfig;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _controller.Update(this, (float)e.Time);

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDocking;

            if (fullscreen)
            {
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGui.SetNextWindowViewport(viewport.ID);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
                window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            }

            if ((dockspace_flags & ImGuiDockNodeFlags.PassthruCentralNode) != 0)
                window_flags |= ImGuiWindowFlags.NoBackground;

            //Set the adjustable global font scale
            ImGui.GetIO().FontGlobalScale = font_scale;

            if (status_start > 0)
            {
                var dif = ImGui.GetTime() - status_start;
                if (dif > status_delay)
                {
                    status = "";
                    status_start = 0.0f;
                }
            }

            ImGui.Begin("WindowSpace", ref p_open, window_flags);
            ImGui.PopStyleVar(2);

            dock_id = ImGui.GetID("##DockspaceRoot");

            LoadFileMenu();
            LoadWorkspaces();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Width, Height);

            _controller.Render();

            SwapBuffers();
        }

        private unsafe void LoadWorkspaces()
        {
            uint dockspaceId = ImGui.GetID($"###workspace");
            int workspaceID = 0;
            var windowFlags = ImGuiWindowFlags.NoCollapse;

            if (ImGui.DockBuilderGetNode(dock_id).NativePtr == null) {
                ReloadDockLayout(dock_id, workspaceID);
            }

            //Create an inital dock space for docking workspaces.
            ImGui.DockSpace(dock_id, new System.Numerics.Vector2(0.0f, 0.0f), 0, window_class);

            LoadWindow(GetWindowName("Viewport", workspaceID), windowFlags | ImGuiWindowFlags.MenuBar, ViewportRender);
            LoadWindow(GetWindowName("Timeline", workspaceID), windowFlags, TimelineWindow.Render);
            LoadWindow(GetWindowName("Outliner", workspaceID), windowFlags, () => Outliner.Render());
            LoadWindow(GetWindowName("Properties", workspaceID), windowFlags, () => PropertyWindow.Render(Outliner, TimelineWindow));
        }

        private void ViewportRender()
        {
            if (ImGui.BeginMenuBar())
            {
                DrawViewportMenu();
                ImGui.EndMenuBar();
            }

            var pos = ImGui.GetCursorPos();

            if (ImGui.BeginChild("viewport_child1"))
            {
                DrawViewport();

                ImGui.SetCursorPos(new System.Numerics.Vector2(pos.X + 20, pos.Y - 40));
                ImGui.Text($"Projection: {(Pipeline._context.Camera.IsOrthographic ? "Orthographic" : "Perspective")}");

                ImGui.SetCursorPosX(pos.X + 20);
                ImGui.Text($"Direction: {Pipeline._context.Camera.Direction}");

                ImGui.SetCursorPosX(pos.X + 20);
                ImGui.Text($"Mode: {Pipeline._context.Camera.Mode}");
            }
            ImGui.EndChild();
        }

        private void LoadWindow(string name, ImGuiWindowFlags windowFlags, Action action)
        {
            if (ImGui.Begin(name, windowFlags))
            {
                action.Invoke();
            }
            ImGui.End();
        }

        private void ReloadDockLayout( uint dockspaceId, int workspaceID)
        {
            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;

            ImGui.DockBuilderRemoveNode(dockspaceId); // Clear out existing layout
            ImGui.DockBuilderAddNode(dockspaceId, dockspace_flags); // Add empty node

            // This variable will track the document node, however we are not using it here as we aren't docking anything into it.
            uint dock_main_id = dockspaceId;

            var dock_right = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Right, 0.2f, out uint nullL, out dock_main_id);
            var dock_left = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Left, 0.2f, out uint nullR, out dock_main_id);
            var dock_down_left = ImGui.DockBuilderSplitNode(dock_left, ImGuiDir.Down, 0.2f, out uint nullUL, out dock_left);
            var dock_down = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Down, 0.3f, out uint nullU, out dock_main_id);

            ImGui.DockBuilderDockWindow(GetWindowName("Properties", workspaceID), dock_right);
            ImGui.DockBuilderDockWindow(GetWindowName("Outliner", workspaceID), dock_left);
            ImGui.DockBuilderDockWindow(GetWindowName("Viewport", workspaceID), dock_main_id);
            ImGui.DockBuilderDockWindow(GetWindowName("Timeline", workspaceID), dock_down);

            ImGui.DockBuilderFinish(dockspaceId);
        }

        private string GetWindowName(string name, int id)
        {
            return $"{name}##{name}_{id}";
        }

        private void DrawViewportMenu()
        {
            if (ImGui.BeginMenu("View Setting"))
            {
                if (ImGui.BeginMenu("Background"))
                {
                    ImGui.Checkbox("Display", ref DrawableBackground.Display);
                    ImGui.ColorEdit3("Color Top", ref DrawableBackground.BackgroundTop);
                    ImGui.ColorEdit3("Color Bottom", ref DrawableBackground.BackgroundBottom);
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Grid"))
                {
                    ImGui.Checkbox("Display", ref DrawableFloor.Display);
                    ImGui.ColorEdit4("Grid Color", ref DrawableFloor.GridColor);
                    ImGui.InputInt("Grid Cell Count", ref Toolbox.Core.Runtime.GridSettings.CellAmount);
                    ImGui.InputFloat("Grid Cell Size", ref Toolbox.Core.Runtime.GridSettings.CellSize);
                    ImGui.EndMenu();
                }

                ImGui.Checkbox("Wireframe", ref Toolbox.Core.Runtime.RenderSettings.Wireframe);
                ImGui.Checkbox("WireframeOverlay", ref Toolbox.Core.Runtime.RenderSettings.WireframeOverlay);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Camera"))
            {
                if (ImGui.Button("Reset Transform"))
                {
                    Pipeline._context.Camera.Translation = new OpenTK.Vector3(0, 1, -5);
                    Pipeline._context.Camera.RotationX = 0;
                    Pipeline._context.Camera.RotationY = 0;
                    Pipeline._context.Camera.UpdateTransform();
                }

                ImGuiHelper.ComboFromEnum<Camera.FaceDirection>("Direction", Pipeline._context.Camera, "Direction");
                ImGuiHelper.ComboFromEnum<Camera.CameraMode>("Mode", Pipeline._context.Camera, "Mode");

                ImGuiHelper.InputFromBoolean("Orthographic", Pipeline._context.Camera, "IsOrthographic");
                ImGuiHelper.InputFromBoolean("Lock Rotation", Pipeline._context.Camera, "LockRotation");

                ImGuiHelper.InputFromFloat("Fov (Degrees)", Pipeline._context.Camera, "FovDegrees", true, 1f);
                if (Pipeline._context.Camera.FovDegrees != 45)
                {
                    ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.FovDegrees = 45; }
                }

                ImGuiHelper.InputFromFloat("ZFar", Pipeline._context.Camera, "ZFar", true, 1f);
                if (Pipeline._context.Camera.ZFar != 100000.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZFar = 100000.0f; }
                }

                ImGuiHelper.InputFromFloat("ZNear", Pipeline._context.Camera, "ZNear", true, 0.1f);
                if (Pipeline._context.Camera.ZNear != 0.1f)
                {
                    ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZNear = 0.1f; }
                }

                ImGuiHelper.InputFromFloat("Zoom Speed", Pipeline._context.Camera, "ZoomSpeed", true, 0.1f);
                if (Pipeline._context.Camera.ZoomSpeed != 1.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.ZoomSpeed = 1.0f; }
                }

                ImGuiHelper.InputFromFloat("Pan Speed", Pipeline._context.Camera, "PanSpeed", true, 0.1f);
                if (Pipeline._context.Camera.PanSpeed != 1.0f)
                {
                    ImGui.SameLine(); if (ImGui.Button("Reset")) { Pipeline._context.Camera.PanSpeed = 1.0f; }
                }

                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Reset Animations"))
            {
                TimelineWindow.Reset();
                ImGui.EndMenu();
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("Active Model(s)");
            ImGui.SameLine();

            ImGui.PushItemWidth(250);
            if (ImGui.BeginCombo("##model_select", selectedModel))
            {
                bool isSelected = "All Models" == selectedModel;
                if (ImGui.Selectable("All Models", isSelected)) {
                    selectedModel = "All Models";
                    ToggleModel();
                }
                if (isSelected)
                    ImGui.SetItemDefaultFocus();

                foreach (var file in Pipeline.Files)
                {
                    foreach (var model in file.Renderer.Models)
                    {
                        string name = $"{file.Renderer.Name}.{model.Name}";
                        isSelected = name == selectedModel;

                        if (ImGui.Selectable(name, isSelected))
                        {
                            selectedModel = name;
                            ToggleModel();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
        }

        private void ToggleModel()
        {
            foreach (var file in Pipeline.Files)
            {
                foreach (var model in file.Renderer.Models)
                {
                    string name = $"{file.Renderer.Name}.{model.Name}";

                    if (selectedModel == "All Models")
                        model.IsVisible = true;
                    else if (name == selectedModel)
                        model.IsVisible = true;
                    else
                        model.IsVisible = false;
                }
            }
        }

        private void DrawViewport()
        {
            var size = ImGui.GetWindowSize();
            if (Pipeline.Width != (int)size.X || Pipeline.Height != (int)size.Y)
            {
                Pipeline.Width = (int)size.X;
                Pipeline.Height = (int)size.Y;
                Pipeline.OnResize();
            }

            Pipeline.RenderScene();

            if (ImGui.IsWindowFocused() && ImGui.IsWindowHovered() || _mouseDown)
            {
                if (!onEnter)
                {
                    Pipeline.ResetPrevious();
                    onEnter = true;
                }

                //Only update scene when necessary
                if (ImGuiController.ApplicationHasFocus)
                    UpdateCamera();
            }
            else
            {
                onEnter = false;
            }

            var id = Pipeline.GetViewportTexture();
            ImGui.Image((IntPtr)id, size, new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));
        }

        bool showStyleEditor;
        bool showAboutPage;
        bool showLightingEditor;
        BfresEditor.LightingEditor lightingEditor;

        private void LoadFileMenu()
        {
            float framerate = ImGui.GetIO().Framerate;

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open", "Ctrl+O", false, initGlobalShaders))
                    {
                        OpenFileWithDialog();
                    }
                    if (ImGui.BeginMenu("Recent"))
                    {
                        for (int i = 0; i < recentFiles.Count; i++)
                        {
                            if (ImGui.Selectable(recentFiles[i]))
                            {
                                LoadFileFormat(recentFiles[i]);
                            }
                        }
                        ImGui.EndMenu();
                    }

                    var canSave = Outliner.ActiveFileFormat != null && Outliner.ActiveFileFormat.CanSave;
                    if (ImGui.MenuItem("Save", "Ctrl+S", false, canSave))
                    {
                        SaveFileWithCurrentPath();
                    }
                    if (ImGui.MenuItem("Save As", "Ctrl+Shift+S", false, canSave))
                    {
                        SaveFileWithDialog();
                    }

                    if (ImGui.MenuItem("Clear Workspace"))
                    {
                        ClearWorkspace();
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Setting"))
                {
                    ImGui.InputFloat("Font Size", ref font_scale, 0.1f);

                    _config.RenderUI();

                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Style Editor"))
                {
                    showStyleEditor = true;
                }

                if (_config.HasValidSMOPath)
                {
                    if (ImGui.MenuItem("Mario Viewer", initGlobalShaders)) {
                        TryLoadPartInfo();
                    }
                }

                /*  if (ImGui.MenuItem("Lighting"))
                  {
                      showLightingEditor = true;
                  }*/

                if (ImGui.BeginMenu("Help"))
                {
                    if (ImGui.Selectable($"About")) {
                        showAboutPage = true;
                    }
                    if (ImGui.Selectable($"Donate")) {
                        BrowserHelper.OpenDonation();
                    }
                    ImGui.EndMenu();
                }

                float size = ImGui.GetWindowWidth();
                if (!string.IsNullOrEmpty(status)){

                    string statusLabel = $"Status: {status}";
                    float lbSize = ImGui.CalcTextSize(statusLabel).X;

                    ImGui.SetCursorPosX((size - (lbSize)) / 2);
                    ImGui.Text($"Status: {status}");
                }

                ImGui.SetCursorPosX(size - 100);
                ImGui.Text($"({framerate:0.#} FPS)");
                ImGui.EndMainMenuBar();
            }

            if (showStyleEditor)
            {
                if (ImGui.Begin("Style Editor", ref showStyleEditor))
                {
                    ImGui.ShowStyleEditor();
                    ImGui.End();
                }
            }
            if (showLightingEditor)
            {
                if (lightingEditor == null) lightingEditor = new BfresEditor.LightingEditor();

                lightingEditor.Render(this.Pipeline._context);
            }
            if (showAboutPage)
            {
                if (ImGui.Begin($"About", ref showAboutPage))
                {
                    if (ImGui.CollapsingHeader($"Credits", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.BulletText("KillzXGaming - main developer");
                        ImGui.BulletText("JuPaHe64 - created animation timeline");
                        ImGui.BulletText("Ryujinx - for shader libraries used to decompile and translate switch binaries into glsl code.");
                        ImGui.BulletText("OpenTK Team - for opengl c# bindings.");
                        ImGui.BulletText("mellinoe and IMGUI Team - for c# port and creating the IMGUI library");
                        ImGui.BulletText("Syroot - for bfres library and binary IO");
                    }
                    ImGui.End();
                }
            }
        }

        private void SaveFileWithCurrentPath()
        {
            var fileFormat = Outliner.ActiveFileFormat;
            if (fileFormat.FileInfo.ParentArchive != null)
                fileFormat = fileFormat.FileInfo.ParentArchive as IFileFormat;

            string path = fileFormat.FileInfo.FilePath;
            SaveFileFormat(fileFormat, path);
        }

        private void SaveFileWithDialog()
        {
            var fileFormat = Outliner.ActiveFileFormat;
            if (fileFormat.FileInfo.ParentArchive != null)
                fileFormat = fileFormat.FileInfo.ParentArchive as IFileFormat;

            ImguiFileDialog sfd = new ImguiFileDialog();
            sfd.SaveDialog = true;
            foreach (var extension in fileFormat.Extension)
                sfd.AddFilter(extension, "");

            foreach (var format in FileManager.GetCompressionFormats())
            {
                foreach (var extension in format.Extension)
                    sfd.AddFilter(extension, "");
            }

            sfd.FileName = fileFormat.FileInfo.FileName;

            if (sfd.ShowDialog("SAVE_FILE")) {
                SaveFileFormat(fileFormat, sfd.FilePath);
            }
        }

        private void SaveFileFormat(IFileFormat fileFormat, string path)
        {
            var log = STFileSaver.SaveFileFormat(fileFormat, path);

            string compInfo = "";
            if (fileFormat.FileInfo.Compression != null)
            {
                string compType = fileFormat.FileInfo.Compression.ToString();
                compInfo = $"Compressed with {compType}.";
            }

            TinyFileDialog.MessageBoxInfoOk($"File {path} has been saved! { log.SaveTime}. {compInfo}");
        }

        private void OpenFileWithDialog()
        {
            ImguiFileDialog ofd = new ImguiFileDialog();
            if (ofd.ShowDialog("OPEN_FILE"))
            {
                foreach (var file in ofd.FilePaths)
                    LoadFileFormat(file);
            }
        }

        private void ClearWorkspace()
        {
            foreach (var render in DataCache.ModelCache.Values)
                render.Dispose();

            foreach (var render in Pipeline.SceneObjects)
                render.Dispose();

            TimelineWindow.Reset();
            Outliner.ActiveFileFormat = null;
            TimelineWindow.Reset();
            Outliner.Nodes.Clear();
            Outliner.SelectedNodes.Clear();
            Pipeline.Files.Clear();
            Pipeline.SceneObjects.Clear();
            Pipeline._context.Scene.PickableObjects.Clear();
            DataCache.ModelCache.Clear();

            GC.Collect();
        }

        private bool onEnter = false;
        private bool _mouseDown = false;

        private void UpdateCamera()
        {
            var mouseInfo = CreateMouseState();

            if (ImGui.IsAnyMouseDown() && !_mouseDown)
            {
                Pipeline.OnMouseDown(mouseInfo);
                _mouseDown = true;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Right) ||
               ImGui.IsMouseReleased(ImGuiMouseButton.Middle))
            {
                Pipeline.OnMouseUp(mouseInfo);
                _mouseDown = false;
            }

            if (_mouseDown)
                Pipeline.OnMouseMove(mouseInfo);

            Pipeline.OnMouseWheel(mouseInfo);
            //   Pipeline._context.Camera.Controller.KeyPress();
        }

        private MouseEventInfo CreateMouseState()
        {
            var mouseInfo = new MouseEventInfo();

            //Prepare info
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                mouseInfo.RightButton = ButtonState.Pressed;
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                mouseInfo.LeftButton = ButtonState.Pressed;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                mouseInfo.RightButton = ButtonState.Released;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                mouseInfo.LeftButton = ButtonState.Released;

            MouseState mouseState = Mouse.GetState();
            mouseInfo.WheelPrecise = mouseState.WheelPrecise;

            //Construct relative position
            var windowPos = ImGui.GetWindowPos();

            var pos = ImGui.GetIO().MousePos;
            pos = new System.Numerics.Vector2(pos.X - windowPos.X, pos.Y - windowPos.Y);

            if (ImGui.IsMousePosValid())
                mouseInfo.Position = new System.Drawing.Point((int)pos.X, (int)pos.Y);
            else
                mouseInfo.HasValue = false;

            return mouseInfo;
        }

        protected override void OnFileDrop(FileDropEventArgs e) {
            LoadFileFormat(e.FileName);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            _controller.PressChar(e.KeyChar);
        }

        private void SaveRecentFile(string path)
        {
            LoadRecentList(); //load list from file
            if (!(recentFiles.Contains(path))) //prevent duplication on recent list
                recentFiles.Insert(0, path); //insert given path into list

            //keep list number not exceeded the given value
            while (recentFiles.Count > MaxRecentFileCount)
            {
                recentFiles.RemoveAt(MaxRecentFileCount);
            }

            //writing menu list to file
            //create file called "Recent.txt" located on app folder
            StreamWriter stringToWrite =
            new StreamWriter(Runtime.ExecutableDir + "\\Recent.txt");
            foreach (string item in recentFiles)
            {
                stringToWrite.WriteLine(item); //write list to stream
            }
            stringToWrite.Flush(); //write stream to file
            stringToWrite.Close(); //close the stream and reclaim memory
        }

        public void LoadRecentList()
        {
            recentFiles.Clear();

            if (File.Exists(Runtime.ExecutableDir + "\\Recent.txt"))
            {
                StreamReader listToRead = new StreamReader(Runtime.ExecutableDir + "\\Recent.txt"); //read file stream
                string line;
                while ((line = listToRead.ReadLine()) != null) //read each line until end of file
                {
                    if (File.Exists(line))
                        recentFiles.Add(line); //insert to list
                }
                listToRead.Close(); //close the stream
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
