using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimpleAnimate.Converters;

/// <summary>
/// Takes [currentFrame, selectedFrame] and returns a highlight brush if they match.
/// </summary>
public sealed class FrameSelectedBrushConverter : IMultiValueConverter
{
    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromRgb(0x40, 0xC4, 0xFF));
    private static readonly Brush DefaultBrush = new SolidColorBrush(Colors.White);

    static FrameSelectedBrushConverter()
    {
        SelectedBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is not null && ReferenceEquals(values[0], values[1]))
            return SelectedBrush;
        return DefaultBrush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
