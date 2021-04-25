using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace GLFrameworkEngine
{
    public class CameraFrame
    {
        public static Frame CreateFrame(Camera camera, Vector4 boundingSphere)
        {
            camera.FrameBoundingSphere(boundingSphere);
            return new Frame() {
                ViewMatrix = camera.GetViewMatrix(),
            };
        }

        public class Frame
        {
            public Matrix4 ViewMatrix { get; set; }
        }
    }
}
