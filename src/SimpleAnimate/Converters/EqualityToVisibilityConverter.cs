using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleAnimate.Converters;

/// <summary>
/// Returns Visible when the bound value's ToString() equals the ConverterParameter string.
/// Used for active tool indicators, selected state, etc.
/// </summary>
public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
