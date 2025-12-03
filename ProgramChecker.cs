using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    internal static class ProgramChecker
    {
        private static RootConfig _rootConfig;
        private static ProfileConfig _activeProfile;

        // Maps "From" Keys -> "To" Keys OR block
        public class ProgramMapping
        {
            public Dictionary<Keys, Keys> Remaps = new Dictionary<Keys, Keys>();
            public HashSet<Keys> BlockOnly = new HashSet<Keys>();
        }

        // Win32: foreground window -> process
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

            try
            {
                _rootConfig = JsonSerializer.Deserialize<RootConfig>(json);

                Debug.WriteLine("[ProgramChecker] _rootConfig\n: " + _rootConfig);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ProgramChecker] Failed to deserialize: " + ex.Message);
                _rootConfig = new RootConfig();
            }

            if (_rootConfig == null)
            {
                Debug.WriteLine("[ProgramChecker] Root config is null.");
                _rootConfig = new RootConfig();
            }

            if (string.IsNullOrEmpty(_rootConfig.ActiveProfile) ||                                  //chk if AP exist
                !_rootConfig.Profiles.TryGetValue(_rootConfig.ActiveProfile, out _activeProfile))   //chk if AP valid
            {
                Debug.WriteLine("[ProgramChecker] ActiveProfile not set or not found. Using first profile if any.");
                foreach (var kv in _rootConfig.Profiles)
                {
                    _activeProfile = kv.Value;
                    Debug.WriteLine("[ProgramChecker] Using profile: " + kv.Key);
                    break;
                }
            }
            else
            {
                Debug.WriteLine("[ProgramChecker] Active profile: " + _rootConfig.ActiveProfile);
            }
        }

        public static ProgramMapping GetMappingForCurrentProgram()
        {
            ProgramMapping result = new ProgramMapping();

            if (_activeProfile == null)
            {
                // No profile loaded
                return result;
            }

            string exeName = GetForegroundExeName();
            if (exeName == null)
            {
                return result;
            }

            exeName = exeName.ToLowerInvariant();
            Debug.WriteLine("[ProgramChecker] Foreground EXE: " + exeName);

            ProgramConfig programConfig = null;

            // 1) Exact match: "notepad.exe"
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

            // 2) If no exact match, try wildcard "*.exe" (very basic)
            if (programConfig == null)
            {
                foreach (KeyValuePair<string, ProgramConfig> kv in _activeProfile.Programs)
                {
                    if (kv.Key == "*.exe")
                    {
                        programConfig = kv.Value;
                        Debug.WriteLine("[ProgramChecker] Using wildcard profile: " + kv.Key);
                        break;
                    }
                }
            }

            // 3) Build mapping from ProgramConfig
            if (programConfig != null)
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

                    if (string.IsNullOrEmpty(rule.To))
                    {
                        // empty To = block this key
                        result.BlockOnly.Add(fromKey);
                    }
                    else
                    {
                        Keys toKey;
                        if (!Enum.TryParse(rule.To, out toKey))
                        {
                            Debug.WriteLine("[ProgramChecker] Invalid To key: " + rule.To);
                            continue;
                        }

                        result.Remaps[fromKey] = toKey;
                    }
                }
            }

            return result;
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
                    // ProcessName usually without .exe, so add it
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
