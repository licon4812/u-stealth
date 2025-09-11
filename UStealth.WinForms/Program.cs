using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UStealth
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8f));
            if (IsDarkModeAvailable())
            {
                Application.SetColorMode(SystemColorMode.Dark);
            }
            Application.Run(new ToggleMain());
        }

        /// <summary>
        /// Checks if dark mode is available (system theme is dark).
        /// </summary>
        public static bool IsDarkModeAvailable()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                object value = key?.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = dark mode, 1 = light mode
                }
            }
            catch { }
            return false;
        }
    }
}
