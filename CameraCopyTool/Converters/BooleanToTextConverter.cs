using System;
using System.Globalization;
using System.Windows.Data;

namespace CameraCopyTool.Converters;

/// <summary>
/// Converts a boolean value to Show/Hide text for toggle buttons.
/// True shows "Hide Instructions ▲", False shows "Show Instructions ▼".
/// </summary>
public class BooleanToTextConverter : IValueConverter
{
    /// <summary>
    /// Converts boolean to text: True = "Hide Instructions ▲", False = "Show Instructions ▼".
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "Hide Instructions ▲" : "Show Instructions ▼";
        }
        return "Show Instructions ▼";
    }

    /// <summary>
    /// Converts back - not used for this scenario.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
