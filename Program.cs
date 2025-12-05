using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace HotKeyDemo2
{
    internal static class Program
    {
        //The main entry point for the application.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1) Load config.json ONCE
            ProgramChecker.LoadConfig("config.json");

            // 2) Install global hooks
            KeyboardHook.Install();
            MouseHook.Install();

            // 3) Attach your logic handlers
            KeyboardOutput.Attach();
            MouseOutput.Attach();

            Application.Run(new Form6());
        }
        private static void OnAppExit(object sender, EventArgs e)
        {
            Form5.UninstallHookSafe();

            KeyboardHook.Uninstall();
            MouseHook.Uninstall();
        }
    }
}