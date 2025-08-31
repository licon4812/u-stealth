using System;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage;
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

        public SettingsPage()
        {
            InitializeComponent();
            Page_Loaded();
        }


        private void Page_Loaded()
        {
            App.Current.ThemeService.SetThemeComboBoxDefaultItem(ThemeComboBox);
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
                    await ClearTheme();
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

        

        private async Task ClearTheme()
        { 
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("AppTheme");
        }

        private async Task ShowDialog(string title,string message)
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
    }
}
