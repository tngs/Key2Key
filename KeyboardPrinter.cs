using HotKeyDemo2;
using System.Diagnostics;
using System.Windows.Forms;

internal static class KeyboardPrinter
{
    private static bool[] _keyIsDown = new bool[256];
    private static bool _ctrlDown, _altDown, _shiftDown;

    public static void Attach()
    {
        KeyboardHook.OnKeyEvent += HandleKey;
    }

    private static bool HandleKey(Keys key, bool isKeyUp, bool isInjected)
    {
        bool isKeyDown = !isKeyUp;

        // update modifier state
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
                    case Keys.LMenu:
                    case Keys.RMenu:
                    case Keys.Menu:
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

        // build modifiers
        Keys mods = Keys.None;
        if (_ctrlDown) mods |= Keys.Control;
        if (_altDown) mods |= Keys.Alt;
        if (_shiftDown) mods |= Keys.Shift;

        uint vk = (uint)key;

        if (isKeyDown)
        {
            if (!_keyIsDown[vk])
            {
                _keyIsDown[vk] = true;

                Debug.WriteLine($"\nDW: {key} [{(IsPureModifier(key) ? "yes" : "no")}]");
                if (!IsPureModifier(key))
                {
                    string combo = KeyboardOutput.DescribeKeyCombo(key, mods);
                    Debug.WriteLine($"[ENTRY] {combo}");
                }
            }
        }
        else // isKeyUp
        {
            _keyIsDown[vk] = false;

            Debug.WriteLine($"\nUP: {key} [{(IsPureModifier(key) ? "yes" : "no")}]");
            if (!IsPureModifier(key))
            {
                string combo = KeyboardOutput.DescribeKeyCombo(key, mods);
                Debug.WriteLine($"[ENTRY] {combo}");
            }
        }
        return false;
    }

    private static bool IsPureModifier(Keys key)
    {
        return key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey
            || key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey
            || key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu;
    }
}
