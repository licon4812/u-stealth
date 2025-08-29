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

        public static List<Program.DriveInfoDisplay> GetDrives()
        {
            var drives = new List<Program.DriveInfoDisplay>();
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "lsblk",
                    Arguments = "-o NAME,SIZE,TYPE,MOUNTPOINT,FSTYPE,LABEL,MODEL,VENDOR,TRAN -J",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                    throw new InvalidOperationException("Failed to start lsblk process.");
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var lsblkOutput = System.Text.Json.JsonDocument.Parse(output);
                if (lsblkOutput.RootElement.TryGetProperty("blockdevices", out var blockDevices))
                {
                    foreach (var device in blockDevices.EnumerateArray().Where(device => device.GetProperty("type").GetString() == "disk"))
                    {
                        var drive = new Program.DriveInfoDisplay
                        {
                            IsSystemDrive = false,
                            DeviceID = device.GetProperty("name").GetString() ?? "Unknown",
                            Size = device.GetProperty("size").GetString() ?? "Unknown",
                            MediaType = device.GetProperty("type").GetString() ?? "Unknown",
                            DriveLetter = device.TryGetProperty("mountpoint", out var mp) ? $@"{mp.GetString()}" ?? "" : "",
                            Format = device.TryGetProperty("fstype", out var fs) ? fs.GetString() ?? "" : "",
                            VolumeLabel = device.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "",
                            Model = device.TryGetProperty("model", out var mdl) ? $"{mdl.GetString()}" ?? "" : "",
                            Status = "*UNKNOWN*",
                            Interface = device.TryGetProperty("tran", out var trn) ? trn.GetString() ?? "" : ""
                        };
                        drives.Add(drive);
                    }
                }
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Error retrieving drives: {e}[/]");
            }
            return drives;
        }
    }
}
