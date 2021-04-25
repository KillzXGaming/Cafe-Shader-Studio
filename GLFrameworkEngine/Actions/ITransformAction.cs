using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public interface ITransformAction
    {
        bool IsActive { get; set; }

        GLTransform Transform { get; set; }

        void Render(GLContext context);

        int OnMouseMove(GLContext context, MouseEventInfo e);
        int OnMouseDown(GLContext context, MouseEventInfo e);
        int OnMouseUp(GLContext context, MouseEventInfo e);
        GLScene.Axis ActiveAxis { get; set; }
    }
}
