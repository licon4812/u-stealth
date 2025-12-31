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
            Application.SetColorMode(SystemColorMode.System);

            // Listen for system color/theme changes and update the app while running
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            Application.ApplicationExit += (_, __) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            Application.Run(new ToggleMain());
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Respond to relevant categories only
            if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color
                or UserPreferenceCategory.VisualStyle)) return;

            void Apply()
            {
                // Re-apply System color mode and refresh open forms
                Application.SetColorMode(SystemColorMode.System);
                foreach (Form f in Application.OpenForms)
                {
                    try
                    {
                        f.Invalidate(true);
                        f.Refresh();
                        f.Update();
                    }
                    catch { /* ignore repaint errors */ }
                }
            }

            // Marshal to UI thread if needed
            if (Application.OpenForms.Count > 0)
            {
                var anyForm = Application.OpenForms[0];
                if (anyForm.IsHandleCreated && anyForm.InvokeRequired)
                {
                    try { anyForm.BeginInvoke((Action)Apply); } catch { }
                }
                else
                {
                    Apply();
                }
            }
            else
            {
                // No forms yet; still re-apply app-wide setting
                Application.SetColorMode(SystemColorMode.System);
            }
        }
    }
}
