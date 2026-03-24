using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleAnimate.Core.Models;

/// <summary>
/// A visual object on the canvas (shape, stamp, drawing stroke).
/// Properties use [ObservableProperty] so XAML bindings update live during drag/edit.
/// </summary>
public partial class Element : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private ElementKind _kind;

    public string? Name { get; set; }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width = 100;

    [ObservableProperty]
    private double _height = 100;

    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private double _scaleX = 1;

    [ObservableProperty]
    private double _scaleY = 1;

    [ObservableProperty]
    private string _fillColor = "#FF4081";

    [ObservableProperty]
    private string _strokeColor = "#000000";

    [ObservableProperty]
    private double _strokeThickness = 2;

    [ObservableProperty]
    private double _opacity = 1;

    [ObservableProperty]
    private bool _isSelected;

    // For stamps/stickers
    public string? AssetPath { get; set; }

    // For freehand drawing strokes
    public List<StrokePoint>? StrokePoints { get; set; }

    [ObservableProperty]
    private int _strokeVersion;

    [ObservableProperty]
    private double _strokeWidth = 3;

    public Element Clone()
    {
        return new Element
        {
            Id = Id,
            Kind = Kind,
            Name = Name,
            X = X, Y = Y,
            Width = Width, Height = Height,
            Rotation = Rotation,
            ScaleX = ScaleX, ScaleY = ScaleY,
            FillColor = FillColor,
            StrokeColor = StrokeColor,
            StrokeThickness = StrokeThickness,
            Opacity = Opacity,
            AssetPath = AssetPath,
            StrokePoints = StrokePoints?.Select(p => new StrokePoint(p.X, p.Y)).ToList(),
            StrokeVersion = StrokeVersion,
            StrokeWidth = StrokeWidth
        };
    }
}

public record StrokePoint(double X, double Y);

public enum ElementKind
{
    Rectangle,
    Ellipse,
    Star,
    Triangle,
    Pentagon,
    Hexagon,
    Heptagon,
    Rhombus,
    Trapezium,
    Parallelogram,
    Stamp,
    Drawing
}
