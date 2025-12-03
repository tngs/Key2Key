using HotKeyDemo2;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

internal static class KeyboardRemapper
{
    public static void Attach()
    {
        //-----------------------ConfigManager.Load("config.json");
        ProgramChecker.LoadConfig("config.json");//-------------------------
        KeyboardHook.OnKeyEvent += HandleKey;
    }

    private static bool HandleKey(Keys key, bool isKeyUp, bool isInjected)
    {
        // 1) ignore our own synthetic input
        if (isInjected)
            return false;

        ProgramChecker.ProgramMapping mapping = ProgramChecker.GetMappingForCurrentProgram();//-------------------------

        //-------------------------
        //// 2) block-only keys (no remap, just swallow)
        //if (ConfigManager.BlockOnly.Contains(key))
        //{
        //    // Optionally: no synthetic key here
        //    return true;
        //}

        //// 3) remap keys if configured
        //if (ConfigManager.RemapLookup.TryGetValue(key, out Keys target))
        //{
        //    KeyboardInput.SendMappedKey(target, isKeyUp);
        //    return true; // block original
        //}
        //-------------------------

        //------------------------
        if (mapping.BlockOnly.Contains(key))
        {
            return true; // block this key for this program
        }

        // 2) Remap
        Keys target;
        if (mapping.Remaps.TryGetValue(key, out target))
        {
            KeyboardInput.SendMappedKey(target, isKeyUp);
            return true; // block original key
        }
        //------------------------

        // 4) everything else passes through
        return false;
    }
}
