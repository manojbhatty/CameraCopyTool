using System;
using System.Globalization;
using System.Windows.Data;

namespace CameraCopyTool.Converters
{
    /// <summary>
    /// Converts a height value to half its size.
    /// Used to limit the expanded Already Copied ListView to half the window height.
    /// </summary>
    public class HalfHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height / 2.0;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
