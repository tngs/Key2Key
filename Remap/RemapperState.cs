namespace HotKeyDemo2
{
    /// <summary>
    /// Global on/off flag for all remapping (keyboard + mouse).
    /// Toggled by the panic key (Pause).
    /// </summary>
    internal static class RemapperState
    {
        public static volatile bool Enabled = true;
    }
}
