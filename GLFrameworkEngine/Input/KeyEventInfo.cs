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
        public List<string> KeyChars { get; set; } = new List<string>();

        public bool KeyShift { get; set; }
        public bool KeyCtrl { get; set; }
        public bool KeyAlt { get; set; }

        public bool IsKeyDown(string key) {
            if (KeyCtrl && key.StartsWith("ctrl+"))
                return KeyChars.Contains(key.Split("+").Last());
            if (KeyShift && key.StartsWith("shift+"))
                return KeyChars.Contains(key.Split("+").Last());
            if (KeyAlt && key.StartsWith("alt+"))
                return KeyChars.Contains(key.Split("+").Last());


            return KeyChars.Contains(key);
        }
    }
}
