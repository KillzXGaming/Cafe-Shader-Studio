using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace CafeStudio.UI
{
    public class UndoStack
    {
        public static List<UndoOperation> UndoObjects = new List<UndoOperation>();

        public static void Add(UndoOperation operation) {
            UndoObjects.Add(operation);
        }
    }

    public class UndoOperation
    {
        public object OriginalState;
        public object NewState;

        public virtual Action UndoAction { get; set; }
        public virtual Action RedoAction { get; set; }

        public string Name;

        public bool IsReverted = false;

        public void Redo()
        {
            RedoAction.Invoke();
            IsReverted = false;
        }

        public void Undo()
        {
            UndoAction.Invoke();
            IsReverted = true;
        }
    }

    public class UndoStringOperation : UndoOperation
    {
        PropertyInfo propertyInfo;
        object targetObj;

        public UndoStringOperation(string name, object obj, string property, string output)
        {
            propertyInfo = obj.GetType().GetProperty(property);
            targetObj = obj;

            Name = name;
            OriginalState = propertyInfo.GetValue(obj);
            NewState = output;
        }

        public override Action UndoAction => UndoString;
        public override Action RedoAction => RedoString;

        public void UndoString() {
            propertyInfo.SetValue(targetObj, OriginalState);
        }

        public void RedoString() {
            propertyInfo.SetValue(targetObj, NewState);
        }
    }
}
