using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    /// <summary>
    /// Helper for sending synthetic keyboard input (key down / key up).
    /// You can call this from your hook code or controller code.
    /// </summary>
    internal static class KeyboardInput
    {
        // Legacy, but simple and fine for your use case.
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(
            byte bVk,
            byte bScan,
            uint dwFlags,
            UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        //no: “Pretend the user pressed the key whose virtual key is vk.”                           //Windows reads bVk (virtual key code).
        //yes:“Pretend the user pressed the key whose hardware scan code is bScan (ignore bVk).”    //Windows reads bScan (scan code) and ignores bVk.
        //Scan code = “physical key at this position on the keyboard”
        //Virtual key (VK) = “logical key meaning: ‘A’, ‘Left Arrow’, ‘Space’…”

        /// <summary>
        /// Send a virtual key down event.
        /// </summary>
        public static void SendKeyDown(Keys key)
        {
            byte vk = (byte)key;
            keybd_event(vk, 0, 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Send a virtual key up event.
        /// </summary>
        public static void SendKeyUp(Keys key)
        {
            byte vk = (byte)key;
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        /// <summary>
        /// Press and release a key (simple tap).
        /// </summary>
        public static void SendKeyPress(Keys key, int releaseDelayMs = 10)
        {
            SendKeyDown(key);
            System.Threading.Thread.Sleep(releaseDelayMs);
            SendKeyUp(key);
        }

        /// <summary>
        /// Your original style: one function, keyUp flag.
        /// </summary>
        public static void SendMappedKey(Keys key, bool keyUp)//nor version
        {
            byte vk = (byte)key;
            keybd_event(vk, 0, keyUp ? KEYEVENTF_KEYUP : 0, UIntPtr.Zero);
        }

        /// <summary>
        /// Send a chord like Ctrl + key (e.g., Ctrl+C).
        /// </summary>
        public static void SendChord(Keys modifier, Keys key, int releaseDelayMs = 10)
        {
            // Down: modifier, then key
            SendKeyDown(modifier);
            SendKeyDown(key);

            System.Threading.Thread.Sleep(releaseDelayMs);

            // Up: key, then modifier
            SendKeyUp(key);
            SendKeyUp(modifier);
        }
    }
}
