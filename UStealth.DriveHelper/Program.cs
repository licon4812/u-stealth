using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text.Json;
using System.Management;
using System.Collections.Generic;

namespace UStealth.DriveHelper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: UStealth.DriveHelper <toggleboot|readboot|writeboot|listdrives> [args...]");
                return 100;
            }

            string command = args[0].ToLowerInvariant();
            string device = args.Length > 1 ? args[1] : null;

            try
            {
                switch (command)
                {
                    case "toggleboot":
                        return ToggleBoot(device);
                    case "readboot":
                        return ReadBoot(device);
                    case "writeboot":
                        if (args.Length >= 3) return WriteBoot(device, args[2]);
                        Console.Error.WriteLine("Missing data argument for writeboot.");
                        return 101;
                    case "listdrives":
                        return ListDrives();
                    default:
                        Console.Error.WriteLine($"Unknown command: {command}");
                        return 102;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 110;
            }
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
            var drives = new List<object>();
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

                    drives.Add(new {
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
                Console.WriteLine(JsonSerializer.Serialize(drives));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
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
            int numBytesToWrite, out int numBytesWritten, IntPtr overlapped_MustBeZero);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, 
            byte[] lpInBuffer, int nInBufferSize, byte[] lpOutBuffer, int nOutBufferSize, 
            out int lpBytesReturned, IntPtr lpOverlapped);

        static byte[]? ReadBootSector(string device)
        {
            uint GENERIC_READ = 0x80000000;
            uint OPEN_EXISTING = 3;
            try
            {
                using var handle = CreateFile(device, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (handle.IsInvalid)
                    return null;
                int offset = 0;
                byte[] buf = new byte[512];
                int read = 0;
                int moveToHigh;
                SetFilePointer(handle, offset, out moveToHigh, 0);
                ReadFile(handle, buf, 512, out read, IntPtr.Zero);
                return buf;
            }
            catch
            {
                return null;
            }
        }

        static int WriteBootSector(string device, byte[] bufToWrite)
        {
            uint GENERIC_WRITE = 0x40000000;
            uint FSCTL_LOCK_VOLUME = 0x00090018;
            uint OPEN_EXISTING = 3;
            int intOut;
            using var handle = CreateFile(device, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle.IsInvalid)
                return 1;
            bool success = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
            if (!success)
                return 2;
            int offset = 0;
            int bytesWritten = 0;
            int moveToHigh;
            SetFilePointer(handle, offset, out moveToHigh, 0);
            WriteFile(handle, bufToWrite, bufToWrite.Length, out bytesWritten, IntPtr.Zero);
            // Verify
            var bufVerify = ReadBootSector(device);
            int lastIndex = Math.Min(bufVerify?.Length ?? 0, bufToWrite.Length) - 1;
            if (lastIndex >= 0 && bufVerify[lastIndex] == bufToWrite[lastIndex]) return 99;
            return 3;
        }
    }
}
