using System.Diagnostics;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    /// <summary>
    /// Handles mouse events:
    /// - Logs basic info
    /// - Example remap: swap right/left, block middle, invert wheel
    /// </summary>
    internal static class MouseOutput
    {
        public static void Attach()
        {
            MouseHook.OnMouseEvent += HandleMouse;
        }

        /// <summary>
        /// Main mouse handler. Return true to block original mouse event.
        /// </summary>
        private static bool HandleMouse(
            MouseEventType type,
            MouseButtons button,
            int x,
            int y,
            int delta,
            bool isInjected)
        {
            if (!RemapperState.Enabled)
                return false;

            if (isInjected)
                return false;
            //Debug.WriteLine($"Mouse: {type} {button} ({x},{y}) delta={delta} injected={isInjected}");
            if (type == MouseEventType.Down || type == MouseEventType.Up)
                return HandleButton(type, button);

            if (type == MouseEventType.Wheel)
                return HandleVerticalWheel(delta);

            // For now, horizontal wheel and move pass through unchanged.
            return false;
        }

        private static bool HandleButton(MouseEventType type, MouseButtons button)
        {
            bool buttonUp = (type == MouseEventType.Up);

            if (button == MouseButtons.Middle)
            {
                // Swallow middle button entirely.
                return true;
            }

            if (button == MouseButtons.Right)
            {
                MouseInput.SendButton(MouseButtons.Left, buttonUp);
                return true;
            }

            if (button == MouseButtons.Left)
            {
                // Left click passes through unchanged.
                return false;
            }

            if (button == MouseButtons.XButton1)
            {
                MouseInput.SendButton(MouseButtons.XButton2, buttonUp);
                return true;
            }

            if (button == MouseButtons.XButton2)
            {
                MouseInput.SendButton(MouseButtons.XButton1, buttonUp);
                return true;
            }

            return false;
        }

        private static bool HandleVerticalWheel(int delta)
        {
            if (delta == 0)
                return false;

            int inverted = -delta;
            MouseInput.ScrollVertical(inverted);
            return true;
        }
    }
}
