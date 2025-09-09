using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UStealth.WinUI.Converters
{
    public partial class StatusToTemplateColumnVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            var status = value as string;
            return status is "NORMAL" or "HIDDEN" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }

    public partial class StatusToTextColumnVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            var status = value as string;
            return status is "NORMAL" or "HIDDEN" ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}
