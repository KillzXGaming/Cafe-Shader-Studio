using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace CafeStudio.UI
{
    public class UndoWindow
    {
        private UndoOperation SelectedUndoOperation = null;
        private int currentStackCount;

        public void Render()
        {
            //Select last operation by default if the undo stack has changed
            if (UndoStack.UndoObjects.Count != currentStackCount)
            {
                SelectedUndoOperation = UndoStack.UndoObjects.LastOrDefault();
                currentStackCount = UndoStack.UndoObjects.Count;
            }

            if (ImGui.Button("Undo"))
            {
                if (SelectedUndoOperation != null)
                    SelectedUndoOperation.Undo();
            }
            ImGui.SameLine();
            if (ImGui.Button("Redo"))
            {
                if (SelectedUndoOperation != null)
                    SelectedUndoOperation.Redo();
            }

            foreach (var op in UndoStack.UndoObjects)
            {
                bool isSelected = SelectedUndoOperation == op;

                if (ImGui.Selectable(op.Name, isSelected))
                {
                    SelectedUndoOperation = op;
                }
            }
        }
    }
}
