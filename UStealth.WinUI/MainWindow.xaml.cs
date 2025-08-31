using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using UStealth.WinUI.Pages;

namespace UStealth.WinUI
{
    public sealed partial class MainWindow : Window
    {

        public string AppName => "U-Stealth";
        public string AppVersion => $"v{GetAppVersion()}";
        public Microsoft.UI.Xaml.Media.Imaging.BitmapImage AppIconUri => new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));

        public MainWindow()
        {
            InitializeComponent();
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


        //C# code behind
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // Don't handle settings here, as it is handled by SelectionChanged
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
    }
}
