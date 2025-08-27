using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace UStealth.WinUI.Converters
{
    public class StatusToIsOnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Equals(value as string, "HIDDEN", StringComparison.Ordinal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return b ? "HIDDEN" : "NORMAL";
            return "*UNKNOWN*";
        }
    }
}
