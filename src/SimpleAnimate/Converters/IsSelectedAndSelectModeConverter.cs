using System.Globalization;
using System.Windows.Data;

namespace SimpleAnimate.Converters;

public sealed class IsSelectedAndSelectModeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3) return false;
        bool isSelected = values[0] is not null && ReferenceEquals(values[0], values[1]);
        bool isSelectMode = values[2] is true;
        return isSelected && isSelectMode;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
