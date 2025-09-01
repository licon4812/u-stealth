using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using DevWinUI;
using UStealth.WinUI.Pages;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace UStealth.WinUI
{
    public sealed partial class MainWindow : Window
    {

        public new static MainWindow? Current { get; private set; }
        public string AppName => "U-Stealth";
        public string AppVersion => $"v{GetAppVersion()}";
        public Microsoft.UI.Xaml.Media.Imaging.BitmapImage AppIconUri => new(new Uri("ms-appx:///Assets/StoreLogo.png"));
        public  NavigationViewPaneDisplayMode NavigationStyle { get; set; }

        public MainWindow()
        {
            Current = this;
            InitializeComponent();
            ApplySavedWindowSize();
            GetNavigationStyle();
            ExtendsContentIntoTitleBar = true;
            AppIcon.ImageSource = AppIconUri;
            TitleBar.Title = $"{AppName} - {AppVersion}";
            SetTitleBar(TitleBar);
            if (Content is FrameworkElement fe)
            {
                fe.Loaded += MainWindow_Loaded;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            rootFrame.Navigate(typeof(MainPage));
        }

        private static string GetAppVersion()
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }



        private void NavigationView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                navOptions.IsNavigationStackEnabled = false;
            }
            Type pageType;
            if ((NavigationViewItem)args.SelectedItem == Main)
            {
                pageType = typeof(MainPage);
            }
            else
            {
                pageType = typeof(SettingsPage);
            }
            rootFrame.NavigateToType(pageType, null, navOptions);
        }


        private void GetNavigationStyle()
        {
            var navStyle = Windows.Storage.ApplicationData.Current.LocalSettings.Values["NavigationStyle"]?.ToString();
            NavigationStyle = navStyle switch
            {
                "Left" => NavigationViewPaneDisplayMode.Left,
                "Left Compat" => NavigationViewPaneDisplayMode.LeftCompact,
                "Left Minimal" => NavigationViewPaneDisplayMode.LeftMinimal,
                "Auto" => NavigationViewPaneDisplayMode.Auto,
                _ => NavigationViewPaneDisplayMode.Top
            };
        }

        private void ApplySavedWindowSize()
        {
            var sizeString = Windows.Storage.ApplicationData.Current.LocalSettings.Values["WindowSize"] as string;
            if (string.IsNullOrEmpty(sizeString)) return;
            var parts = sizeString.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                this.AppWindow.Resize(new SizeInt32(width, height));
            }
        }
    }
}
