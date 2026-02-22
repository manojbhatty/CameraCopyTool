using System;
using System.Globalization;
using System.Windows.Data;

namespace CameraCopyTool.Converters;

/// <summary>
/// Converts a boolean value to its inverse for UI display purposes.
/// Used for toggle buttons that show opposite state text.
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to its inverse.
    /// </summary>
    /// <param name="value">The boolean value to invert.</param>
    /// <param name="targetType">The type of the target property (must be bool).</param>
    /// <param name="parameter">Optional parameter (not used).</param>
    /// <param name="culture">The culture to use (not used).</param>
    /// <returns>The inverse of the input boolean.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    /// <summary>
    /// Converts back by inverting the boolean value again.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }
}
