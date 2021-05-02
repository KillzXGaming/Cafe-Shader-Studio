using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using ImGuiNET;
using Toolbox.Core;
using OpenTK.Input;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CafeStudio.UI
{
    public class Outliner
    {
        public static IFileFormat ActiveFileFormat;

        public List<NodeBase> Nodes = new List<NodeBase>();
        public List<NodeBase> SelectedNodes = new List<NodeBase>();

        public NodeBase SelectedNode => SelectedNodes.LastOrDefault();

        static NodeBase dragDroppedNode;

        const float RENAME_DELAY_TIME = 0.5f;
        const bool RENAME_ENABLE = false;

        /// <summary>
        /// Gets the currently dragged/dropped node from the outliner.
        /// If a node is dropped onto a control, this is used to get the data.
        /// </summary>
        /// <returns></returns>
        public static NodeBase GetDragDropNode()
        {
            return dragDroppedNode;
        }

        //Rename handling
        private NodeBase renameNode;
        private bool isNameEditing = false;
        private string renameText;
        private double renameClickTime;

        //Search handling
        public bool ShowSearchBar = true;
        private bool isSearch = false;
        private string _searchText = "";

        public Outliner() {
        }

        public void Render()
        {
            //For loading files into the existing workspace.
            ImGui.Checkbox($"Load files to active outliner.", ref WorkspaceWindow.AddToActiveWorkspace);

            if (ShowSearchBar)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Search");
                ImGui.SameLine();

                var posX = ImGui.GetCursorPosX();
                var width = ImGui.GetWindowWidth();

                //Span across entire outliner width
                ImGui.PushItemWidth(width - posX);
                if (ImGui.InputText("##search_box", ref _searchText, 200))
                {
                    isSearch = !string.IsNullOrWhiteSpace(_searchText);
                }
                ImGui.PopItemWidth();
            }

            //Set the same header colors as hovered and active. This makes nav scrolling more seamless looking
            var active = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, active);
            ImGui.PushStyleColor(ImGuiCol.NavHighlight, new Vector4(0));

            if (ImGui.BeginChild("##tree_view1"))
            {
                foreach (var child in Nodes)
                    DrawNode(child);
                ImGui.EndChild();
            }

            ImGui.PopStyleColor(2);
        }

        public void DrawNode(NodeBase node)
        {
            bool HasText = node.Header != null &&
                 node.Header.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            char icon = IconManager.FOLDER_ICON;
            if (node.Children.Count == 0)
                icon = IconManager.FILE_ICON;
            if (node.Tag is STGenericMesh)
                icon = IconManager.MESH_ICON;
            if (node.Tag is STGenericModel)
                icon = IconManager.MODEL_ICON;

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None;
            flags |= ImGuiTreeNodeFlags.SpanFullWidth;

            if (node.Children.Count == 0 || isSearch)
                flags |= ImGuiTreeNodeFlags.Leaf;
            else
            {
                flags |= ImGuiTreeNodeFlags.OpenOnDoubleClick;
                flags |= ImGuiTreeNodeFlags.OpenOnArrow;
            }

            if (node.IsExpanded && !isSearch)
                flags |= ImGuiTreeNodeFlags.DefaultOpen;

            //Node was selected manually outside the outliner so update the list
            if (node.IsSelected && !SelectedNodes.Contains(node))
                SelectedNodes.Add(node);

            //Node was deselected manually outside the outliner so update the list
            if (!node.IsSelected && SelectedNodes.Contains(node))
                SelectedNodes.Remove(node);

            if (SelectedNodes.Contains(node))
                flags |= ImGuiTreeNodeFlags.Selected;

            if (isSearch && HasText || !isSearch)
            {
                //Add active file format styling. This determines what file to save.
                //For files inside archives, it gets the parent of the file format to save.
                bool isActiveFile = false;
                isActiveFile = ActiveFileFormat == node.Tag;

                bool isRenaming = node == renameNode && isNameEditing && node.Tag is IRenamableNode;

                //Improve tree node spacing.
                var spacing = ImGui.GetStyle().ItemSpacing;
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(spacing.X, 1));

                //Make the active file noticable
                if (isActiveFile)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.834f, 0.941f, 1.000f, 1.000f));

                //Align the text to improve selection sizing. 
                ImGui.AlignTextToFramePadding();

                //Disable selection view in renaming handler to make text more clear
                if (isRenaming)
                {
                    flags &= ~ImGuiTreeNodeFlags.Selected;
                    flags &= ~ImGuiTreeNodeFlags.SpanFullWidth;
                }

                //Load the expander or selection
                if (isSearch)
                    ImGui.Selectable(node.ID, flags.HasFlag(ImGuiTreeNodeFlags.Selected));
                else
                    node.IsExpanded = ImGui.TreeNodeEx(node.ID, flags, $"");

                ImGui.SameLine(); ImGuiHelper.IncrementCursorPosX(3);

                bool leftClicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
                bool rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
                bool nodeFocused = ImGui.IsItemFocused();
                bool isToggleOpened = ImGui.IsItemToggledOpen();
                bool hasContextMenu = node is IContextMenu || node is IExportReplaceNode || node.Tag is ICheckableNode ||
                    node.Tag is IContextMenu || node.Tag is IExportReplaceNode ||
                    node.Tag is STGenericTexture;

                //Apply a pop up menu for context items. Only do this if the menu has possible items used
                if (hasContextMenu && SelectedNodes.Contains(node))
                {
                    ImGui.PushID(node.Header);
                    if (ImGui.BeginPopupContextItem("##OUTLINER_POPUP", ImGuiPopupFlags.MouseButtonRight))
                    {
                        SetupRightClickMenu(node);
                        ImGui.EndPopup();
                    }
                    ImGui.PopID();
                }

                //Load the icon
                if (node.Tag is STGenericTexture) {
                    LoadTextureIcon(node);
                }
                else
                {
                    IconManager.DrawIcon(icon);
                    ImGui.SameLine(); ImGuiHelper.IncrementCursorPosX(3);
                }

                ImGui.AlignTextToFramePadding();

                //if (node.Tag is ICheckableNode)
                  //  ImGuiHelper.IncrementCursorPosY(-2);

                if (!isRenaming)
                    ImGui.Text(node.Header);
                else
                {
                    var renamable = node.Tag as IRenamableNode;

                    var bg = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];

                    //Make the textbox frame background blend with the tree background
                    //This is so we don't see the highlight color and can see text clearly
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, bg);
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);

                    var length = ImGui.CalcTextSize(renameText).X + 20;
                    ImGui.PushItemWidth(length);

                    if (ImGui.InputText("##RENAME_NODE", ref renameText, 512,
                        ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion |
                        ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.NoHorizontalScroll))
                    {
                        renamable.Renamed(renameText);
                        node.Header = renameText;

                        isNameEditing = false;
                    }
                    if (!ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        isNameEditing = false;
                    }

                    ImGui.PopItemWidth();
                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor(2);
                }
                ImGui.PopStyleVar();

                if (isActiveFile)
                    ImGui.PopStyleColor();

                if (!isRenaming)
                {
                    // Drag/drop tag types
                    if (node.Tag is IDragDropNode)
                    {
                        if (ImGui.BeginDragDropSource())
                        {
                            //Placeholder pointer data. Instead use drag/drop nodes from GetDragDropNode()
                            GCHandle handle1 = GCHandle.Alloc(node.ID);
                            ImGui.SetDragDropPayload("OUTLINER_ITEM", (IntPtr)handle1, sizeof(int), ImGuiCond.Once);
                            handle1.Free();

                            dragDroppedNode = node;

                            //Display icon for texture types
                            if (node.Tag is STGenericTexture)
                                LoadTextureIcon(node);

                            //Display text for item being dragged
                            ImGui.Button($"{node.Header}");
                            ImGui.EndDragDropSource();
                        }
                    }

                    //Check for rename selection on selected renamable node
                    if (node.IsSelected && node.Tag is IRenamableNode && RENAME_ENABLE)
                    {
                        bool renameStarting = renameClickTime != 0;
                        bool wasCancelled = false;

                        //Mouse click before editing started cancels the event
                        if (renameStarting && leftClicked)
                        {
                            renameClickTime = 0;
                            renameStarting = false;
                            wasCancelled = true;
                        }

                        //Check for delay
                        if (renameStarting)
                        {
                            //Create a delay between actions. This can be cancelled out during a mouse click
                            var diff = ImGui.GetTime() - renameClickTime;
                            if (diff > RENAME_DELAY_TIME)
                            {
                                //Name edit executed. Setup data for renaming.
                                isNameEditing = true;
                                renameNode = node;
                                renameText = ((IRenamableNode)node.Tag).GetRenameText();
                                //Reset the time
                                renameClickTime = 0;
                            }
                        }

                        //User has started a rename click. Start a time check
                        if (leftClicked && renameClickTime == 0 && !wasCancelled)
                        {
                            //Do a small delay for the rename event
                            renameClickTime = ImGui.GetTime();
                        }
                    }

                    //Click event executed on item
                    if ((leftClicked || rightClicked) && !isToggleOpened) //Prevent selection change on toggle
                    {
                        //Reset all selection unless shift/control held down
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                        {
                            foreach (var n in SelectedNodes)
                                n.IsSelected = false;
                            SelectedNodes.Clear();
                        }
                        //Add the clicked node to selection.
                        SelectedNodes.Add(node);
                        node.IsSelected = true;
                    }
                    else if (nodeFocused && !isToggleOpened && !node.IsSelected)
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                        {
                            foreach (var n in SelectedNodes)
                                n.IsSelected = false;
                            SelectedNodes.Clear();
                        }

                        //Add the clicked node to selection.
                        SelectedNodes.Add(node);
                        node.IsSelected = true;
                    }

                    if (leftClicked && node.IsSelected)
                    {
                        if (node is ArchiveHiearchy && node.Tag == null)
                        {
                            var archiveWrapper = (ArchiveHiearchy)node;
                            archiveWrapper.OpenFileFormat();
                            archiveWrapper.IsExpanded = true;
                        }
                    }

                    //Update the active file format when selected. (updates dockspace layout and file menus)
                    if (node.Tag is IFileFormat && node.IsSelected)
                    {
                        if (ActiveFileFormat != node.Tag)
                            ActiveFileFormat = (IFileFormat)node.Tag;
                    }
                    else if (node.IsSelected && node.Parent != null)
                    {
                    }
                }
            }

            if (isSearch)
            {
                foreach (var child in node.Children)
                    DrawNode(child);
            }
            else if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                    DrawNode(child);

                ImGui.TreePop();
            }
        }

        private void LoadTextureIcon(NodeBase node)
        {
            if (((STGenericTexture)node.Tag).RenderableTex == null)
                ((STGenericTexture)node.Tag).LoadRenderableTexture();

            //Render textures loaded in GL as an icon
            if (((STGenericTexture)node.Tag).RenderableTex != null)
            {
                var tex = ((STGenericTexture)node.Tag);
                //Turn the texture to a cached icon
                IconManager.LoadTexture(node.Header, tex);
                ImGui.SameLine();
            }
        }

        private void SetupRightClickMenu(NodeBase node)
        {
            if (node.Tag is ICheckableNode)
            {
                var checkable = (ICheckableNode)node.Tag;
                if (ImGui.Selectable("Enable"))
                {
                    checkable.OnChecked(true);
                }
                if (ImGui.Selectable("Disable"))
                {
                    checkable.OnChecked(false);
                }
            }
            if (node.Tag is IExportReplaceNode)
            {
                var exportReplaceable = (IExportReplaceNode)node.Tag;

                if (ImGui.Selectable("Export"))
                {
                    var dialog = new ImguiFileDialog();
                    dialog.FileName = node.Header;

                    dialog.SaveDialog = true;
                    foreach (var filter in exportReplaceable.ExportFilter)
                        dialog.AddFilter(filter);
                    if (dialog.ShowDialog($"{node.GetType()}export"))
                    {
                        exportReplaceable.Export(dialog.FilePath);
                    }
                }
                if (ImGui.Selectable("Replace"))
                {
                    var dialog = new ImguiFileDialog();
                    foreach (var filter in exportReplaceable.ReplaceFilter)
                        dialog.AddFilter(filter);
                    if (dialog.ShowDialog($"{node.GetType()}replace"))
                    {
                        exportReplaceable.Replace(dialog.FilePath);
                    }
                }
                ImGui.Separator();
            }
            if (node.Tag is IContextMenu)
            {
                var contextMenu = (IContextMenu)node.Tag;
                var menuItems = contextMenu.GetContextMenuItems();
                foreach (var item in menuItems)
                    LoadMenuItem(item);

                ImGui.Separator();
            }
        }

        private void LoadMenuItem(MenuItemModel item)
        {
            if (string.IsNullOrEmpty(item.Header)) {
                ImGui.Separator();
                return;
            }

            if (item.MenuItems.Count > 0)
            {
                bool menuItem = ImGui.MenuItem(item.Header, "", true);
                var hovered = ImGui.IsItemHovered();

                if (menuItem && hovered)
                {
                    foreach (var c in item.MenuItems)
                        LoadMenuItem(c);
                    ImGui.EndMenu();
                }
            }
            else
            {
                if (ImGui.Selectable(item.Header)) {
                    item.Command?.Execute(item);
                }
            }
        }

        private void DeselectAll(NodeBase node)
        {
            node.IsSelected = false;
            foreach (var c in node.Children)
                DeselectAll(c);
        }
    }
}
