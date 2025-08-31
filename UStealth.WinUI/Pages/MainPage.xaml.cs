using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Diagnostics;
using System.Security.Principal;
using System.Linq;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UStealth.WinUI.Pages
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Models.DriveInfoModel> Drives { get; } = new();
        public Models.DriveInfoModel SelectedDrive { get; set; }
        private readonly DriveManager _driveManager = new();

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            await CheckAndPromptForElevation();
#endif
            await LoadDrivesAsync();
        }

        private async Task CheckAndPromptForElevation()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin) return;
            var dialog = new ContentDialog
            {
                Title = "Administrator Required",
                Content = "This app needs to be run as administrator to function properly. Relaunch as administrator?",
                PrimaryButtonText = "Relaunch as Admin",
                SecondaryButtonText = "Exit",
                XamlRoot = this.XamlRoot // Use the page's XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                try
                {
                    Process.Start(psi);
                }
                catch { /* User cancelled UAC or error */ }
                App.Current.Exit();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                App.Current.Exit();
            }
        }

        private async Task LoadDrivesAsync()
        {
            try
            {
                IsLoading(true);
                Drives.Clear();
                var drives = await Task.Run(() => _driveManager.GetDriveList());
                if (drives != null)
                {
                    foreach (var d in drives.OrderBy(x => x.DriveLetter))
                    {
                        Drives.Add(d);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowDialog($"Failed to load drives: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading(false);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadDrivesAsync();
        }

        private async Task ShowDialog(string content, string title)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void IsLoading(bool isLoading)
        {
            ProgressRing.IsActive = isLoading;
        }

        private async void StatusToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading(true);
                if (sender is not ToggleSwitch { DataContext: Models.DriveInfoModel drive } toggle)
                    return;

                // Get the latest status for this drive
                string? latestStatus = _driveManager.GetDriveList()
                    .FirstOrDefault(d => d.DeviceID == drive.DeviceID)?.Status;

                // Determine what the toggle is switching to
                bool togglingToHidden = toggle.IsOn; // Assuming IsOn means "HIDDEN", Off means "NORMAL"
                bool alreadyHidden = string.Equals(latestStatus, "HIDDEN", StringComparison.OrdinalIgnoreCase);
                bool alreadyNormal = string.Equals(latestStatus, "NORMAL", StringComparison.OrdinalIgnoreCase);

                if ((togglingToHidden && alreadyHidden) || (!togglingToHidden && alreadyNormal))
                {
                    // No change needed, revert toggle to match actual status
                    toggle.IsOn = alreadyHidden;
                    IsLoading(false);
                    return;
                }
                SelectedDrive = drive;
                var result = await ToggleSelectedDriveAsync();
                if (!string.IsNullOrEmpty(result))
                {
                    var parts = result.Split('|');
                    await ShowDialog(parts[0], parts.Length > 1 ? parts[1] : "");
                }
                IsLoading(false);
            }
            catch (Exception ex)
            {
                await ShowDialog(ex.Message, "Error");
                IsLoading(false);
            }
        }

        private async Task<string> ToggleSelectedDriveAsync()
        {
            if (SelectedDrive == null)
                return null;
            if (SelectedDrive.IsSystemDrive)
                return "You cannot make changes to the System drive!|Impossible!";
            if (SelectedDrive.Status == "*UNKNOWN*")
                return "You cannot make changes to an unknown boot sector type!|Impossible!";

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
                    CreateNoWindow = true
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
                    await LoadDrivesAsync();
                    return "Partition was hidden. You will only be able to access this partition with Wii USB loaders that support it. Be warned that Windows may ask if you want to format the drive when you insert it next time since it is hidden. The obvious answer to that is NO unless you want to lose the data on it.|Done";
                }
                else if (exitCode == 99 && output == "UNHIDDEN")
                {
                    await LoadDrivesAsync();
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
    }
}
