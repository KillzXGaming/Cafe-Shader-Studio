using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class GLErrorHandler
    {
        public static bool CheckGLError()
        {
            return false;

           /* var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Debug.Print($"{title}: {error}");
            }*/
        }
    }
}
