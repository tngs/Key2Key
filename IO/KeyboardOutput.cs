using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace HotKeyDemo2
{
    /// <summary>
    /// Handles keyboard events:
    /// - Tracks modifiers (Ctrl/Alt/Shift)
    /// - Logs combos (for debugging)
    /// - Executes remap sequences from config.json
    /// </summary>
    internal static class KeyboardOutput
    {
        // Track "key already down" to avoid repeat firing while held.
        private static bool[] _keyIsDown = new bool[256];

        // Modifier state
        private static bool _ctrlDown;
        private static bool _altDown;
        private static bool _shiftDown;

        public static void Attach()
        {
            KeyboardHook.OnKeyEvent += HandleKey;
        }

        /// <summary>
        /// Main keyboard handler. Return true to block original key.
        /// </summary>
        private static bool HandleKey(Keys key, bool isKeyUp, bool isInjected)
        {
            // 0) Panic key: Pause toggles all remapping ON/OFF and is never blocked.
            if (key == Keys.End && !isInjected)
            {
                if (!isKeyUp)
                {
                    RemapperState.Enabled = !RemapperState.Enabled;
                    Debug.WriteLine("[KeyboardOutput] Toggled Enabled = " + RemapperState.Enabled);
                }
                return false;
            }

            //if safety is off, pass through all keys
            if (!RemapperState.Enabled)
                return false;

            // We already ignore injected in KeyboardHook; but keep this for clarity.
            if (isInjected)
                return false;

            // 1) Update modifier state first
            UpdateModifierState(key, isKeyUp);

            // 2) Handle up/down for non-modifier keys
            //& 0xFF keeps only the lower 8 bits// to fit in 255
            int idx = ((int)key) & 0xFF;
            if (isKeyUp)
            {
                if (idx >= 0 && idx < _keyIsDown.Length)//cheking bounds
                    _keyIsDown[idx] = false;
                if (IsPureModifier(key))
                {
                    LogModifierChange(key, isKeyUp);
                    return false; // we only fire sequences on key down
                }
                LogKeyEvent(key, isKeyUp);
                return false; // we only fire sequences on key down
            }

            // isKeyDown
            if (idx >= 0 && idx < _keyIsDown.Length)//cheking bounds
            {
                if (_keyIsDown[idx])// key already down
                {
                    // auto-repeat; ignore to avoid multiple macro triggers
                    return false;
                }
                _keyIsDown[idx] = true;
            }

            // 3) Build current modifier combo for logging
            if (IsPureModifier(key))
            {
                LogModifierChange(key, isKeyUp);
                return false; // we only fire sequences on key down
            }
            Keys mods = GetCurrentModifiers();
            string combo = DescribeKeyCombo(key, mods);
            Debug.WriteLine("[KeyboardOutput] DOWN: " + combo);








            //this part: ONLY READING BCZ ILL CHANGE IT ENTIRELY LATER





            // 4) Get actions for this key from config.json
            ProgramChecker.ProgramMapping mapping = ProgramChecker.GetMappingForCurrentProgram();

            List<RemapAction> actions;
            if (!mapping.Sequences.TryGetValue(key, out actions) ||
                actions == null || actions.Count == 0)
            {
                // No mapping = let original key pass through
                return false;
            }

            Debug.WriteLine("[KeyboardOutput] MAPPED: " + combo + " -> " + actions.Count + " action(s)");

            // 5) Run sequence off the hook thread
            Task.Run(() => ExecuteSequence(actions));

            // 6) Block original key
            return true;
        }

        // ===== Modifier handling =====

        private static bool IsPureModifier(Keys key)
        {
            return key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey
                || key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey
                || key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu;
        }

        private static void UpdateModifierState(Keys key, bool isKeyUp)
        {
            bool down = !isKeyUp;

            switch (key)
            {
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.ControlKey:
                    _ctrlDown = down;
                    break;

                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.ShiftKey:
                    _shiftDown = down;
                    break;

                case Keys.LMenu:   // Left Alt
                case Keys.RMenu:   // Right Alt
                case Keys.Menu:    // generic Alt
                    _altDown = down;
                    break;
            }
        }

        private static Keys GetCurrentModifiers()
        {
            Keys mods = Keys.None;
            if (_ctrlDown) mods |= Keys.Control;
            if (_altDown) mods |= Keys.Alt;
            if (_shiftDown) mods |= Keys.Shift;
            return mods;
        }

        private static void LogModifierChange(Keys key, bool isKeyUp)
        {
            string state = isKeyUp ? "UP" : "DOWN";
            Debug.WriteLine($"[KeyboardOutput] {state}: {key} (modifier)\t\t\t\t{(uint)key}");
        }

        private static void LogKeyEvent(Keys key, bool isKeyUp)
        {
            string state = isKeyUp ? "UP" : "DOWN";
            Debug.WriteLine($"[KeyboardOutput] {state}: {key}\t\t\t\t{(uint)key}");
        }

        private static string DescribeKeyCombo(Keys key, Keys mods)
        {
            List<string> parts = new List<string>();
            if ((mods & Keys.Control) != 0) parts.Add("Ctrl");
            if ((mods & Keys.Shift) != 0) parts.Add("Shift");
            if ((mods & Keys.Alt) != 0) parts.Add("Alt");

            parts.Add(key.ToString());
            return string.Join("+", parts);
        }

        // ===== Sequence execution =====

        private static void ExecuteSequence(List<RemapAction> actions)
        {
            foreach (RemapAction action in actions)
            {
                if (action == null || string.IsNullOrEmpty(action.Type))
                    continue;

                string type = action.Type.Trim();
                string data = action.Data != null ? action.Data.Trim() : "";

                switch (type)
                {
                    case "Keyboard":
                        ExecuteKeyboardAction(data);
                        break;

                    case "Mouse":
                        ExecuteMouseAction(data);
                        break;

                    case "Delay":
                        ExecuteDelayAction(data);
                        break;

                    case "Controller":
                        // Placeholder for future controller output
                        break;
                }
            }
        }

        private static void ExecuteKeyboardAction(string data)
        {
            // "Key(A)", "Key(Space)"
            string keyName = ExtractBetween(data, "Key(", ")");
            if (string.IsNullOrEmpty(keyName))
                return;

            Keys target;
            if (!Enum.TryParse(keyName, out target))
                return;

            KeyboardInput.SendMappedKey(target, false); // down
            KeyboardInput.SendMappedKey(target, true);  // up
        }

        private static void ExecuteMouseAction(string data)
        {
            // Supported:
            // "Click(Left)"
            // "MoveBy(100,0)"
            // "MoveTo(500,300)"

            if (data.StartsWith("Click("))
            {
                string btnName = ExtractBetween(data, "Click(", ")");
                MouseButtons btn;
                if (!Enum.TryParse(btnName, out btn))
                    return;

                MouseInput.SendClick(btn);
            }
            else if (data.StartsWith("MoveBy("))
            {
                int dx, dy;
                if (TryParseTwoInts(data, "MoveBy(", ")", out dx, out dy))
                {
                    MouseInput.MoveBy(dx, dy);
                }
            }
            else if (data.StartsWith("MoveTo("))
            {
                int x, y;
                if (TryParseTwoInts(data, "MoveTo(", ")", out x, out y))
                {
                    MouseInput.MoveTo(x, y);
                }
            }
        }

        private static void ExecuteDelayAction(string data)
        {
            int ms = ParseDelay(data);
            if (ms > 0)
            {
                Thread.Sleep(ms);
            }
        }

        // ===== Helpers =====

        private static string ExtractBetween(string text, string start, string end)
        {
            int i = text.IndexOf(start, StringComparison.Ordinal);
            if (i < 0)
                return null;
            i += start.Length;
            int j = text.IndexOf(end, i, StringComparison.Ordinal);
            if (j < 0)
                return null;

            return text.Substring(i, j - i).Trim();
        }

        private static bool TryParseTwoInts(string text, string start, string end, out int a, out int b)
        {
            a = 0;
            b = 0;

            string inside = ExtractBetween(text, start, end);
            if (string.IsNullOrEmpty(inside))
                return false;

            string[] parts = inside.Split(',');
            if (parts.Length != 2)
                return false;

            return int.TryParse(parts[0].Trim(), out a) &&
                   int.TryParse(parts[1].Trim(), out b);
        }

        private static int ParseDelay(string data)
        {
            int ms = 0;
            string lower = data.ToLowerInvariant().Trim();

            if (lower.EndsWith("ms"))
            {
                string num = lower.Substring(0, lower.Length - 2);
                int.TryParse(num, out ms);
            }
            else if (lower.EndsWith("s"))
            {
                string num = lower.Substring(0, lower.Length - 1);
                int seconds;
                if (int.TryParse(num, out seconds))
                    ms = seconds * 1000;
            }
            else
            {
                int.TryParse(lower, out ms);
            }

            return ms;
        }
    }
}
