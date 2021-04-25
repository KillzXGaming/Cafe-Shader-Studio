using OpenTK;

namespace GLFrameworkEngine
{
    public interface ICameraController
    {
        void MouseClick(MouseEventInfo e);
        void MouseMove(MouseEventInfo e, Vector2 previousLocation);
        void MouseWheel(MouseEventInfo e);
        void KeyPress(KeyEventInfo e);
    }
}
