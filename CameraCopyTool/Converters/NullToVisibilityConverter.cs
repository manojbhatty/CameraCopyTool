using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CameraCopyTool.Converters
{
    /// <summary>
    /// Converts null or empty string to Visibility.Collapsed, non-null to Visibility.Visible.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
