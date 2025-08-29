using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using static UStealth.DriveHelper.Program;

namespace UStealth.DriveHelper
{
    internal static class Linux
    {
        internal static DriveInfoDisplay? SystemDrive { get; set; } = FindSystemDrive();

        private static DriveInfoDisplay? FindSystemDrive()
        {
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
                    return null;
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var lsblkOutput = System.Text.Json.JsonDocument.Parse(output);

                static bool IsSystemMount(string? mount) =>
                    mount == "/" || mount == "/mnt/wslg/distro";

                if (lsblkOutput.RootElement.TryGetProperty("blockdevices", out var blockDevices))
                {
                    foreach (var device in blockDevices.EnumerateArray())
                    {
                        // Check if device itself is mounted at / or /mnt/wslg/distro
                        if (device.TryGetProperty("mountpoint", out var mp) && IsSystemMount(mp.GetString()))
                        {
                            return new DriveInfoDisplay
                            {
                                IsSystemDrive = true,
                                DeviceID = device.GetProperty("name").GetString() ?? "Unknown",
                                Size = device.GetProperty("size").GetString() ?? "Unknown",
                                MediaType = device.GetProperty("type").GetString() ?? "Unknown",
                                DriveLetter = mp.GetString() ?? "",
                                Format = device.TryGetProperty("fstype", out var fs) ? fs.GetString() ?? "" : "",
                                VolumeLabel = device.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "",
                                Model = device.TryGetProperty("model", out var mdl) ? mdl.GetString() ?? "" : "",
                                Status = "*SYSTEM*",
                                Interface = device.TryGetProperty("tran", out var trn) ? trn.GetString() ?? "" : ""
                            };
                        }
                        // Check children (partitions)
                        if (device.TryGetProperty("children", out var children))
                        {
                            foreach (var child in children.EnumerateArray())
                            {
                                if (child.TryGetProperty("mountpoint", out var cmp) && IsSystemMount(cmp.GetString()))
                                {
                                    return new DriveInfoDisplay
                                    {
                                        IsSystemDrive = true,
                                        DeviceID = device.GetProperty("name").GetString() ?? "Unknown",
                                        Size = device.GetProperty("size").GetString() ?? "Unknown",
                                        MediaType = device.GetProperty("type").GetString() ?? "Unknown",
                                        DriveLetter = cmp.GetString() ?? "",
                                        Format = child.TryGetProperty("fstype", out var fs) ? fs.GetString() ?? "" : "",
                                        VolumeLabel = child.TryGetProperty("label", out var lbl) ? lbl.GetString() ?? "" : "",
                                        Model = device.TryGetProperty("model", out var mdl) ? mdl.GetString() ?? "" : "",
                                        Status = "*SYSTEM*",
                                        Interface = device.TryGetProperty("tran", out var trn) ? trn.GetString() ?? "" : ""
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors, return null
            }
            return null;
        }

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

        public static List<DriveInfoDisplay> GetDrives()
        {
            var drives = new List<DriveInfoDisplay>();
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
                        var drive = new DriveInfoDisplay
                        {
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
                        drive.IsSystemDrive = drive.DeviceID == SystemDrive?.DeviceID;
                        // Boot sector status
                        var bufR = ReadBootSector(drive.DeviceID);
                        if (bufR == null)
                            drive.Status = "*UNKNOWN*";
                        else
                            drive.Status = bufR[511] switch { 170 => "NORMAL", 171 => "HIDDEN", _ => "*UNKNOWN*" };
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

        internal static List<string> GetDrivesForPrompt()
        {
            var drives = new List<string>();
            try
            {
                var diskDrives = GetDrives();
                foreach (var drive in diskDrives)
                {
                    string deviceId = drive.DeviceID;
                    string model = drive.Model;
                    string size = drive.Size;
                    string label = $"{deviceId} ({model}, {FormatSize(size)})";
                    drives.Add(label);
                }
            }
            catch { }
            return drives;
        }

        /// <summary>
        /// Lists drives and outputs their details as JSON.
        /// </summary>
        /// <returns></returns>
        internal static int ListDrives()
        {

            try
            {
                var drives = GetDrives();
                Console.WriteLine(JsonSerializer.Serialize(drives, DriveInfoDisplayJsonContext.Default.ListDriveInfoDisplay));
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 111;
            }
        }

        internal static int ReadBoot(string device)
        {
            var buf = ReadBootSector(device);
            if (buf == null)
            {
                Console.Error.WriteLine("Failed to read boot sector.");
                return 1;
            }
            Console.WriteLine(Convert.ToHexString(buf));
            return 0;
        }
        private static byte[]? ReadBootSector(string deviceId)
        {
            // deviceId is usually like "sda", so prepend /dev/
            var path = deviceId.StartsWith("/dev/") ? deviceId : $"/dev/{deviceId}";
            try
            {
                using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                byte[] buffer = new byte[512];
                int read = fs.Read(buffer, 0, buffer.Length);
                if (read == 512)
                    return buffer;
                // Partial read, treat as error
                return null;
            }
            catch (Exception)
            {
                // Could not open/read device (likely not root or device busy)
                return null;
            }
        }

        internal static int ToggleBoot(string? device)
        {
            if (device == SystemDrive?.DeviceID)
            {
                Console.Error.WriteLine("You cannot make changes to the System drive!");
                return 4;
            }
            var buf = ReadBootSector(device);
            if (buf == null)
            {
                Console.Error.WriteLine("Failed to read boot sector.");
                return 1;
            }
            switch (buf[510])
            {
                case 0x55 when buf[511] == 0xAA:
                {
                    buf[511] = 0xAB;
                    int res = WriteBootSector(device, buf);
                    Console.WriteLine(res == 99 ? "HIDDEN" : "FAILED");
                    return res;
                }
                case 0x55 when buf[511] == 0xAB:
                {
                    buf[511] = 0xAA;
                    int res = WriteBootSector(device, buf);
                    Console.WriteLine(res == 99 ? "UNHIDDEN" : "FAILED");
                    return res;
                }
                default:
                    Console.Error.WriteLine("Unknown boot signature.");
                    return 3;
            }
        }

        private static int WriteBootSector(string device, byte[] buf)
        {
            // device is usually like "sda", so prepend /dev/
            var path = device.StartsWith("/dev/") ? device : $"/dev/{device}";
            try
            {
                using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
                fs.Seek(0, System.IO.SeekOrigin.Begin);
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
                // Verify what was written
                fs.Seek(0, System.IO.SeekOrigin.Begin);
                byte[] verify = new byte[buf.Length];
                int read = fs.Read(verify, 0, verify.Length);
                if (read == buf.Length && verify.SequenceEqual(buf))
                    return 99; // success
                return 3; // nothing appears to have happened
            }
            catch (Exception)
            {
                // Could not open/write device (likely not root or device busy)
                return 1;
            }
        }
    }
}
