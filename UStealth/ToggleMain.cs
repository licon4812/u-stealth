using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace UStealth
{
    public partial class ToggleMain : Form
    {

        public ToggleMain()
        {
            InitializeComponent();
            PopulateDriveListBox();
        }

        ///////////////////////////////////
        public enum EMoveMethod : uint
        {
            Begin = 0,
            Current = 1,
            End = 2
        }

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
        ///////////////////////////////////



        private void PopulateDriveListBox()
        {
            Cursor.Current = Cursors.WaitCursor;
            this.dg1.AutoGenerateColumns = true;
            this.dg1.DataSource = GetDriveList();
            // Set column headers and widths to match new column order
            dg1.RowHeadersVisible = false;
            dg1.Columns[0].HeaderText = "";
            dg1.Columns[0].Width = 62;
            dg1.Columns[1].HeaderText = "Drive Letter";
            dg1.Columns[1].Width = 55;
            dg1.Columns[2].HeaderText = "Interface";
            dg1.Columns[2].Width = 80;
            dg1.Columns[3].HeaderText = "Model";
            dg1.Columns[3].Width = 200;
            dg1.Columns[4].HeaderText = "Mediatype";
            dg1.Columns[4].Width = 123;
            dg1.Columns[5].HeaderText = "Size";
            dg1.Columns[5].Width = 75;
            dg1.Columns[6].HeaderText = "Status";
            dg1.Columns[6].Width = 75;
            dg1.Columns[7].HeaderText = "DeviceID";
            dg1.Columns[7].Visible = false;
            Cursor.Current = Cursors.Default;
        }


        public DataTable GetDriveList()
        {
            string strIsSys = "", strDriveLetter, strInt, strMod, strMed, strSiz, strSta = "", strDev;
            decimal decSiz;
            string sysDevice = "";

            //Get device ID for operating system drive to disable it from the list
            string sysDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DeviceID='" + sysDrive + "'"))
            {
                foreach (ManagementObject logicalDisk in searcher.Get())
                    foreach (ManagementObject partition in logicalDisk.GetRelated("Win32_DiskPartition"))
                        foreach (ManagementObject diskDrive in partition.GetRelated("Win32_DiskDrive"))
                            sysDevice = diskDrive["DeviceID"].ToString();
            }

            DataTable dt = new DataTable();
            dt.Columns.Add("System Drive");
            dt.Columns.Add("Drive Letter");
            dt.Columns.Add("Interface");
            dt.Columns.Add("Model");
            dt.Columns.Add("Mediatype");
            dt.Columns.Add("Size");
            dt.Columns.Add("Status");
            dt.Columns.Add("DeviceID");

            try
            {
                ManagementObjectSearcher mObjS =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject mObj in mObjS.Get())
                {
                    strInt = mObj["InterfaceType"].ToString();
                    strMod = mObj["Model"].ToString();
                    strMed = mObj["MediaType"].ToString();
                    strDev = mObj["DeviceID"].ToString();
                    decSiz = Convert.ToDecimal(mObj["Size"].ToString());
                    strSiz = decSiz switch
                    {
                        > 999999999999 => Math.Round((decSiz / 1000000000000), 1) + " TB",
                        > 999999999 => Math.Round((decSiz / 1000000000), 1) + " GB",
                        > 999999 => Math.Round((decSiz / 1000000), 1) + " MB",
                        > 999 => Math.Round((decSiz / 1000), 1) + " KB",
                        _ => Math.Round(decSiz, 1).ToString(CultureInfo.InvariantCulture)
                    };
                    if (strDev == sysDevice)
                    {
                        strIsSys = "*SYSTEM*";
                    }
                    //Read boot sector and confirm whether it's a hidden, normal or unknown type
                    byte[] bufR = new byte[512];
                    bufR = ReadBoot(strDev);
                    if (bufR == null)
                    {
                        strSta = "*UNKNOWN*";
                    }
                    else
                    {
                        strSta = bufR[511] switch
                        {
                            170 => "NORMAL",
                            171 => "HIDDEN",
                            _ => "*UNKNOWN*"
                        };
                    }

                    // Find drive letter(s) for this physical drive
                    strDriveLetter = "";
                    try
                    {
                        foreach (ManagementObject partition in mObj.GetRelated("Win32_DiskPartition"))
                        {
                            foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
                            {
                                if (!string.IsNullOrEmpty(strDriveLetter))
                                    strDriveLetter += ", ";
                                strDriveLetter += logicalDisk["DeviceID"].ToString();
                            }
                        }
                    }
                    catch { }

                    dt.Rows.Add(new object[] { strIsSys, strDriveLetter, strInt, strMod, strMed, strSiz, strSta, strDev });
                    strIsSys = "";
                }
            }
            catch (ManagementException)
            {
                return null;
            }
            return dt;
        }

        /// <summary>
        /// Refresh button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            PopulateDriveListBox();
        }

        /// <summary>
        /// Double click action on any item in the datagridview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dg1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dg1.CurrentRow.Cells[0].Value.ToString() == "*SYSTEM*")
            {
                MessageBox.Show("You cannot make changes to the System drive!", "Impossibru!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (dg1.CurrentRow.Cells[6].Value.ToString() == "*UNKNOWN*")
            {
                MessageBox.Show("You cannot make changes to an unknown boot sector type!", "Impossibru!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            ToggleBoot(dg1.CurrentRow.Cells[4].Value.ToString() + " " + dg1.CurrentRow.Cells[1].Value.ToString() + " drive, model " + dg1.CurrentRow.Cells[3].Value.ToString() );
        }

        /// <summary>
        /// Reads the boot sector of the chosen drive
        /// </summary>
        /// <returns>returns a 512k byte array of the first sector of the drive</returns>
        public byte[] ReadBoot(string strDev)
        {
            uint GENERIC_READ = 0x80000000;
            uint OPEN_EXISTING = 3;

            try
            {

                SafeFileHandle handleValue = CreateFile(strDev, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (handleValue.IsInvalid)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                int offset = 0;
                byte[] buf = new byte[512];
                int read = 0;
                int moveToHigh;
                SetFilePointer(handleValue, offset, out moveToHigh, EMoveMethod.Begin);
                ReadFile(handleValue, buf, 512, out read, IntPtr.Zero);
                handleValue.Close();
                return buf;
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to get handle on or read the following drive: " + strDev + ".\rError details: " + e.Message.ToString()
                ,"Error",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

        }

        /// <summary>
        /// Toggles boot signature from 55AA to 55AB and back.  Checks for valid boot signature.
        /// Calls ReadBoot to read the current boot sector and wrBoot to write the new boot sector.
        /// </summary>
        public void ToggleBoot(string strDriveDetails)
        {
            byte[] bufR = new byte[512];
            bufR = ReadBoot(dg1.CurrentRow.Cells[7].Value.ToString());
            if (bufR == null)
            {
                return;
            }
            switch (bufR[510].ToString() + bufR[511].ToString())
            {
                //55AA - normal partition
                case "85170" when MessageBox.Show("Are you sure you want to hide the drive: " + strDriveDetails + "?", "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No:
                    return;
                case "85170":
                {
                    bufR[511] = 171;
                    if (wrBoot (bufR) == 99) 
                    {//success
                        MessageBox.Show("Partition was hidden.  You will only be able to access this partition with Wii USB loaders that support it.  "
                                        + "Be warned that Windows may ask if you want to format the drive when you insert it next time since it is hidden.  The obvious answer to that "
                                        + "is NO unless you want to lose the data on it."
                            , "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        PopulateDriveListBox();
                        return;
                    }
                    else
                    {//something wrong
                        MessageBox.Show("Something went wrong.  I hope you know what you're doing.", "Hmm....", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                case "85171":
                {
                    //55AB - hidden partition
                    bufR[511] = 170;
                    if (wrBoot (bufR) == 99)
                    {//success
                        MessageBox.Show("Partition was unhidden successfully.  You can now access this partition from anywhere.", "Done",MessageBoxButtons.OK,MessageBoxIcon.Information);
                        PopulateDriveListBox();
                        return;
                    }
                    else
                    {//something wrong
                        MessageBox.Show("Something went wrong.  I hope you know what you're doing.", "Hmm....", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                default: //unknown partition type
                    MessageBox.Show("Unknown boot signature found on the drive, for safety's sake, nothing was done.", "Hmmm...", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
            }
        }

        /// <summary>
        /// Writes the new boot sector - the system has checked everything for safety at this point
        /// </summary>
        /// <param name="bufToWrite">The modified data to write to the partition</param>
        /// <returns>1 = can't get disk handle, 2 = can't lock the device for edit, 3 = no change after edit, 99 = all good/returns>
        public int wrBoot(byte[] bufToWrite)
        {
            uint GENERIC_WRITE = 0x40000000;
            uint FSCTL_LOCK_VOLUME = 0x00090018;
            uint OPEN_EXISTING = 3;
            int intOut;
            string strDev = dg1.CurrentRow.Cells[7].Value.ToString();
            bool success = false;

            SafeFileHandle handleValue = CreateFile(strDev, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handleValue.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return 1; //can't get disk handle
            }
            
            success = DeviceIoControl(handleValue, FSCTL_LOCK_VOLUME, null, 0, null, 0, out intOut, IntPtr.Zero);
            if (!success)
            {
                handleValue.Close();
                MessageBox.Show("Unable to lock the device.  Check that it is not in use and try again.", "Device locked", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return 2; //can't lock the device for edit
            }

            //Got the handle, set the pointer
            int offset = 0;
            int bytesWritten = 0;
            int moveToHigh;
            SetFilePointer(handleValue, offset, out moveToHigh, EMoveMethod.Begin);

            ///Pointer set, write to the boot sector
            WriteFile(handleValue, bufToWrite, bufToWrite.Length, out bytesWritten, IntPtr.Zero);
            handleValue.Close();   
            
            //Verify what was written here
            byte [] bufVerify = ReadBoot(strDev);
            int lastIndex = Math.Min(bufVerify?.Length ?? 0, bufToWrite.Length) - 1;
            if (lastIndex >= 0 && bufVerify[lastIndex] == bufToWrite[lastIndex]) return 99; //success
            MessageBox.Show("On verify, it appears that nothing has changed.  Somehow I was unable to toggle the boot sector.", "Verify", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return 3; //nothing appears to have happened
            //success
        }
    }
}
