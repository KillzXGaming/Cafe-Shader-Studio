using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLFrameworkEngine
{
    public interface IPickable
    {
        GLTransform Transform { get; set; }

        bool IsHovered { get; set; }

        bool IsSelected { get; set; }

        void DrawColorPicking(GLContext context);

        //When something outside has dropped onto the picking object (ie a material onto a mesh)
        void DragDroppedOnLeave();
        void DragDroppedOnEnter();
        void DragDropped(object droppedItem);
    }
}
