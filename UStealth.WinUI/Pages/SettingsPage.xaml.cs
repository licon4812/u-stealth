using DevWinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Windows.Storage;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
//using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UStealth.WinUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private ComboBox _themeComboBox;
        private double MaximumHeight { get; } = GetScreenResolution().Height;
        private double MaximumWidth { get;} = GetScreenResolution().Width;

        public SettingsPage()
        {
            InitializeComponent();
            Page_Loaded();
        }

        private void Page_Loaded()
        {
            App.Current.ThemeService.SetThemeComboBoxDefaultItem(ThemeComboBox);
            NavigationStyleComboBox.SelectedValue = MainWindow.Current!.NavigationStyle switch
            {
                NavigationViewPaneDisplayMode.Top => "Top",
                NavigationViewPaneDisplayMode.Left => "Left",
                NavigationViewPaneDisplayMode.LeftCompact => "Left Compact",
                NavigationViewPaneDisplayMode.LeftMinimal => "Left Minimal",
                NavigationViewPaneDisplayMode.Auto => "Auto",
                _ => "Top"
            };
            BackdropComboBox.SelectedValue = App.Current.AppBackdrop switch
            {
                BackdropType.None => "None",
                BackdropType.Mica => "Mica",
                BackdropType.MicaAlt => "MicaAlt",
                BackdropType.Acrylic => "Acrylic",
                BackdropType.AcrylicThin => "AcrylicThin",
                BackdropType.Transparent => "Transparent",
                _ => "Mica"
            };

            
            WindowWidthSlider.Value = MainWindow.Current.AppWindow.Size.Width;
            WindowHeightSlider.Value = MainWindow.Current.AppWindow.Size.Height;
        }


        private async void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                App.Current.ThemeService.OnThemeComboBoxSelectionChanged(sender);
                if (ThemeComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
                string selectedTheme = selectedItem.Tag?.ToString() ?? "Default";
                if (selectedTheme == "Default")
                { 
                    ClearTheme();
                }
                else
                {
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppTheme"] = selectedTheme;
                }
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
        }



        private void ClearTheme()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("AppTheme");
        }


        private async Task ShowDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot // Use the page's XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void NavigationStyleComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NavigationStyleComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
            string selectedStyle = selectedItem.Tag?.ToString() ?? "Top";
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["NavigationStyle"] = selectedStyle;
            MainWindow.Current!.NavigationStyle = selectedStyle switch
            {
                "Top" => NavigationViewPaneDisplayMode.Top,
                "Left" => NavigationViewPaneDisplayMode.Left,
                "Left Compact" => NavigationViewPaneDisplayMode.LeftCompact,
                "Left Minimal" => NavigationViewPaneDisplayMode.LeftMinimal,
                "Auto" => NavigationViewPaneDisplayMode.Auto,
                _ => MainWindow.Current.NavigationStyle
            };
            
            MainWindow.Current.NavigationViewControl.PaneDisplayMode = MainWindow.Current.NavigationStyle;
        }

        private async void BackdropComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                App.Current.ThemeService.OnBackdropComboBoxSelectionChanged(sender);
                if (BackdropComboBox.SelectedItem is not ComboBoxItem selectedItem) return;
                string selectedBackdrop = selectedItem.Tag?.ToString() ?? "Mica";
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppBackdrop"] = selectedBackdrop;
            }
            catch (Exception ex)
            {
                await ShowDialog("Error", ex.Message);
            }
        }

        private void WindowHeightSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SaveWindowSize();
        }

        private void WindowWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SaveWindowSize();
        }

        private void SaveWindowSize()
        {
            if (WindowWidthSlider == null || WindowHeightSlider == null)
                return;

            var width = (int)WindowWidthSlider.Value;
            var height = (int)WindowHeightSlider.Value;
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["WindowSize"] = $"{width},{height}";
        }

        private static RectInt32 GetScreenResolution()
        {
            var appWindow = MainWindow.Current?.AppWindow;
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            return displayArea.WorkArea; // or displayArea.Bounds for the full screen
        }
    }
}
