using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UStealth.WinUI
{
    public class DriveManager
    {
        public enum EMoveMethod : uint { Begin = 0, Current = 1, End = 2 }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint SetFilePointer(
            [In] SafeFileHandle hFile,
            [In] int lDistanceToMove,
            [Out] out int lpDistanceToMoveHigh,
            [In] EMoveMethod dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
          uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
          uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        internal extern static int ReadFile(SafeFileHandle handle, byte[] bytes,
           int numBytesToRead, out int numBytesRead, IntPtr overlapped_MustBeZero);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal extern static int WriteFile(SafeFileHandle handle, byte[] bytes, 
            int numBytesToWrite, out int numBytesWritten, IntPtr overlapped_MustBeZero);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, 
            byte[] lpInBuffer, int nInBufferSize, byte[] lpOutBuffer, int nOutBufferSize, 
            out int lpBytesReturned, IntPtr lpOverlapped);

        public List<DriveInfoModel> GetDriveList()
        {
            var drives = new List<DriveInfoModel>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "UStealth.DriveHelper.exe",
                    Arguments = "listdrives",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var driveList = JsonSerializer.Deserialize(output, DriveInfoModelJsonContext.Default.ListDriveInfoModel);
                    if (driveList != null)
                        drives.AddRange(driveList);
                }
                // Now get boot status for each drive
                foreach (var drive in drives)
                {
                    drive.Status = GetBootStatusFromHelper(drive.DeviceID);
                }
            }
            catch { }
            return drives;
        }

        private string GetBootStatusFromHelper(string device)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "UStealth.DriveHelper.exe",
                    Arguments = $"readboot \"{device}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                output = output?.Trim();
                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output) && output.Length >= 1024)
                {
                    // output is hex string, last two bytes are 510, 511
                    // 510 = AA, 511 = AB or AA
                    string lastByte = output.Substring(output.Length - 2, 2);
                    if (lastByte.Equals("AA", StringComparison.OrdinalIgnoreCase))
                        return "NORMAL";
                    if (lastByte.Equals("AB", StringComparison.OrdinalIgnoreCase))
                        return "HIDDEN";
                    return "*UNKNOWN*";
                }
            }
            catch { }
            return "*UNKNOWN*";
        }

        public byte[] ReadBoot(string strDev)
        {
            uint GENERIC_READ = 0x80000000;
            uint OPEN_EXISTING = 3;
            try
            {
                SafeFileHandle handleValue = CreateFile(strDev, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (handleValue.IsInvalid)
                    return null;
                int offset = 0;
                byte[] buf = new byte[512];
                int read = 0;
                int moveToHigh;
                SetFilePointer(handleValue, offset, out moveToHigh, EMoveMethod.Begin);
                ReadFile(handleValue, buf, 512, out read, IntPtr.Zero);
                handleValue.Close();
                return buf;
            }
            catch
            {
                return null;
            }
        }

        public int WriteBoot(string strDev, byte[] bufToWrite)
        {
            uint GENERIC_WRITE = 0x40000000;
            uint FSCTL_LOCK_VOLUME = 0x00090018;
            uint OPEN_EXISTING = 3;
            int intOut;
            SafeFileHandle handleValue = CreateFile(strDev, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handleValue.IsInvalid)
                return 1; // can't get disk handle
            bool success = DeviceIoControl(handleValue, FSCTL_LOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
            if (!success)
            {
                handleValue.Close();
                return 2; // can't lock the device for edit
            }
            int offset = 0;
            int bytesWritten = 0;
            int moveToHigh;
            SetFilePointer(handleValue, offset, out moveToHigh, EMoveMethod.Begin);
            WriteFile(handleValue, bufToWrite, bufToWrite.Length, out bytesWritten, IntPtr.Zero);
            handleValue.Close();
            byte[] bufVerify = ReadBoot(strDev);
            int lastIndex = Math.Min(bufVerify?.Length ?? 0, bufToWrite.Length) - 1;
            if (lastIndex >= 0 && bufVerify[lastIndex] == bufToWrite[lastIndex]) return 99; // success
            return 3; // nothing appears to have happened
        }

        public (bool? hidden, int result) ToggleBoot(DriveInfoModel drive)
        {
            var bufR = ReadBoot(drive.DeviceID);
            if (bufR == null) return (null, 0);
            // 55AA = normal, 55AB = hidden
            if (bufR[510] == 0x55 && bufR[511] == 0xAA)
            {
                bufR[511] = 0xAB;
                int res = WriteBoot(drive.DeviceID, bufR);
                return (true, res);
            }
            else if (bufR[510] == 0x55 && bufR[511] == 0xAB)
            {
                bufR[511] = 0xAA;
                int res = WriteBoot(drive.DeviceID, bufR);
                return (false, res);
            }
            return (null, 0);
        }
    }

    // Add a partial context class for source generation
    [JsonSerializable(typeof(List<DriveInfoModel>))]
    internal partial class DriveInfoModelJsonContext : JsonSerializerContext
    {
    }
}