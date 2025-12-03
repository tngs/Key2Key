using System.Collections.Generic;

namespace HotKeyDemo2
{
    internal class RootConfig
    {
        public Dictionary<string, ProfileConfig> Profiles { get; set; } = new Dictionary<string, ProfileConfig>();

        public string ActiveProfile { get; set; }
    }

    internal class ProfileConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }

        // exe name -> ProgramConfig
        public Dictionary<string, ProgramConfig> Programs { get; set; }
            = new Dictionary<string, ProgramConfig>();
    }

    internal class ProgramConfig
    {
        public List<RemapRule> Remaps { get; set; } = new List<RemapRule>();
    }

    internal class RemapRule
    {
        public string From { get; set; }
        public string To { get; set; }
    }
}
