using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    /// <summary>
    /// Loads config.json and returns per-program key mappings.
    /// </summary>
    internal static class ProgramChecker
    {
        private static RootConfig _rootConfig;
        private static ProfileConfig _activeProfile;

        /// <summary>
        /// Result: From key -> sequence of actions.
        /// </summary>
        internal class ProgramMapping
        {
            public Dictionary<Keys, List<RemapAction>> Sequences
                = new Dictionary<Keys, List<RemapAction>>();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint lpdwProcessId);

        public static void LoadConfig(string path = "config.json")
        {
            string fullPath = Path.GetFullPath(path);
            Debug.WriteLine("[ProgramChecker] Loading: " + fullPath);

            if (!File.Exists(path))
            {
                Debug.WriteLine("[ProgramChecker] config.json not found.");
                _rootConfig = new RootConfig();
                _activeProfile = null;
                return;
            }

            string json = File.ReadAllText(path);
            Debug.WriteLine("[ProgramChecker] Raw JSON length: " + json.Length);
            //Debug.WriteLine("[ProgramChecker] Raw JSON       : " + json);
            TryDeserializeConfig(json);
            ResolveActiveProfile();
        }

        /// <summary>
        /// Returns mapping for the foreground process (or empty mapping).
        /// Called on each key down.
        /// </summary>
        public static ProgramMapping GetMappingForCurrentProgram()
        {
            ProgramMapping result = new ProgramMapping();

            if (_activeProfile == null)
                return result;

            string exeName = GetForegroundExeName();
            if (exeName == null)
                return result;

            exeName = exeName.ToLowerInvariant();
            Debug.WriteLine("[ProgramChecker] Foreground EXE: " + exeName);

            ProgramConfig programConfig = FindProgramConfig(exeName);
            if (programConfig == null)
                return result;

            BuildMappingFromProgram(result, programConfig);
            return result;
        }

        // ===== Helpers =====

        private static void TryDeserializeConfig(string json)
        {
            try
            {
                _rootConfig = JsonSerializer.Deserialize<RootConfig>(json);


                //------test: pretty print deserialized config-----
                //if (_rootConfig != null)
                //{
                //    string pretty = JsonSerializer.Serialize(
                //        _rootConfig,
                //        new JsonSerializerOptions { WriteIndented = true });

                //    Debug.WriteLine("[ProgramChecker] Deserialized config:\n" + pretty);
                //}
                //------end test------------------------------------
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ProgramChecker] Failed to deserialize: " + ex.Message);
                _rootConfig = new RootConfig();
            }
            if (_rootConfig == null)
            {
                _rootConfig = new RootConfig();
            }
        }

        private static void ResolveActiveProfile()
        {
            if (string.IsNullOrEmpty(_rootConfig.ActiveProfile) ||
                !_rootConfig.Profiles.TryGetValue(_rootConfig.ActiveProfile, out _activeProfile))
            {
                Debug.WriteLine("[ProgramChecker] ActiveProfile not set or not found. Using first profile if any.");

                _activeProfile = null;
                foreach (KeyValuePair<string, ProfileConfig> kv in _rootConfig.Profiles)
                {
                    _activeProfile = kv.Value;
                    Debug.WriteLine("[ProgramChecker] Using profile: " + kv.Key);
                    break;
                }
            }
            else
            {
                Debug.WriteLine("[ProgramChecker] Active profile: " + _rootConfig.ActiveProfile);
                //Debug.WriteLine("[ProgramChecker] _activeProfile: " + _activeProfile);
            }
        }

        private static ProgramConfig FindProgramConfig(string exeName)
        {
            ProgramConfig programConfig = null;

            // exact match (no wildcard)
            foreach (KeyValuePair<string, ProgramConfig> kv in _activeProfile.Programs)
            {
                if (!string.IsNullOrEmpty(kv.Key) &&
                    !kv.Key.Contains("*") &&
                    string.Equals(kv.Key, exeName, StringComparison.OrdinalIgnoreCase))
                {
                    programConfig = kv.Value;
                    Debug.WriteLine("[ProgramChecker] Matched program: " + kv.Key);
                    break;
                }
            }

            // fallback: "*.exe"
            if (programConfig == null)
            {
                ProgramConfig wildcard;
                if (_activeProfile.Programs.TryGetValue("*.exe", out wildcard))
                {
                    programConfig = wildcard;
                    Debug.WriteLine("[ProgramChecker] Using wildcard profile: *.exe");
                }
            }

            return programConfig;
        }

        private static void BuildMappingFromProgram(ProgramMapping result, ProgramConfig programConfig)
        {
            foreach (RemapRule rule in programConfig.Remaps)
            {
                if (string.IsNullOrEmpty(rule.From))
                    continue;

                Keys fromKey;
                if (!Enum.TryParse(rule.From, out fromKey))
                {
                    Debug.WriteLine("[ProgramChecker] Invalid From key: " + rule.From);
                    continue;
                }

                // Overwrite if duplicate; last rule wins.
                result.Sequences[fromKey] = rule.To ?? new List<RemapAction>();
            }
        }

        private static string GetForegroundExeName()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return null;

            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == 0)
                return null;

            try
            {
                using (Process proc = Process.GetProcessById((int)pid))
                {
                    string name = proc.ProcessName;
                    if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        name += ".exe";

                    return name;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
