using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Converters;

public class ElementKindToGeometryConverter : IValueConverter
{
    private static readonly Geometry RectGeo = new RectangleGeometry(new Rect(0, 0, 100, 100), 8, 8);
    private static readonly Geometry EllipseGeo = new EllipseGeometry(new Point(50, 50), 50, 50);
    private static readonly Geometry StarGeo = CreateStarGeometry();
    private static readonly Geometry TriangleGeo = CreateRegularPolygon(3);
    private static readonly Geometry PentagonGeo = CreateRegularPolygon(5);
    private static readonly Geometry HexagonGeo = CreateRegularPolygon(6);
    private static readonly Geometry HeptagonGeo = CreateRegularPolygon(7);
    private static readonly Geometry RhombusGeo = CreateRhombus();
    private static readonly Geometry TrapeziumGeo = CreateTrapezium();
    private static readonly Geometry ParallelogramGeo = CreateParallelogram();

    static ElementKindToGeometryConverter()
    {
        RectGeo.Freeze();
        EllipseGeo.Freeze();
        StarGeo.Freeze();
        TriangleGeo.Freeze();
        PentagonGeo.Freeze();
        HexagonGeo.Freeze();
        HeptagonGeo.Freeze();
        RhombusGeo.Freeze();
        TrapeziumGeo.Freeze();
        ParallelogramGeo.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ElementKind kind)
            return RectGeo;

        return kind switch
        {
            ElementKind.Rectangle => RectGeo,
            ElementKind.Ellipse => EllipseGeo,
            ElementKind.Star => StarGeo,
            ElementKind.Triangle => TriangleGeo,
            ElementKind.Pentagon => PentagonGeo,
            ElementKind.Hexagon => HexagonGeo,
            ElementKind.Heptagon => HeptagonGeo,
            ElementKind.Rhombus => RhombusGeo,
            ElementKind.Trapezium => TrapeziumGeo,
            ElementKind.Parallelogram => ParallelogramGeo,
            _ => RectGeo
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Geometry CreateStarGeometry()
    {
        const double size = 100;
        var cx = size / 2;
        var cy = size / 2;
        var outerRadius = size / 2;
        var innerRadius = outerRadius * 0.4;
        const int points = 5;

        var figure = new PathFigure { IsClosed = true, IsFilled = true };
        for (int i = 0; i < points * 2; i++)
        {
            var angle = Math.PI / 2 * -1 + i * Math.PI / points;
            var r = i % 2 == 0 ? outerRadius : innerRadius;
            var pt = new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));

            if (i == 0)
                figure.StartPoint = pt;
            else
                figure.Segments.Add(new LineSegment(pt, true));
        }

        var geo = new PathGeometry([figure]);
        geo.Freeze();
        return geo;
    }

    private static Geometry CreateRegularPolygon(int sides)
    {
        const double size = 100;
        var cx = size / 2;
        var cy = size / 2;
        var r = size / 2;

        var figure = new PathFigure { IsClosed = true, IsFilled = true };
        for (int i = 0; i < sides; i++)
        {
            // Start from top (−π/2)
            var angle = -Math.PI / 2 + i * 2 * Math.PI / sides;
            var pt = new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));

            if (i == 0)
                figure.StartPoint = pt;
            else
                figure.Segments.Add(new LineSegment(pt, true));
        }

        var geo = new PathGeometry([figure]);
        geo.Freeze();
        return geo;
    }

    private static Geometry CreateRhombus()
    {
        // Diamond shape: points at top, right, bottom, left
        var figure = new PathFigure
        {
            StartPoint = new Point(50, 0),
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment(new Point(100, 50), true));
        figure.Segments.Add(new LineSegment(new Point(50, 100), true));
        figure.Segments.Add(new LineSegment(new Point(0, 50), true));

        var geo = new PathGeometry([figure]);
        geo.Freeze();
        return geo;
    }

    private static Geometry CreateTrapezium()
    {
        // Shorter top, wider bottom
        var figure = new PathFigure
        {
            StartPoint = new Point(25, 0),
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment(new Point(75, 0), true));
        figure.Segments.Add(new LineSegment(new Point(100, 100), true));
        figure.Segments.Add(new LineSegment(new Point(0, 100), true));

        var geo = new PathGeometry([figure]);
        geo.Freeze();
        return geo;
    }

    private static Geometry CreateParallelogram()
    {
        // Slanted rectangle
        var figure = new PathFigure
        {
            StartPoint = new Point(20, 0),
            IsClosed = true,
            IsFilled = true
        };
        figure.Segments.Add(new LineSegment(new Point(100, 0), true));
        figure.Segments.Add(new LineSegment(new Point(80, 100), true));
        figure.Segments.Add(new LineSegment(new Point(0, 100), true));

        var geo = new PathGeometry([figure]);
        geo.Freeze();
        return geo;
    }
}
