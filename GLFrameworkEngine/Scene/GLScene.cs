using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    public class GLScene
    {
        public ITransformAction ActiveAction = null;

        public List<IPickable> PickableObjects = new List<IPickable>();

        public List<IPickable> GetSelected() {
            return PickableObjects.Where(x => x.IsSelected).ToList();
        }

        public void SetTransformAction(GLTransform transform, TransformActions action)
        {
            switch (action)
            {
                case TransformActions.Translate: ActiveAction = new TranslateAction(transform); break;
            }

            if (action.HasFlag(TransformActions.X)) ActiveAction.ActiveAxis = Axis.X;
            if (action.HasFlag(TransformActions.Y)) ActiveAction.ActiveAxis = Axis.Y;
            if (action.HasFlag(TransformActions.Z)) ActiveAction.ActiveAxis = Axis.Z;
        }

        public IPickable FindPickableAtPosition(GLContext context, Vector2 point) {
           return context.ColorPicker.FindPickableAtPosition(context, PickableObjects, point);
        }

        public void DrawSelection(GLContext context) {
            var selected = GetSelected();
            if (selected.Count == 0)
                return;

            var first = selected.FirstOrDefault();
            ActiveAction.Render(context);
        }

        [Flags]
        public enum TransformActions 
        {
            Translate = 0 >> 1,
            Scale = 0 >> 2,
            Rotate = 0 >> 3,
            X = 0 >> 4,
            Y = 0 >> 5,
            Z = 0 >> 6,
        }

        [Flags]
        public enum Axis
        {
            None = 0,
            X = 1,
            Y = 2,
            Z = 3,
        }
    }
}
