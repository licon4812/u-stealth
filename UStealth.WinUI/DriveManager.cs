using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
            string sysDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
            string sysDriveLetter = sysDrive.TrimEnd('\\');
            foreach (var drive in DriveInfo.GetDrives())
            {
                string driveLetter = drive.Name.TrimEnd('\\');
                string strIsSys = driveLetter.Equals(sysDriveLetter, StringComparison.OrdinalIgnoreCase) ? "*SYSTEM*" : "";
                string strInt = "N/A";
                string strMod = "N/A";
                string strMed = drive.DriveType.ToString(); 
                string volLabel = drive.VolumeLabel;
                string format = drive.DriveFormat;
                string strDev = drive.Name;
                string strSiz = drive.IsReady ?
                    (drive.TotalSize switch
                    {
                        > 999999999999 => Math.Round((drive.TotalSize / 1000000000000.0), 1) + " TB",
                        > 999999999 => Math.Round((drive.TotalSize / 1000000000.0), 1) + " GB",
                        > 999999 => Math.Round((drive.TotalSize / 1000000.0), 1) + " MB",
                        > 999 => Math.Round((drive.TotalSize / 1000.0), 1) + " KB",
                        _ => Math.Round((double)drive.TotalSize, 1).ToString(CultureInfo.InvariantCulture)
                    }) : "N/A";
                string strSta = "*UNKNOWN*";
                // Try to get boot sector status if possible
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    // Try to get the physical drive number for this logical drive
                    // This is a best-effort guess: assumes C: = PhysicalDrive0, D: = PhysicalDrive1, etc.
                    // For more accuracy, SetupAPI or DeviceIoControl with IOCTL_STORAGE_GET_DEVICE_NUMBER is needed
                    string physicalDrive = $"\\.\\PhysicalDrive{driveLetter[0] - 'C'}";
                    byte[] bufR = ReadBoot(physicalDrive);
                    if (bufR != null)
                        strSta = bufR[511] switch { 170 => "NORMAL", 171 => "HIDDEN", _ => "*UNKNOWN*" };
                }
                drives.Add(new DriveInfoModel
                {
                    SystemDrive = strIsSys,
                    DriveLetter = driveLetter,
                    Interface = strInt,
                    Model = strMod,
                    MediaType = strMed,
                    VolumeLabel = volLabel,
                    Format = format,
                    Size = strSiz,
                    Status = strSta,
                    DeviceID = drive.Name
                });
            }
            return drives;
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
}