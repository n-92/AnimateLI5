using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Converters;

public class StrokePointsToPointsConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is not Element element || element.StrokePoints is null)
            return new PointCollection();

        var points = new PointCollection(element.StrokePoints.Count);
        foreach (var sp in element.StrokePoints)
            points.Add(new Point(sp.X, sp.Y));
        return points;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
