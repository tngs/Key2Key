
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;


namespace HotKeyDemo2
{

    public partial class Form5 : Form
    {
        // ----- Global hook state -----
        private static IntPtr _hookId = IntPtr.Zero; //hook id //for in case of canceling(or calling) the hook
        private static LowLevelKeyboardProc _proc = HookCallback;

        // ----- Key remap table -----
        // Press LEFT key -> send RIGHT
        // Press A -> send B
        private static readonly Dictionary<Keys, Keys> keyMap = new Dictionary<Keys, Keys>
        {
            { Keys.A, Keys.B },
            { Keys.Left, Keys.Right },
            // add more here: { Keys.Original, Keys.Remapped }
        };

        // ----- Target processes (only active in these apps) -----
        // Use file names, NOT window titles.
        private static readonly HashSet<string> targetProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "notepad.exe",
            "explorer.exe", // desktop / file explorer
            // add more process names here, e.g. "chrome.exe", "game.exe"
        };

        public Form5()
        {
            InitializeComponent();

            // Install global keyboard hook
            _hookId = SetHook(_proc);
        }

        private void Form5_Load(object sender, EventArgs e)
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UninstallHookSafe();   // turn off hook when this form closes
            base.OnFormClosing(e);
        }
        //unhooking safely
        public static void UninstallHookSafe()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        // ================== HOOK SETUP ==================

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())//“Give me a managed object that represents this EXE.”
            using (ProcessModule curModule = curProcess.MainModule)//“Give me the main module of this process”//yourapp.exe
            {
                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,//“I want a low-level keyboard hook.”
                    proc,//“This is the function to call on every key event.”
                    GetModuleHandle(curModule.ModuleName),//“Give me the HMODULE (internal handle) for this EXE.”
                    0);//0 = “Hook all threads in the system”
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);//It defines the shape of the function Windows is allowed to call.
        //delegate: “A variable that can hold a function with this exact signature.”

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)//Windows Hook Keyboard Low-Level//Hook Code: Action
            //other wise, pass it on
            {//we are allowed to process this event here
                int msg = wParam.ToInt32();//windows message code

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYUP)//key event
                {
                    KBDLLHOOKSTRUCT kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);//keyboarD

                    // Ignore events we injected ourselves
                    if ((kbd.flags & LLKHF_INJECTED) == 0)
                    {
                        Keys vk = (Keys)kbd.vkCode;//which key was pressed

                        // Only act in target processes
                        if (IsTargetProcessActive() && keyMap.TryGetValue(vk, out Keys mapped))
                        {
                            bool keyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                            // Send the mapped key instead
                            SendMappedKey(mapped, keyUp);

                            // Block the original key from reaching apps
                            return (IntPtr)1;
                        }
                    }
                }
            }

            // Pass through if we didn't handle it
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ================== HELPER: CHECK ACTIVE PROCESS ==================

        private static bool IsTargetProcessActive()
        {
            IntPtr hwnd = GetForegroundWindow();//Handle to a WiNDow

            // No active window
            //e.g. desktop with no windows open
            if (hwnd == IntPtr.Zero)
                return false;

            uint pid;//program ID
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0)//no process
                return false;

            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, false, pid);
            //“Give me a handle to this process (PID), with these access rights.”
            if (hProcess == IntPtr.Zero)
                return false;

            try
            {
                StringBuilder sb = new StringBuilder(260);
                if (GetModuleFileNameEx(hProcess, IntPtr.Zero, sb, sb.Capacity) == 0)
                    return false;

                string path = sb.ToString();
                //Debug.WriteLine($"++++++{path}");//litteral c:\windows\system32\notepad.exe
                string fileName = Path.GetFileName(path); // e.g. "notepad.exe"

                return targetProcesses.Contains(fileName);//running through targetProcesses
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        // ================== HELPER: SEND MAPPED KEY ==================

        private static void SendMappedKey(Keys key, bool keyUp)
        {
            byte vk = (byte)key;
            const uint KEYEVENTF_KEYUP = 0x0002;

            keybd_event(vk, 0, keyUp ? KEYEVENTF_KEYUP : 0, UIntPtr.Zero);
        }

        // ================== CONSTANTS & STRUCTS ==================

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const uint LLKHF_INJECTED = 0x00000010;

        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int PROCESS_VM_READ = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;//which key
            public uint scanCode;
            public uint flags;//how that key event happened
            /*
             * const uint LLKHF_EXTENDED = 0x01;   // extended key (like Right Alt, arrow keys, etc.)
             * const uint LLKHF_INJECTED = 0x10;   // key was injected by software
             * const uint LLKHF_ALTDOWN  = 0x20;   // ALT key is down for this event
             * const uint LLKHF_UP       = 0x80;   // key up event
             */
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // ================== P/INVOKE ==================

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule,
            [Out] StringBuilder lpFilename, int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }
}

