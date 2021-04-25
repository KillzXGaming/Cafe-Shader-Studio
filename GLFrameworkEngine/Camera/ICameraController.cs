using OpenTK;

namespace GLFrameworkEngine
{
    public interface ICameraController
    {
        void MouseClick(MouseEventInfo e, KeyEventInfo k);
        void MouseMove(MouseEventInfo e, KeyEventInfo k, Vector2 previousLocation);
        void MouseWheel(MouseEventInfo e, KeyEventInfo k);
        void KeyPress(KeyEventInfo e);
    }
}
