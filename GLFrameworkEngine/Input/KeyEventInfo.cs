using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;

namespace GLFrameworkEngine
{
    public class KeyEventInfo
    {
        public char KeyChar { get; set; }

        public bool KeyShift { get; set; }
        public bool KeyCtrl { get; set; }
        public bool KeyAlt { get; set; }

        public bool IsKeyDown(char key)
        {
            return key == KeyChar;
        }
    }
}
