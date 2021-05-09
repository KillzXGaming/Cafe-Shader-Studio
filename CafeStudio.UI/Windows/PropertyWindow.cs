using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using ImGuiNET;

namespace CafeStudio.UI
{
    public class PropertyWindow
    {
        public NodeBase ActiveNode = null;

        byte[] memPool = new byte[0];

        private object ActiveEditor = null;

        public void OnLoad()
        {

        }

        public void Render(Outliner outliner, TimelineWindow timeline)
        {
            //Display based on selected objects
            var node = outliner.SelectedNode;
            if (node == null)
                return;

            bool valueChanged = ActiveNode != node;
            if (valueChanged)
                ActiveNode = node;

            //Clear and reset all animations when an animation is clicked.
            if (valueChanged && node.Tag is Toolbox.Core.Animations.STAnimation)
                timeline.ResetAnimations();

            //Check for editor purpose node handling
            foreach (var n in outliner.SelectedNodes)
                CheckEditorSelectedNodes(timeline, n, valueChanged);

            //Archive files have multiple purpose editors (hex, tag properties, text previewer)
            if (node is ArchiveHiearchy)
                LoadArchiveProperties(node, valueChanged);
            else
                LoadProperties(node, valueChanged);
        }

        private void CheckEditorSelectedNodes(TimelineWindow timeline, NodeBase node, bool valueChanged)
        {
            //Check for active changes for functions that load only once on click
            if (valueChanged)
            {
                if (node.Tag != null && node.Tag is Toolbox.Core.Animations.STAnimation) {
                    var anim = (Toolbox.Core.Animations.STAnimation)node.Tag;
                    timeline.AddAnimation(anim, false);
                }
                //Container to batch play multiple animations at once
                if (node.Tag != null && node.Tag is IAnimationContainer) {
                    timeline.ClearAnimations();
                    foreach (var anim in ((IAnimationContainer)node.Tag).AnimationList)
                        timeline.AddAnimation(anim, false);
                }
            }

            //Keep track of the selected bone index for debug shading (view assigned bone weights)
            if (node.Tag is STBone) {
                Runtime.SelectedBoneIndex = ((STBone)node.Tag).Index;
            }
        }

        private void LoadArchiveProperties(NodeBase node, bool onLoad)
        {
            var archiveFileWrapper = node as ArchiveHiearchy;
            //Wrapper is a folder, skip
            if (!archiveFileWrapper.IsFile)
                return;

            if (ImGui.BeginChild("##editor_menu", new System.Numerics.Vector2(200, 22))) {
                if (ImGui.BeginCombo("Editor", archiveFileWrapper.ArchiveEditor)) {
                    if (ImGui.Selectable("Hex Preview")) { archiveFileWrapper.ArchiveEditor = "Hex Preview"; }
                    if (ImGui.Selectable("File Editor")) { archiveFileWrapper.ArchiveEditor = "File Editor"; }
                    if (ImGui.Selectable("Text Preview")) { archiveFileWrapper.ArchiveEditor = "Text Preview"; }

                    ImGui.EndCombo();
                }
            }
            ImGui.EndChild();

            if (archiveFileWrapper.ArchiveEditor == "Hex Preview") {
                if (ActiveEditor == null || ActiveEditor.GetType() != typeof(MemoryEditor))
                    ActiveEditor = new MemoryEditor();

                var data = archiveFileWrapper.ArchiveFileInfo.FileData;
                if (memPool.Length != data.Length)
                    memPool = data.ToArray();

                ((MemoryEditor)ActiveEditor).Draw(memPool, memPool.Length);
            }
            if (archiveFileWrapper.ArchiveEditor == "File Editor") {
                LoadProperties(node, onLoad);
            }
        }

        private void LoadProperties(NodeBase node, bool onLoad)
        {
            if (node.Tag is IPropertyUI)
            {
                //A UI type that can display rendered IMGUI code.
                var propertyUI = (IPropertyUI)node.Tag;
                if (ActiveEditor == null || ActiveEditor.GetType() != propertyUI.GetTypeUI()) {
                    var instance = Activator.CreateInstance(propertyUI.GetTypeUI());
                    ActiveEditor = instance;
                }
                if (onLoad)
                    propertyUI.OnLoadUI(ActiveEditor);

                propertyUI.OnRenderUI(ActiveEditor);
            }
            else if (node.Tag is STGenericTexture)
            {
                //A generic image viewer for image types.
                ImageEditor.LoadEditor((STGenericTexture)node.Tag);
            } //A basic UI type to generate properties like a property grid.
            else if (node.Tag is IPropertyDisplay)
            {
                var prop = ((IPropertyDisplay)node.Tag);
                if (prop.PropertyDisplay != null)
                    ImGuiHelper.LoadProperties(prop.PropertyDisplay);
            }
        }
    }
}
