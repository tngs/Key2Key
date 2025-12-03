using HotKeyDemo2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class KeyboardHook
{
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);//≈ WH_KEYBOARD_LL Structure
    private static LowLevelKeyboardProc _proc = HookCallback;//basically my function's pointer
    private static IntPtr _hookId = IntPtr.Zero;
    private static bool[] _keyIsDown = new bool[256];

    private const int WH_KEYBOARD_LL = 13;//“I want to install a low-level keyboard hook.”
    //Log keys,
    //Remap them (using your KeyboardInput),
    //Block them (by returning (IntPtr)1).

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const uint LLKHF_UP = 0x80;// low-level keyboard flag: key up event
    private const uint LLKHF_INJECTED = 0x10;// low-level keyboard flag: event was injected
    private const uint LLKHF_LOWER_IL_INJECTED = 0x02;// low-level keyboard flag: event was injected from lower integrity level

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

    private static bool _ctrlDown = false;
    private static bool _altDown = false;
    private static bool _shiftDown = false;

    public static void Install()
    {
        if (_hookId != IntPtr.Zero)
            return;
        using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())//“Give me a managed object that represents this EXE.”
        using (var curModule = curProcess.MainModule)////“Give me the main module of this process”//yourapp.exe
        {
            Debug.WriteLine($"curModule.ModuleName: {curModule.ModuleName}");

            _hookId = SetWindowsHookEx(
                WH_KEYBOARD_LL,                         //“low-level keyboard hook”
                _proc,                                  // your callback delegate
                GetModuleHandle(curModule.ModuleName),  // module handle of your exe
                0);                                     // 0 = hook all threads (global)
            //“Call _proc (your HookCallback) for every keyboard event, globally (all apps), before the messages go to the target window.”
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

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)//that prints the pressed and released keys
    {
        if (nCode < 0)
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        // The event is not valid, or you’re not allowed to look at wParam / lParam.


        //The event is valid, and you’re allowed to look at wParam / lParam and possibly act on it.


        var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);//lparam->kb
        Keys key = (Keys)kbd.vkCode;

        int msg = wParam.ToInt32();
        bool isKeyUp = (kbd.flags & LLKHF_UP) != 0;
        bool isKeyDown = !isKeyUp;
        bool isInjected =
            (kbd.flags & LLKHF_INJECTED) != 0 ||
            (kbd.flags & LLKHF_LOWER_IL_INJECTED) != 0;
        if (IsPureModifier(key))
        {
            if (isKeyDown || isKeyUp)
            {
                switch (key)
                {
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                    case Keys.ControlKey:
                        _ctrlDown = isKeyDown;
                        break;

                    case Keys.LMenu:      // Left Alt
                    case Keys.RMenu:      // Right Alt
                    case Keys.Menu:       // Alt (generic)
                        _altDown = isKeyDown;
                        break;

                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                    case Keys.ShiftKey:
                        _shiftDown = isKeyDown;
                        break;
                }
            }
        }
        Keys mods = Keys.None;
        if (_ctrlDown) mods |= Keys.Control;
        if (_altDown) mods |= Keys.Alt;
        if (_shiftDown) mods |= Keys.Shift;

        if (isKeyDown)
        {
            if (!_keyIsDown[kbd.vkCode])
            {
                _keyIsDown[kbd.vkCode] = true;

                Debug.WriteLine($"\nDW: {key} [{(IsPureModifier(key) ? "yes" : "no")}]");
                if (!IsPureModifier(key))
                {
                    string combo = KeyboardOutput.DescribeKeyCombo(key, mods);
                    Debug.WriteLine($"[ENTRY] {combo}");
                }



            }

        }
        else if (isKeyUp)
        {
            _keyIsDown[kbd.vkCode] = false;

            Debug.WriteLine($"\nUP: {key} [{(IsPureModifier(key) ? "yes" : "no")}]");
            if (!IsPureModifier(key))
            {
                string combo = KeyboardOutput.DescribeKeyCombo(key, mods);
                Debug.WriteLine($"[ENTRY] {combo}");
            }
        }


        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    private static bool IsPureModifier(Keys key)
    {
        return key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey
            || key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey
            || key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu;
    }
    //private static IntPtr HookCallback2(int nCode, IntPtr wParam, IntPtr lParam)//remap hook
    //{
    //    if (nCode < 0)
    //        return CallNextHookEx(_hookId, nCode, wParam, lParam);

    //    var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
    //    Keys key = (Keys)kbd.vkCode;

    //    bool isKeyUp = (kbd.flags & LLKHF_UP) != 0;
    //    bool isInjected =
    //        (kbd.flags & LLKHF_INJECTED) != 0 ||
    //        (kbd.flags & LLKHF_LOWER_IL_INJECTED) != 0;

    //    // 1) Ignore our own synthetic keys (recursion prevention)
    //    if (isInjected)
    //    {
    //        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    //    }

    //    // 2) Remap K -> A and BLOCK the original K
    //    if (key == Keys.K)
    //    {
    //        // Send A instead of K, preserving up/down
    //        KeyboardInput.SendMappedKey(Keys.A, isKeyUp);

    //        // Important: returning 1 blocks the original K from reaching the app
    //        return (IntPtr)1;
    //    }

    //    // 3) Everything else passes through normally
    //    return CallNextHookEx(_hookId, nCode, wParam, lParam);
    //}

}
