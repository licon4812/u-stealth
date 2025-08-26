using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Diagnostics;
using System.Security.Principal;

namespace UStealth.WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            if (Content is FrameworkElement fe)
            {
                fe.Loaded += MainWindow_Loaded;
            }
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
            ViewModel.LoadDrives();
        }

        private async void DrivesTableView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DrivesTableView.SelectedItem is DriveInfoModel selected)
            {
                ViewModel.SelectedDrive = selected;
                var result = await ViewModel.ToggleSelectedDriveAsync();
                if (!string.IsNullOrEmpty(result))
                {
                    var parts = result.Split('|');
                    await ShowDialog(parts[0], parts.Length > 1 ? parts[1] : "");
                }
            }
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
    }
}
