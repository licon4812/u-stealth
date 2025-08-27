using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UStealth.WinUI
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private readonly DriveManager _driveManager = new();
        public ObservableCollection<DriveInfoModel> Drives { get; } = new();
        private DriveInfoModel _selectedDrive;
        public DriveInfoModel SelectedDrive
        {
            get => _selectedDrive;
            set { _selectedDrive = value; OnPropertyChanged(); }
        }

        public string AppName => "U-Stealth";
        public string AppVersion => $"v{GetAppVersion()}";
        public BitmapImage AppIconUri => new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));

        private static string GetAppVersion()
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public MainViewModel()
        {
            LoadDrives();
        }

        public void LoadDrives()
        {
            Drives.Clear();
            var drives = _driveManager.GetDriveList();
            if (drives != null)
            {
                foreach (var d in drives.OrderBy(x => x.DriveLetter))
                    Drives.Add(d);
            }
        }

        public async Task<string> ToggleSelectedDriveAsync()
        {
            if (SelectedDrive == null)
                return null;
            if (SelectedDrive.SystemDrive == "*SYSTEM*")
                return "You cannot make changes to the System drive!|Impossible!";
            if (SelectedDrive.Status == "*UNKNOWN*")
                return "You cannot make changes to an unknown boot sector type!|Impossible!";

            // Path to the helper EXE (adjust as needed)
            string helperExe = "UStealth.DriveHelper.exe";
            string args = $"toggleboot \"{SelectedDrive.DeviceID}\"";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = helperExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
#if DEBUG
                    CreateNoWindow = false
#else
                    CreateNoWindow = true
#endif

                };
                using var process = Process.Start(psi);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                int exitCode = process.ExitCode;
                output = output?.Trim();
                error = error?.Trim();

                if (exitCode == 99 && output == "HIDDEN")
                {
                    LoadDrives();
                    return "Partition was hidden. You will only be able to access this partition with Wii USB loaders that support it. Be warned that Windows may ask if you want to format the drive when you insert it next time since it is hidden. The obvious answer to that is NO unless you want to lose the data on it.|Done";
                }
                else if (exitCode == 99 && output == "UNHIDDEN")
                {
                    LoadDrives();
                    return "Partition was unhidden successfully. You can now access this partition from anywhere.|Done";
                }
                else if (exitCode == 1)
                {
                    return $"Unable to get handle on or read the drive. {error}|Error";
                }
                else if (exitCode == 2)
                {
                    return $"Unable to lock the device. Check that it is not in use and try again. {error}|Device locked";
                }
                else if (exitCode == 3)
                {
                    return $"On verify, it appears that nothing has changed. Somehow I was unable to toggle the boot sector. {error}|Verify";
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"{error}|Hmmm...";
                }
                else
                {
                    return "Unknown boot signature found on the drive, for safety's sake, nothing was done.|Hmmm...";
                }
            }
            catch (System.Exception ex)
            {
                return $"Failed to launch helper: {ex.Message}|Error";
            }
        }

        public async Task<string> ReadBootAsync()
        {
            if (SelectedDrive == null)
                return null;
            if (SelectedDrive.SystemDrive == "*SYSTEM*")
                return "You cannot make changes to the System drive!|Impossible!";
            if (SelectedDrive.Status == "*UNKNOWN*")
                return "You cannot make changes to an unknown boot sector type!|Impossible!";

            string helperExe = "UStealth.DriveHelper.exe";
            string args = $"readboot \"{SelectedDrive.DeviceID}\"";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = helperExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                int exitCode = process.ExitCode;
                output = output?.Trim();
                error = error?.Trim();

                if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output; // Hex string of boot sector
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"{error}|Error";
                }
                else
                {
                    return "Failed to read boot sector.|Error";
                }
            }
            catch (System.Exception ex)
            {
                return $"Failed to launch helper: {ex.Message}|Error";
            }
        }

        public async Task<string> WriteBootAsync(string hexData)
        {
            if (SelectedDrive == null)
                return null;
            if (SelectedDrive.SystemDrive == "*SYSTEM*")
                return "You cannot make changes to the System drive!|Impossible!";
            if (SelectedDrive.Status == "*UNKNOWN*")
                return "You cannot make changes to an unknown boot sector type!|Impossible!";
            if (string.IsNullOrWhiteSpace(hexData))
                return "No data provided to write.|Error";

            string helperExe = "UStealth.DriveHelper.exe";
            string args = $"writeboot \"{SelectedDrive.DeviceID}\" {hexData}";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = helperExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                int exitCode = process.ExitCode;
                output = output?.Trim();
                error = error?.Trim();

                if (exitCode == 99 && output == "OK")
                {
                    LoadDrives();
                    return "Boot sector written successfully.|Done";
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"{error}|Error";
                }
                else
                {
                    return "Failed to write boot sector.|Error";
                }
            }
            catch (System.Exception ex)
            {
                return $"Failed to launch helper: {ex.Message}|Error";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
