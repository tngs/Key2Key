using System.Collections.Generic;

namespace HotKeyDemo2
{
    /// <summary>
    /// Root of config.json (all profiles + active profile name).
    /// </summary>
    internal class RootConfig
    {
        public Dictionary<string, ProfileConfig> Profiles { get; set; }
            = new Dictionary<string, ProfileConfig>();

        public string ActiveProfile { get; set; }
    }

    /// <summary>
    /// A profile groups program-specific mappings.
    /// </summary>
    internal class ProfileConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Key: exe name (e.g., "notepad.exe" or "*.exe").
        /// </summary>
        public Dictionary<string, ProgramConfig> Programs { get; set; }
            = new Dictionary<string, ProgramConfig>();
        public override string ToString()
        {
            int programCount = Programs != null ? Programs.Count : 0;
            return $"Name={Name}, Description={Description}, Programs={programCount}";
        }
    }

    /// <summary>
    /// Mappings for a single program (set of remap rules).
    /// </summary>
    internal class ProgramConfig
    {
        public List<RemapRule> Remaps { get; set; }
            = new List<RemapRule>();
    }

    /// <summary>
    /// One mapping: From key -> sequence of actions.
    /// </summary>
    internal class RemapRule
    {
        public string From { get; set; }

        /// <summary>
        /// Ordered actions to run when From key is pressed.
        /// </summary>
        public List<RemapAction> To { get; set; }
            = new List<RemapAction>();
    }

    /// <summary>
    /// Single step in a sequence (keyboard/mouse/controller/delay).
    /// </summary>
    internal class RemapAction
    {
        public string Type { get; set; }  // "Mouse", "Keyboard", "Controller", "Delay"
        public string Data { get; set; }  // e.g. "MoveBy(100,0)", "Key(A)", "100ms"
    }

}
