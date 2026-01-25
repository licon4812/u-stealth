using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UStealth.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public Frame RootFrame { get; set; }
        public new static App Current => (App)Application.Current;
        public IThemeService ThemeService { get; set; }
        public BackdropType AppBackdrop { get; set; } = BackdropType.Mica;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
            ThemeService = new ThemeService();
            ThemeService.Initialize(_window);
            SetApplicationTheme();
            SetApplicationBackDrop();
            ThemeService.ConfigureBackdrop(AppBackdrop);
        }

        public async void SetApplicationTheme()
        {
            var theme = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppTheme"]?.ToString();
            if (theme != null)
            {
                switch (theme)
                {
                    case "Light":
                        await ThemeService.SetElementThemeAsync(ElementTheme.Light);
                        break;
                    case "Dark":
                        await ThemeService.SetElementThemeAsync(ElementTheme.Dark);
                        break;
                    default:
                        await ThemeService.SetElementThemeAsync(ElementTheme.Default);
                        break;
                }
            }
            else
            {
                await ThemeService.SetElementThemeAsync(ElementTheme.Default);
            }
        }

        public void SetApplicationBackDrop()
        {
            var backdrop = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppBackdrop"]?.ToString();
            if (backdrop != null)
            {
                AppBackdrop = backdrop switch
                {
                    "None" => BackdropType.None,
                    "Mica" => BackdropType.Mica,
                    "MicaAlt" => BackdropType.MicaAlt,
                    "Acrylic" => BackdropType.Acrylic,
                    "AcrylicThin" => BackdropType.AcrylicThin,
                    "Transparent" => BackdropType.Transparent,
                    _ => BackdropType.Mica
                };
            }
        }
    }
}
