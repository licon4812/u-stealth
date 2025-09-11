using Microsoft.Win32.SafeHandles;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UStealth.DriveHelper.Models;
using WmiLight;
using static UStealth.DriveHelper.Program;

namespace UStealth.DriveHelper
{
    public static class Windows
    {
        internal static DriveInfoDisplay? SystemDrive { get; set; } = FindSystemDrive();

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

        /// <summary>
        /// Windows implementation of checking to see if the current user is running as Administrator.
        /// </summary>
        /// <returns></returns>
        internal static bool IsRunningAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Windows implementation of elevating the current process to run as Administrator.
        /// </summary>
        public static void ElevateToAdministrator()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath ?? Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to elevate: {ex.Message}[/]");
            }
        }

        /// <summary>
        /// Windows implementation of Getting a formatted size string from bytes. Used to select a drive
        /// </summary>
        /// <returns></returns>
        internal static List<string> GetDrivesForPrompt()
        {
            var drives = new List<string>();
            try
            {
                using var connection = new WmiConnection();
                var diskDrives = connection.CreateQuery("SELECT DeviceID, Model, Size FROM Win32_DiskDrive");
                foreach (var drive in diskDrives)
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

        /// <summary>
        /// Finds the system drive and returns its details.
        /// </summary>
        /// <returns></returns>
        private static DriveInfoDisplay? FindSystemDrive()
        {
            string sysDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
            using var connection = new WmiConnection();
            var diskDrives = connection.CreateQuery("SELECT * FROM Win32_DiskDrive");
            foreach (var drive in diskDrives)
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
                bool systemDrive = false;

                try
                {
                    var partitionQuery = connection.CreateQuery($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                    foreach (var partition in partitionQuery)
                    {
                        var logicalQuery = connection.CreateQuery($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["deviceId"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        foreach (var logicalDisk in logicalQuery)
                        {
                            if (!string.IsNullOrEmpty(driveLetters))
                                driveLetters += ", ";
                            driveLetters += logicalDisk["DeviceID"]?.ToString();
                            if (driveLetters == sysDrive)
                            {
                                systemDrive = true;
                            }
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

                if (systemDrive)
                {
                    return new DriveInfoDisplay
                    {
                        IsSystemDrive = true,
                        DeviceID = deviceId,
                        Model = model,
                        Interface = interfaceType,
                        MediaType = mediaType,
                        Size = FormatSize(size),
                        VolumeLabel = volLabel,
                        Format = format,
                        DriveLetter = driveLetters,
                        Status = status
                    };
                }
            }
            return null;
        }

        /// <summary>
        /// Windows implementation of toggling the boot sector hidden flag on a given device.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        internal static int ToggleBoot(string device)
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

        /// <summary>
        /// Windows implementation of reading and outputting the boot sector of a given device as hex.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets a list of drives with their details.
        /// </summary>
        /// <returns></returns>
        internal static List<DriveInfoDisplay> GetDrives()
        {
            var drives = new List<DriveInfoDisplay>();
            string sysDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
            using var connection = new WmiConnection();
            var diskDrives = connection.CreateQuery("SELECT * FROM Win32_DiskDrive");
            foreach (var drive in diskDrives)
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
                bool systemDrive = false;

                // Find drive letters and volume info
                try
                {
                    var partitionQuery = connection.CreateQuery($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                    foreach (var partition in partitionQuery)
                    {
                        var logicalQuery = connection.CreateQuery($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["deviceId"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        foreach (var logicalDisk in logicalQuery)
                        {
                            if (!string.IsNullOrEmpty(driveLetters))
                                driveLetters += ", ";
                            driveLetters += logicalDisk["DeviceID"]?.ToString();
                            if (driveLetters == sysDrive)
                            {
                                systemDrive = true;
                            }
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

                drives.Add(new DriveInfoDisplay
                {
                    IsSystemDrive = systemDrive,
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
            return drives;
        }

        /// <summary>
        ///  Windows implementation of reading the boot sector of a given device.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        static byte[]? ReadBootSector(string device)
        {
            uint GENERIC_READ = 0x80000000;
            uint OPEN_EXISTING = 3;
            // Console.Error.WriteLine($"[ReadBootSector] Opening device: {device}");
            try
            {
                using var handle = CreateFile(device, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                // Console.Error.WriteLine($"[ReadBootSector] Handle valid: {!handle.IsInvalid}");
                if (handle.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    // Console.Error.WriteLine($"[ReadBootSector] CreateFile failed. Win32Error: {err}");
                    return null;
                }
                int offset = 0;
                byte[] buf = new byte[512];
                int read = 0;
                int moveToHigh;
                SetFilePointer(handle, offset, out moveToHigh, 0);
                ReadFile(handle, buf, 512, out read, IntPtr.Zero);
                // Console.Error.WriteLine($"[ReadBootSector] Read {read} bytes");
                return buf;
            }
            catch (Exception ex)
            {
                // Console.Error.WriteLine($"[ReadBootSector] Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Windows implementation of writing a modified boot sector to a given device.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufToWrite"></param>
        /// <returns></returns>
        static int WriteBootSector(string device, byte[] bufToWrite)
        {
            uint GENERIC_WRITE = 0x40000000;
            uint FSCTL_LOCK_VOLUME = 0x00090018;
            uint FSCTL_UNLOCK_VOLUME = 0x0009001C;
            uint OPEN_EXISTING = 3;
            // Console.Error.WriteLine($"[WriteBootSector] Opening device: {device}");
            int intOut;
            // Write and unlock in using block
            using (var handle = CreateFile(device, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
            {
                // Console.Error.WriteLine($"[WriteBootSector] Handle valid: {!handle.IsInvalid}");
                if (handle.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    // Console.Error.WriteLine($"[WriteBootSector] CreateFile failed. Win32Error: {err}");
                    return 1;
                }
                bool success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
                // Console.Error.WriteLine($"[WriteBootSector] Lock success: {success}");
                if (!success)
                    return 2;
                int offset = 0;
                int bytesWritten = 0;
                int moveToHigh;
                SetFilePointer(handle, offset, out moveToHigh, 0);
                WriteFile(handle, bufToWrite, bufToWrite.Length, out bytesWritten, IntPtr.Zero);
                // Console.Error.WriteLine($"[WriteBootSector] Wrote {bytesWritten} bytes");
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
    }
}
