using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace UStealth.DriveHelper
{
    internal static class Linux
    {
        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool IsRunningAdministrator()
        {
            return geteuid() == 0;
        }

        public static bool IsCliMode()
        {
            // True if running in a terminal/console, false if launched as a GUI app
            return Environment.UserInteractive;
        }

        public static void ElevateToAdministrator()
        {
            try
            {
                var processName = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(processName))
                    throw new InvalidOperationException("Cannot determine process path for elevation.");

                var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(a => "\"" + a.Replace("\"", "\\\"") + "\""));
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"{processName} {args}",
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
                Environment.Exit(123); // Exit current process after launching elevated
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Error: {e}[/]");
            }
        }
    }
}
