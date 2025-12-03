using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotKeyDemo2
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ApplicationExit += OnAppExit;
            KeyboardHook.Install();
            Application.Run(new Form6());
        }
        private static void OnAppExit(object sender, EventArgs e)
        {
            // Make sure the global hook from Form5 is removed
            KeyboardHook.Uninstall();
            Form5.UninstallHookSafe();
        }
    }
}
