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
        public string MOVE_FORWARD = "w";
        public string MOVE_BACK = "s";
        public string MOVE_LEFT = "a";
        public string MOVE_RIGHT = "d";
        public string MOVE_UP = "space";
        public string MOVE_DOWN = "ctrl+space";
    }
}
