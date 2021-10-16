using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using OpenTK;

namespace GLFrameworkEngine
{
    public class MouseEventInfo
    {
        public bool HasValue { get; set; } = true;
        public bool CursorVisible { get; set; } = true;

        public int X => Position.X;
        public int Y => Position.Y;

        public System.Drawing.Point Position { get; set; } // Setting does not affect OS mouse position because of some scaling differences between ImGui and OpenTK. Use FullPosition instead.

        public System.Drawing.Point FullPosition { get; set; }

        public ButtonState RightButton { get; set; }
        public ButtonState LeftButton { get; set; }

        public float Delta { get; set; }

        public float WheelPrecise { get; set; }
    }
}
