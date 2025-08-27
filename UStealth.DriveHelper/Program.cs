using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text.Json;
using System.Management;
using System.Collections.Generic;
using Spectre.Console;
using System.Text.Json.Serialization;

namespace UStealth.DriveHelper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                while (true)
                {
                    // Interactive Spectre.Console menu
                    var command = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]Select a command[/]")
                            .AddChoices(new[] { "toggleboot", "readboot", "writeboot", "listdrives", "exit" })
                    );

                    if (command == "exit")
                        break;

                    string device = null;
                    string hexData = null;

                    if (command is "toggleboot" or "readboot" or "writeboot")
                    {
                        // List drives and let user select
                        var drives = GetDrivesForPrompt();
                        if (drives.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[red]No drives found.[/]");
                            continue;
                        }
                        device = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[green]Select a device[/]")
                                .AddChoices(drives)
                        );
                        // Extract deviceId from label
                        device = device.Split(' ')[0];
                    }
                    if (command == "writeboot")
                    {
                        hexData = AnsiConsole.Ask<string>("[green]Enter hex data to write to boot sector[/]");
                    }

                    // Call the appropriate method
                    int result = 0;
                    switch (command)
                    {
                        case "toggleboot":
                            result = ToggleBoot(device);
                            break;
                        case "readboot":
                            result = ReadBoot(device);
                            break;
                        case "writeboot":
                            result = WriteBoot(device, hexData);
                            break;
                        case "listdrives":
                            // Interactive Spectre.Console table view
                            var drives = new List<DriveInfoDisplay>();
                            try
                            {
                                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                                foreach (ManagementObject drive in searcher.Get())
                                {
                                    string deviceId = drive["DeviceID"]?.ToString();
                                    string model = drive["Model"]?.ToString();
                                    string interfaceType = drive["InterfaceType"]?.ToString();
                                    string mediaType = drive["MediaType"]?.ToString();
                                    string size = drive["Size"]?.ToString();
                                    string status = "*UNKNOWN*";
                                    string driveLetters = "";
                                    string volLabel = null;
                                    string format = null;
                                    try
                                    {
                                        foreach (ManagementObject partition in drive.GetRelated("Win32_DiskPartition"))
                                        {
                                            foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
                                            {
                                                if (!string.IsNullOrEmpty(driveLetters))
                                                    driveLetters += ", ";
                                                driveLetters += logicalDisk["DeviceID"]?.ToString();
                                                volLabel = logicalDisk["VolumeName"]?.ToString();
                                                format = logicalDisk["FileSystem"]?.ToString();
                                            }
                                        }
                                    }
                                    catch { }
                                    var bufR = ReadBootSector(deviceId);
                                    if (bufR == null)
                                        status = "*UNKNOWN*";
                                    else
                                        status = bufR[511] switch { 170 => "NORMAL", 171 => "HIDDEN", _ => "*UNKNOWN*" };
                                    drives.Add(new DriveInfoDisplay {
                                        DeviceID = deviceId,
                                        Model = model,
                                        Interface = interfaceType,
                                        MediaType = mediaType,
                                        Size = size,
                                        VolumeLabel = volLabel,
                                        Format = format,
                                        DriveLetter = driveLetters,
                                        Status = status
                                    });
                                }
                                // Print as Spectre.Console table
                                var table = new Table();
                                table.AddColumn("DeviceID");
                                table.AddColumn("Model");
                                table.AddColumn("Interface");
                                table.AddColumn("MediaType");
                                table.AddColumn("Size");
                                table.AddColumn("VolumeLabel");
                                table.AddColumn("Format");
                                table.AddColumn("DriveLetter");
                                table.AddColumn("Status");
                                foreach (var d in drives)
                                {
                                    table.AddRow(
                                        d.DeviceID ?? "",
                                        d.Model ?? "",
                                        d.Interface ?? "",
                                        d.MediaType ?? "",
                                        FormatSize(d.Size),
                                        d.VolumeLabel ?? "",
                                        d.Format ?? "",
                                        d.DriveLetter ?? "",
                                        d.Status ?? ""
                                    );
                                }
                                AnsiConsole.Write(table);
                                // skip the rest of the loop
                                continue;
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                                continue;
                            }
                        default:
                            AnsiConsole.MarkupLine($"[red]Unknown command: {command}[/]");
                            continue;
                    }
                    AnsiConsole.MarkupLine($"[grey]Command finished with code {result}[/]");
                    if (!AnsiConsole.Confirm("Do you want to perform another action?", true))
                        break;
                }
                return 0;
            }

            string commandArg = args[0].ToLowerInvariant();
            string deviceArg = args.Length > 1 ? args[1] : null;

            try
            {
                switch (commandArg)
                {
                    case "toggleboot":
                        return ToggleBoot(deviceArg);
                    case "readboot":
                        return ReadBoot(deviceArg);
                    case "writeboot":
                        if (args.Length >= 3) return WriteBoot(deviceArg, args[2]);
                        Console.Error.WriteLine("Missing data argument for writeboot.");
                        return 101;
                    case "listdrives":
                        return ListDrives();
                    default:
                        Console.Error.WriteLine($"Unknown command: {commandArg}");
                        return 102;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 110;
            }
        }

        private static List<string> GetDrivesForPrompt()
        {
            var drives = new List<string>();
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (System.Management.ManagementObject drive in searcher.Get())
                {
                    string deviceId = drive["DeviceID"]?.ToString();
                    string model = drive["Model"]?.ToString();
                    string size = drive["Size"]?.ToString();
                    string label = $"{deviceId} ({model}, {FormatSize(size)})";
                    drives.Add(label);
                }
            }
            catch { }
            return drives;
        }

        private static string FormatSize(string sizeStr)
        {
            if (long.TryParse(sizeStr, out long size))
            {
                if (size > 999999999999) return $"{size / 1_000_000_000_000.0:F1} TB";
                if (size > 999999999) return $"{size / 1_000_000_000.0:F1} GB";
                if (size > 999999) return $"{size / 1_000_000.0:F1} MB";
                if (size > 999) return $"{size / 1_000.0:F1} KB";
                return size.ToString();
            }
            return sizeStr;
        }

        // Example: Toggle boot sector signature between 0xAA and 0xAB
        static int ToggleBoot(string device)
        {
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

        static int ReadBoot(string device)
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

        static int WriteBoot(string device, string hexData)
        {
            try
            {
                var buf = Convert.FromHexString(hexData);
                int res = WriteBootSector(device, buf);
                Console.WriteLine(res == 99 ? "OK" : "FAILED");
                return res;
            }
            catch
            {
                Console.Error.WriteLine("Invalid hex data.");
                return 2;
            }
        }

        static int ListDrives()
        {
            var drives = new List<DriveInfoDisplay>();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (ManagementObject drive in searcher.Get())
                {
                    string deviceId = drive["DeviceID"]?.ToString();
                    string model = drive["Model"]?.ToString();
                    string interfaceType = drive["InterfaceType"]?.ToString();
                    string mediaType = drive["MediaType"]?.ToString();
                    string size = drive["Size"]?.ToString();
                    string status = "*UNKNOWN*";
                    string driveLetters = "";
                    string volLabel = null;
                    string format = null;

                    // Find drive letters and volume info
                    try
                    {
                        foreach (ManagementObject partition in drive.GetRelated("Win32_DiskPartition"))
                        {
                            foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
                            {
                                if (!string.IsNullOrEmpty(driveLetters))
                                    driveLetters += ", ";
                                driveLetters += logicalDisk["DeviceID"]?.ToString();
                                volLabel = logicalDisk["VolumeName"]?.ToString();
                                format = logicalDisk["FileSystem"]?.ToString();
                            }
                        }
                    }
                    catch { }

                    // Boot sector status
                    var bufR = ReadBootSector(deviceId);
                    if (bufR == null)
                        status = "*UNKNOWN*";
                    else
                        status = bufR[511] switch { 170 => "NORMAL", 171 => "HIDDEN", _ => "*UNKNOWN*" };

                    drives.Add(new DriveInfoDisplay {
                        DeviceID = deviceId,
                        Model = model,
                        Interface = interfaceType,
                        MediaType = mediaType,
                        Size = FormatSize(size),
                        VolumeLabel = volLabel,
                        Format = format,
                        DriveLetter = driveLetters,
                        Status = status
                    });
                }
                Console.WriteLine(JsonSerializer.Serialize(drives, DriveInfoDisplayJsonContext.Default.ListDriveInfoDisplay));
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 111;
            }
        }

        // Native methods and helpers (copy from your WinForms logic)
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint SetFilePointer(
            [In] SafeFileHandle hFile,
            [In] int lDistanceToMove,
            [Out] out int lpDistanceToMoveHigh,
            [In] uint dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
          uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
          uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        internal extern static int ReadFile(SafeFileHandle handle, byte[] bytes,
           int numBytesToRead, out int numBytesRead, IntPtr overlapped_MustBeZero);

        [DllImport("kernel32", SetLastError = true)]
        internal extern static int WriteFile(SafeFileHandle handle, byte[] bytes, 
            int numBytesToWrite, out int numBytesWritten, IntPtr overlapped_MustBe_ZERO);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, 
            byte[] lpInBuffer, int nInBufferSize, byte[] lpOutBuffer, int nOutBufferSize, 
            out int lpBytesReturned, IntPtr lpOverlapped);

        static byte[]? ReadBootSector(string device)
        {
            uint GENERIC_READ = 0x80000000;
            uint OPEN_EXISTING = 3;
            Console.Error.WriteLine($"[ReadBootSector] Opening device: {device}");
            try
            {
                using var handle = CreateFile(device, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                Console.Error.WriteLine($"[ReadBootSector] Handle valid: {!handle.IsInvalid}");
                if (handle.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"[ReadBootSector] CreateFile failed. Win32Error: {err}");
                    return null;
                }
                int offset = 0;
                byte[] buf = new byte[512];
                int read = 0;
                int moveToHigh;
                SetFilePointer(handle, offset, out moveToHigh, 0);
                ReadFile(handle, buf, 512, out read, IntPtr.Zero);
                Console.Error.WriteLine($"[ReadBootSector] Read {read} bytes");
                return buf;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ReadBootSector] Exception: {ex.Message}");
                return null;
            }
        }

        static int WriteBootSector(string device, byte[] bufToWrite)
        {
            uint GENERIC_WRITE = 0x40000000;
            uint FSCTL_LOCK_VOLUME = 0x00090018;
            uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
            uint OPEN_EXISTING = 3;
            Console.Error.WriteLine($"[WriteBootSector] Opening device: {device}");
            int intOut;
            // Write and unlock in using block
            using (var handle = CreateFile(device, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                Console.Error.WriteLine($"[WriteBootSector] Handle valid: {!handle.IsInvalid}");
                if (handle.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"[WriteBootSector] CreateFile failed. Win32Error: {err}");
                    return 1;
                }
                bool success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
                Console.Error.WriteLine($"[WriteBootSector] Lock success: {success}");
                if (!success)
                    return 2;
                int offset = 0;
                int bytesWritten = 0;
                int moveToHigh;
                SetFilePointer(handle, offset, out moveToHigh, 0);
                WriteFile(handle, bufToWrite, bufToWrite.Length, out bytesWritten, IntPtr.Zero);
                Console.Error.WriteLine($"[WriteBootSector] Wrote {bytesWritten} bytes");
                // Unlock the volume before closing the handle
                DeviceIoControl(handle, FSCTL_UNLOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
            }
            // Now the handle is closed, try to re-open for reading
            const int maxTries = 20;
            const int delayMs = 500;
            byte[] bufVerify = null;
            for (int i = 0; i < maxTries; i++)
            {
                System.Threading.Thread.Sleep(delayMs);
                bufVerify = ReadBootSector(device);
                if (bufVerify != null)
                    break;
            }
            int lastIndex = Math.Min(bufVerify?.Length ?? 0, bufToWrite.Length) - 1;
            if (lastIndex >= 0 && bufVerify[lastIndex] == bufToWrite[lastIndex]) return 99;
            return 3;
        }

        // Replace the use of 'dynamic' with a strongly-typed class for drive info
        // Make DriveInfoDisplay public so it can be used by the source generator
        public class DriveInfoDisplay
        {
            public string DeviceID { get; set; }
            public string Model { get; set; }
            public string Interface { get; set; }
            public string MediaType { get; set; }
            public string Size { get; set; }
            public string VolumeLabel { get; set; }
            public string Format { get; set; }
            public string DriveLetter { get; set; }
            public string Status { get; set; }
        }
    }

    [JsonSerializable(typeof(List<Program.DriveInfoDisplay>))]
    internal partial class DriveInfoDisplayJsonContext : JsonSerializerContext { }
}
