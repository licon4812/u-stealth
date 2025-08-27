using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace UStealth.WinUI.Converters
{
    public class UnknownStatusVisibilityConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            return (value as string) == "*UNKNOWN*" ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}
