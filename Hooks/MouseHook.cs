using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    internal enum MouseEventType
    {
        Move,
        Down,
        Up,
        Wheel,
        HWheel
    }

    /// <summary>
    /// Global low-level mouse hook (WH_MOUSE_LL).
    /// Dispatches events to OnMouseEvent handlers.
    /// </summary>
    internal static class MouseHook
    {
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        private const int WH_MOUSE_LL = 14;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int WM_MOUSEHWHEEL = 0x020E;

        private const uint LLMHF_INJECTED = 0x00000001;
        private const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public delegate bool MouseEventHandler(
            MouseEventType type,
            MouseButtons button,
            int x,
            int y,
            int delta,
            bool isInjected);

        public static event MouseEventHandler OnMouseEvent;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelMouseProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void Install()
        {
            // Avoid installing multiple times.
            if (_hookId != IntPtr.Zero)
                return;

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(
                    WH_MOUSE_LL,
                    _proc,
                    GetModuleHandle(curModule.ModuleName),
                    0);
            }

            Debug.WriteLine("Mouse hook installed.");
        }

        public static void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                Debug.WriteLine("Mouse hook uninstalled.");
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode < 0)
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);

                MSLLHOOKSTRUCT ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int msg = wParam.ToInt32();

                MouseEventType type = MouseEventType.Move;
                MouseButtons button = MouseButtons.None;
                int delta = 0;

                bool isInjected =
                    (ms.flags & LLMHF_INJECTED) != 0 ||
                    (ms.flags & LLMHF_LOWER_IL_INJECTED) != 0;

                switch (msg)
                {
                    case WM_MOUSEMOVE:
                        type = MouseEventType.Move;
                        break;

                    case WM_LBUTTONDOWN:
                        type = MouseEventType.Down;
                        button = MouseButtons.Left;
                        break;

                    case WM_LBUTTONUP:
                        type = MouseEventType.Up;
                        button = MouseButtons.Left;
                        break;

                    case WM_RBUTTONDOWN:
                        type = MouseEventType.Down;
                        button = MouseButtons.Right;
                        break;

                    case WM_RBUTTONUP:
                        type = MouseEventType.Up;
                        button = MouseButtons.Right;
                        break;

                    case WM_MBUTTONDOWN:
                        type = MouseEventType.Down;
                        button = MouseButtons.Middle;
                        break;

                    case WM_MBUTTONUP:
                        type = MouseEventType.Up;
                        button = MouseButtons.Middle;
                        break;

                    case WM_XBUTTONDOWN:
                        type = MouseEventType.Down;
                        button = DecodeXButton(ms.mouseData);
                        break;

                    case WM_XBUTTONUP:
                        type = MouseEventType.Up;
                        button = DecodeXButton(ms.mouseData);
                        break;

                    case WM_MOUSEWHEEL:
                        type = MouseEventType.Wheel;
                        delta = DecodeWheelDelta(ms.mouseData);
                        break;

                    case WM_MOUSEHWHEEL:
                        type = MouseEventType.HWheel;
                        delta = DecodeWheelDelta(ms.mouseData);
                        break;

                    default:
                        return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }
                bool block = false;

                //if(isInjected)
                //    return CallNextHookEx(_hookId, nCode, wParam, lParam);

                if (OnMouseEvent != null)
                {
                    foreach (MouseEventHandler handler in OnMouseEvent.GetInvocationList())
                    {
                        if (handler(type, button, ms.pt.x, ms.pt.y, delta, isInjected))
                        {
                            block = true;
                        }
                    }
                }

                if (block)
                    return (IntPtr)1;

                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MouseHook] Exception in HookCallback: " + ex);
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }
        }

        private static MouseButtons DecodeXButton(uint mouseData)
        {
            ushort hiWord = (ushort)((mouseData >> 16) & 0xFFFF);
            if (hiWord == 1) return MouseButtons.XButton1;
            if (hiWord == 2) return MouseButtons.XButton2;
            return MouseButtons.None;
        }

        private static int DecodeWheelDelta(uint mouseData)
        {
            short hiWord = (short)((mouseData >> 16) & 0xFFFF);
            return (int)hiWord;
        }
    }
}
