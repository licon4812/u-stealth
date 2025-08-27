using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Diagnostics;
using System.Security.Principal;
using System.Linq;

namespace UStealth.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            AppIcon.ImageSource = ViewModel.AppIconUri;
            TitleBar.Title = $"{ViewModel.AppName} - {ViewModel.AppVersion}";
            SetTitleBar(TitleBar);
            if (Content is FrameworkElement fe)
            {
                fe.Loaded += MainWindow_Loaded;
            }
            ViewModel.LoadDrivesFailed += ViewModel_LoadDrivesFailed;
        }

        private async void ViewModel_LoadDrivesFailed(object sender, string error)
        {
            await ShowDialog(error, "Error");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckAndPromptForElevation();
        }

        private void CheckAndPromptForElevation()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                var dialog = new ContentDialog
                {
                    Title = "Administrator Required",
                    Content = "This app needs to be run as administrator to function properly. Relaunch as administrator?",
                    PrimaryButtonText = "Relaunch as Admin",
                    SecondaryButtonText = "Exit",
                    XamlRoot = Content.XamlRoot
                };

                _= dialog.ShowAsync().AsTask().ContinueWith(async t =>
                {
                    var result = await t;
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
                        DispatcherQueue.TryEnqueue(Close);
                    } 
                    else if(result == ContentDialogResult.Secondary)
                    {
                        DispatcherQueue.TryEnqueue(Close);
                    }
                });
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading(true);
            ViewModel.LoadDrives(); // Call directly on UI thread
            IsLoading(false);
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
                if (sender is not ToggleSwitch { DataContext: DriveInfoModel drive } toggle)
                    return;

                // Get the latest status for this drive
                var driveManager = new DriveManager();
                string? latestStatus = driveManager.GetDriveList()
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
                ViewModel.SelectedDrive = drive;
                var result = await ViewModel.ToggleSelectedDriveAsync();
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
    }
}
