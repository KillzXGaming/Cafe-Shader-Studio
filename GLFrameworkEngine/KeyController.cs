using System;
using System.Collections.Generic;
using System.Text;

namespace GLFrameworkEngine
{
    public class KeyController
    {
        public static VIEWPORT3D View3D = new VIEWPORT3D();
    }

    public class VIEWPORT3D
    {
        public char MOVE_FORWARD = 'w';
        public char MOVE_BACK = 's';
        public char MOVE_LEFT = 'a';
        public char MOVE_RIGHT = 'd';
    }
}
