using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SimpleAnimate.Converters;
using SimpleAnimate.Core.Models;
using AnimFrame = SimpleAnimate.Core.Models.Frame;

namespace SimpleAnimate.Services;

public static class FrameRenderer
{
    private static readonly ElementKindToGeometryConverter GeoConverter = new();

    public static RenderTargetBitmap RenderFrame(AnimFrame frame, int width, int height)
    {
        var canvas = new Canvas
        {
            Width = width,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA))
        };

        foreach (var element in frame.Elements)
        {
            UIElement visual = element.Kind switch
            {
                ElementKind.Drawing => CreateDrawingVisual(element),
                ElementKind.Stamp => CreateStampVisual(element),
                _ => CreateShapeVisual(element)
            };

            Canvas.SetLeft(visual, element.X);
            Canvas.SetTop(visual, element.Y);
            canvas.Children.Add(visual);
        }

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(0, 0, width, height));
        canvas.UpdateLayout();

        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(canvas);
        return rtb;
    }

    public static void SaveBitmapAsPng(RenderTargetBitmap bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static UIElement CreateShapeVisual(Element element)
    {
        var geo = (Geometry)GeoConverter.Convert(element.Kind, typeof(Geometry), null, CultureInfo.InvariantCulture);
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.FillColor));
        brush.Freeze();

        var path = new System.Windows.Shapes.Path
        {
            Data = geo,
            Fill = brush,
            Stretch = Stretch.Fill,
            Width = element.Width,
            Height = element.Height,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(element.Rotation)
        };
        return path;
    }

    private static UIElement CreateDrawingVisual(Element element)
    {
        if (element.StrokePoints is null || element.StrokePoints.Count == 0)
            return new Canvas { Width = 0, Height = 0 };

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.FillColor));
        brush.Freeze();

        var polyline = new Polyline
        {
            Stroke = brush,
            StrokeThickness = element.StrokeWidth,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = element.Width,
            Height = element.Height,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(element.Rotation)
        };

        foreach (var pt in element.StrokePoints)
            polyline.Points.Add(new Point(pt.X, pt.Y));

        return polyline;
    }

    private static UIElement CreateStampVisual(Element element)
    {
        if (string.IsNullOrEmpty(element.AssetPath))
            return new Canvas { Width = 0, Height = 0 };

        try
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(element.AssetPath, UriKind.RelativeOrAbsolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            var image = new Image
            {
                Source = bitmapImage,
                Stretch = Stretch.Fill,
                Width = element.Width,
                Height = element.Height,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(element.Rotation)
            };
            return image;
        }
        catch
        {
            return new Canvas { Width = 0, Height = 0 };
        }
    }
}
