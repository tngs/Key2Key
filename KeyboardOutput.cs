using System;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace HotKeyDemo2
{
    internal static class KeyboardOutput
    {
        // ------ Win32 helpers for more accurate names ------
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);
        //with exted: EX: numpad0 = "Instert", without: numpad0 = "Num 0" etc

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);//uMapType:VK → scan, scan → VK, etc.

        private const uint MAPVK_VK_TO_VSC = 0x00;//Translate a virtual-key code (VK) into a scan code

        
        // Returns a simple logical name for a key (from Keys enum).
        // Example: Keys.A -> "A", Keys.Left -> "Left", Keys.Control -> "ControlKey"
        public static string GetLogicalName(Keys key)
        {
            return key.ToString();
        }

        
        // Returns a logical name from vkCode (like what you get in a hook).
        public static string GetLogicalName(uint vkCode)
        {
            return ((Keys)vkCode).ToString();
        }

        
        // Returns a human-readable combo like "Ctrl + Shift + A".
        // This is useful with KeyEventArgs (KeyDown/KeyUp) in forms.
        
        public static string DescribeKeyCombo(Keys key, Keys modifiers)
        {
            var parts = new System.Collections.Generic.List<string>();

            if ((modifiers & Keys.Control) != 0)
                parts.Add("Ctrl");
            if ((modifiers & Keys.Shift) != 0)
                parts.Add("Shift");
            if ((modifiers & Keys.Alt) != 0)
                parts.Add("Alt");

            // only the main key name at the end
            parts.Add(GetLogicalName(key));

            return string.Join(" + ", parts);
        }

        /// <summary>
        /// Gets a more "physical" key name using scan code (like "Left Shift", "Num 0", etc.).
        /// Used for low-level hooks when you have vkCode, scanCode, and extended flag.
        /// </summary>
        public static string GetPhysicalKeyName(uint vkCode, uint scanCode, bool isExtended)
        {
            // If scanCode is 0, map from VK to scan code
            if (scanCode == 0)
            {
                scanCode = MapVirtualKey(vkCode, MAPVK_VK_TO_VSC);
            }

            int lParam = (int)(scanCode << 16);

            // extended keys (arrows, right Ctrl, etc.)
            if (isExtended)
            {
                lParam |= 1 << 24;
            }

            StringBuilder sb = new StringBuilder(64);
            int result = GetKeyNameText(lParam, sb, sb.Capacity);

            if (result > 0)
                return sb.ToString();

            // fallback to logical name if API fails
            return GetLogicalName(vkCode);
        }
    }
}
