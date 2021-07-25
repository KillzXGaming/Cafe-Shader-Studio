using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Toolbox.Core;
using GLFrameworkEngine;
using AGraphicsLibrary;
using Toolbox.Core.IO;

namespace CafeStudio.UI
{
    public class WorkspaceWindow
    {
        public bool Opened = true;

        public static bool AddToActiveWorkspace = true;

        public static WorkspaceWindow ActiveWorkspace;

        UndoWindow UndoWindow = new UndoWindow();
        TimelineWindow TimelineWindow = new TimelineWindow();

        Viewport Viewport { get; set; }
        Outliner Outliner { get; set; }
        PropertyWindow PropertyWindow { get; set; }

        public unsafe ImGuiWindowClass* window_class;

        bool saveCompression = true;
        int yazoCompressionLevel = 3;

        public uint DockID;

        public bool UpdateDockspace = true;

        private WorkspaceLayout _layout = WorkspaceLayout.Default;
        private WorkspaceLayout Layout
        {
            get { return _layout; }
            set
            {
                if (_layout != value)
                {
                    _layout = value;
                    UpdateDockspace = true;
                }
            }
        }

        private IFileFormat _activeFileFormat;

        public IFileFormat ActiveFileFormat
        {
            get { return _activeFileFormat; }
            set
            { 
                _activeFileFormat = value;
                //Different file format types can have different dockspaces
                //Renderables will display viewport/timeline controls
                Layout = GetLayout();
            }
        }

        enum WorkspaceLayout
        {
            Default,  //No viewport. Outliner, property window, undo panel
            Viewport, //Viewport, timeline outliner, property window, undo panel
        }

        private WorkspaceLayout GetLayout()
        {
            if (ActiveFileCanRender())
                return WorkspaceLayout.Viewport;

            return WorkspaceLayout.Default;
        }

        public bool ActiveFileCanRender()
        {
            var render = ActiveFileFormat as IRenderableFile;
            if (render != null)
                return true;
            return false;
        }

        public string Name { get; set; }

        public void OnLoad() {
            Viewport = new Viewport(TimelineWindow);
            Outliner = new Outliner();
            PropertyWindow = new PropertyWindow();

            Viewport.OnLoad();
            TimelineWindow.OnLoad();
        }

        private bool init = false;
        public unsafe void InitWindowDocker(int index) {
            if (init)
                return;

            uint windowId = ImGui.GetID($"###window_{Name}{index}");

            ImGuiWindowClass windowClass = new ImGuiWindowClass();
            windowClass.ClassId = windowId;
            windowClass.DockingAllowUnclassed = 0;
            this.window_class = &windowClass;

            init = true;
        }

        public void ResetAnimationPlayer() {
            TimelineWindow.Reset();
        }

        bool debugWindow = false;
        bool showLightingEditor = false;

        public void ShowMenus()
        {
            if (ImGui.BeginMenu("Save Settings"))
            {
                var comp = ActiveFileFormat.FileInfo.Compression;
                var label = comp == null ? "None" : comp.ToString();

                if (ImGui.BeginCombo("Compression", label))
                {
                    if (ImGui.Selectable("None", comp == null)) {
                        ActiveFileFormat.FileInfo.Compression = null;
                    }

                    foreach (var format in FileManager.GetCompressionFormats())
                    {
                        bool isSelected = format == ActiveFileFormat.FileInfo.Compression;
                        if (ImGui.Selectable(format.ToString(), isSelected))
                        {
                            ActiveFileFormat.FileInfo.Compression = format;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
              
                if (comp != null) {
                    ImGui.Checkbox($"Compress with {comp}?", ref saveCompression);
                    ImGui.InputInt("Compression Level", ref yazoCompressionLevel, 1, 1);
                }

                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Debug"))
            {
                debugWindow = true;
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Lighting Editor"))
            {
                showLightingEditor = true;
                ImGui.EndMenu();
            }
            if (debugWindow)
            {
                if (ImGui.Begin("Pipeline View"))
                {
                    var size = ImGui.GetWindowSize();
                    var tex = Viewport.Pipeline._context.ColorPicker.GetDebugPickingDisplay();
                    if (tex != null)
                        ImGuiHelper.DisplayFramebufferImage(tex.ID, size);
                    ImGui.End();
                }
            }
        }

        public void Render(int workspaceID)
        {
            var windowFlags = ImGuiWindowFlags.NoCollapse;
            if (Layout == WorkspaceLayout.Viewport)
            {
                LoadWindow(GetWindowName("Viewport", workspaceID), windowFlags | ImGuiWindowFlags.MenuBar, Viewport.Render);
                LoadWindow(GetWindowName("Timeline", workspaceID), windowFlags, TimelineWindow.Render);
            }

            LoadWindow(GetWindowName("Properties", workspaceID), windowFlags, () => PropertyWindow.Render(Viewport.Pipeline, Outliner, TimelineWindow));
            LoadWindow(GetWindowName("Outliner", workspaceID), windowFlags, () => Outliner.Render());
            LoadWindow(GetWindowName("Undo History", workspaceID), windowFlags, () => UndoWindow.Render());
        }

        private void LoadWindow(string name, ImGuiWindowFlags windowFlags, Action action)
        {
            if (ImGui.Begin(name, windowFlags))
            {
                action.Invoke();
            }
            ImGui.End();
        }

        public unsafe uint SetupDockWindows(uint rootID, int workspaceID)
        {
            // This variable will track the document node, however we are not using it here as we aren't docking anything into it.
            uint dock_main_id = rootID;

            var dock_right = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Right, 0.2f, out uint nullL, out dock_main_id);
            var dock_left = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Left, 0.2f, out uint nullR, out dock_main_id);
            var dock_down_left = ImGui.DockBuilderSplitNode(dock_left, ImGuiDir.Down, 0.2f, out uint nullUL, out dock_left);
            var dock_down = ImGui.DockBuilderSplitNode(dock_main_id, ImGuiDir.Down, 0.3f, out uint nullU, out dock_main_id);

            if (Layout == WorkspaceLayout.Viewport)
            {
                ImGui.DockBuilderDockWindow(GetWindowName("Properties", workspaceID), dock_right);
                ImGui.DockBuilderDockWindow(GetWindowName("Outliner", workspaceID), dock_left);
                ImGui.DockBuilderDockWindow(GetWindowName("Undo History", workspaceID), dock_down_left);
                ImGui.DockBuilderDockWindow(GetWindowName("Viewport", workspaceID), dock_main_id);
                ImGui.DockBuilderDockWindow(GetWindowName("Timeline", workspaceID), dock_down);
            }
            else
            {
                ImGui.DockBuilderDockWindow(GetWindowName($"Properties", workspaceID), dock_main_id);
                ImGui.DockBuilderDockWindow(GetWindowName($"Outliner", workspaceID), dock_left);
                ImGui.DockBuilderDockWindow(GetWindowName($"Undo History", workspaceID), dock_down_left);
            }

            return dock_main_id;
        }

        private string GetWindowName(string name, int id)
        {
            return $"{name}##{name}_{id}";
        }

        public void LoadFile(string fileName)
        {
            GLContext.PreviewScale = 1.0f;

            var fileFormat = STFileLoader.OpenFileFormat(fileName);
            //Set filename as workspace tab name
            Name = System.IO.Path.GetFileName(fileName);
            ActiveFileFormat = fileFormat;

            var wrappers = ObjectWrapperFileLoader.OpenFormat(fileFormat);
            if (wrappers != null)
                Outliner.Nodes.Add(wrappers);

            if (fileFormat is IRenderableFile)
                AddDrawable(fileFormat);

            string dir = System.IO.Path.GetDirectoryName(fileName);
            TryLoadCourseDir(dir);
        }

        public void AddDrawable(GenericRenderer render)
        {
            Viewport.AddFile(render);

            if (!DataCache.ModelCache.ContainsKey(render.Name))
                DataCache.ModelCache.Add(render.Name, render);
        }

        public void AddDrawable(IFileFormat format)
        {
            var modelRender = format as IRenderableFile;
            modelRender.Renderer.ID = DataCache.ModelCache.Values.Count.ToString();
            DataCache.ModelCache.Add(modelRender.Renderer.ID.ToString(), modelRender.Renderer);
            Viewport.AddFile(modelRender);
        }

        private void TryLoadCourseDir(string folder)
        {
            if (System.IO.File.Exists($"{folder}\\course.bgenv"))
            {
                var archive = (IArchiveFile)STFileLoader.OpenFileFormat($"{folder}\\course.bgenv");

                LightingEngine lightingEngine = new LightingEngine();
                lightingEngine.LoadArchive(archive.Files.ToList());
                LightingEngine.LightSettings = lightingEngine;
                LightingEngine.LightSettings.UpdateColorCorrectionTable();

            }
            if (System.IO.File.Exists($"{folder}\\course_bglpbd.szs"))
            {
                //ProbeMapManager.Prepare(EveryFileExplorer.YAZ0.Decompress($"{dir}\\course_bglpbd.szs"));
                //  DataCache.ModelCache.Add(bfres.Renderer.Name, bfres.Renderer);
            }
        }

        public void Dispose()
        {

        }
    }
}
