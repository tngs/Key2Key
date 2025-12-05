using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    internal static class MouseInput
    {
        private const uint INPUT_MOUSE = 0;

        [Flags]
        private enum MouseEventFlags : uint
        {
            MOVE = 0x0001,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            XDOWN = 0x0080,
            XUP = 0x0100,
            WHEEL = 0x0800,
            HWHEEL = 0x01000,
            ABSOLUTE = 0x8000
        }

        private const uint XBUTTON1 = 0x0001;
        private const uint XBUTTON2 = 0x0002;

        // for screen size (absolute coords)
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint nInputs,
            [In] INPUT[] pInputs,
            int cbSize);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static void Send(MouseEventFlags flags, int dx, int dy, uint mouseData)
        {
            INPUT[] inputs = new INPUT[1];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = dx;
            inputs[0].mi.dy = dy;
            inputs[0].mi.mouseData = mouseData;
            inputs[0].mi.dwFlags = (uint)flags;
            inputs[0].mi.time = 0;
            inputs[0].mi.dwExtraInfo = IntPtr.Zero;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // ===== public API =====

        public static void SendButton(MouseButtons button, bool buttonUp)
        {
            MouseEventFlags flags;
            uint mouseData = 0;

            switch (button)
            {
                case MouseButtons.Left:
                    flags = buttonUp ? MouseEventFlags.LEFTUP : MouseEventFlags.LEFTDOWN;
                    break;

                case MouseButtons.Right:
                    flags = buttonUp ? MouseEventFlags.RIGHTUP : MouseEventFlags.RIGHTDOWN;
                    break;

                case MouseButtons.Middle:
                    flags = buttonUp ? MouseEventFlags.MIDDLEUP : MouseEventFlags.MIDDLEDOWN;
                    break;

                case MouseButtons.XButton1:
                    flags = buttonUp ? MouseEventFlags.XUP : MouseEventFlags.XDOWN;
                    mouseData = XBUTTON1;
                    break;

                case MouseButtons.XButton2:
                    flags = buttonUp ? MouseEventFlags.XUP : MouseEventFlags.XDOWN;
                    mouseData = XBUTTON2;
                    break;

                default:
                    return;
            }

            Send(flags, 0, 0, mouseData);
        }

        public static void SendClick(MouseButtons button)
        {
            SendButton(button, false);
            SendButton(button, true);
        }

        
        // Move mouse cursor relative to current position (dx, dy in pixels).
        public static void MoveBy(int dx, int dy)
        {
            Send(MouseEventFlags.MOVE, dx, dy, 0);
        }

        
        // Move mouse cursor to an absolute screen position (x, y) in pixels.
        // Uses SendInput with MOUSEEVENTF_ABSOLUTE.
        public static void MoveTo(int x, int y)
        {
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            if (screenWidth <= 1 || screenHeight <= 1)
                return;

            // Normalize to 0–65535 (inclusive) as required by SendInput for ABSOLUTE
            // (x,y) are pixel coords: 0..screenWidth-1, 0..screenHeight-1
            int normalizedX = (int)Math.Round((x * 65535.0) / (screenWidth - 1));
            int normalizedY = (int)Math.Round((y * 65535.0) / (screenHeight - 1));

            Send(MouseEventFlags.MOVE | MouseEventFlags.ABSOLUTE, normalizedX, normalizedY, 0);
        }

        public static void ScrollVertical(int delta)
        {
            Send(MouseEventFlags.WHEEL, 0, 0, (uint)delta);
        }

        public static void ScrollHorizontal(int delta)
        {
            Send(MouseEventFlags.HWHEEL, 0, 0, (uint)delta);
        }

        public static void SendMappedMouseButton(MouseButtons button, bool buttonUp)
        {
            SendButton(button, buttonUp);
        }
    }
}
