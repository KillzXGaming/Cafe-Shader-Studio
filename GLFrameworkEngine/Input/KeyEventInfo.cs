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
        public List<char> KeyChars { get; set; } = new List<char>();

        public bool KeyShift { get; set; }
        public bool KeyCtrl { get; set; }
        public bool KeyAlt { get; set; }

        public bool IsKeyDown(char key)
        {
            return KeyChars.Contains(key);
        }
    }
}
