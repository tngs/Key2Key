using SharpDX.Direct3D;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class KeyboardHook
{
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookId = IntPtr.Zero;

    private const int WH_KEYBOARD_LL = 13;

    private const uint LLKHF_UP = 0x80;
    private const uint LLKHF_INJECTED = 0x10;
    private const uint LLKHF_LOWER_IL_INJECTED = 0x02;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // ===== public "event" style API =====
    public delegate bool KeyEventHandler(Keys key, bool isKeyUp, bool isInjected);
    public static event KeyEventHandler OnKeyEvent;

    public static void Install()
    {
        if (_hookId != IntPtr.Zero)
            return;

        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            Debug.WriteLine($"curModule.ModuleName: {curModule.ModuleName}");

            _hookId = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                _proc,
                GetModuleHandle(curModule.ModuleName),
                0);
        }

        Debug.WriteLine("Keyboard hook installed.");
    }

    public static void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Debug.WriteLine("Keyboard hook uninstalled.");
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        Keys key = (Keys)kbd.vkCode;

        bool isKeyUp = (kbd.flags & LLKHF_UP) != 0;

        bool isInjected =
            (kbd.flags & LLKHF_INJECTED) != 0 ||
            (kbd.flags & LLKHF_LOWER_IL_INJECTED) != 0;
        
        bool blockEvent = false;
        // Option: ignore injected events globally
        if (isInjected)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);

        if (OnKeyEvent != null)
        {
            foreach (KeyEventHandler handler in OnKeyEvent.GetInvocationList())
            {
                if (handler(key, isKeyUp, isInjected))
                {
                    blockEvent = true;
                }
            }
        }

        if (blockEvent)
        {
            // Swallow this key: it never reaches the target window
            return (IntPtr)1;
        }

        // Let it pass through normally
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
