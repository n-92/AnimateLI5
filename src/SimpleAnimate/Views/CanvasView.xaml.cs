using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SimpleAnimate.Core.Models;
using SimpleAnimate.ViewModels;

namespace SimpleAnimate.Views;

public partial class CanvasView : UserControl
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    // Live drawing state (code-behind only, no bindings)
    private Polyline? _activePolyline;
    private readonly List<Point> _activePoints = new();

    // Eraser visual
    private Ellipse? _eraserCursor;

    public CanvasView()
    {
        InitializeComponent();
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is CanvasViewModel vm)
        {
            vm.CanvasActualWidth = e.NewSize.Width;
            vm.CanvasActualHeight = e.NewSize.Height;
        }
    }

    private CanvasViewModel? VM => DataContext as CanvasViewModel;

    private const double EraserRadius = 16;

    // --- Click on empty canvas area ---
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (VM is null || !VM.IsEditable) return;

        if (VM.IsDragging)
        {
            VM.CancelDrag();
            ReleaseMouseCapture();
        }

        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is Element)
            return;

        Point clickPos = e.GetPosition(this);

        // Eraser: swipe-to-delete
        if (VM.IsEraserMode)
        {
            StartErasing(clickPos);
            CaptureMouse();
            Focus();
            e.Handled = true;
            return;
        }

        // Drawing tool: start freehand stroke
        if (VM.ActiveTool == ElementKind.Drawing && !VM.IsSelectMode)
        {
            StartLiveDrawing(clickPos);
            CaptureMouse();
            Focus();
            e.Handled = true;
            return;
        }

        HandleCanvasClick(clickPos);
        Focus();
        e.Handled = true;
    }

    private void HandleCanvasClick(Point clickPos)
    {
        if (VM is null) return;

        if (VM.IsSelectMode)
        {
            VM.SelectedElement = null;
            return;
        }

        VM.AddElement(VM.ActiveTool, clickPos.X, clickPos.Y);
    }

    // --- Live drawing (direct manipulation, no MVVM overhead) ---

    private void StartLiveDrawing(Point pos)
    {
        if (VM is null) return;
        VM.BeginDraw();

        _activePoints.Clear();
        _activePoints.Add(pos);

        var color = (Color)ColorConverter.ConvertFromString(VM.CurrentColor);
        _activePolyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = VM.CurrentStrokeWidth,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        };
        _activePolyline.Points.Add(pos);
        DrawingOverlay.Children.Add(_activePolyline);
    }

    private void ContinueLiveDrawing(Point pos)
    {
        if (_activePolyline is null) return;
        _activePoints.Add(pos);
        _activePolyline.Points.Add(pos);
    }

    private void EndLiveDrawing()
    {
        if (VM is null || _activePolyline is null) return;

        DrawingOverlay.Children.Remove(_activePolyline);
        var strokeWidth = _activePolyline.StrokeThickness;
        _activePolyline = null;

        var strokePoints = _activePoints.Select(p => new StrokePoint(p.X, p.Y)).ToList();
        _activePoints.Clear();

        VM.FinalizeDraw(strokePoints, strokeWidth);
    }

    private void CancelLiveDrawing()
    {
        if (_activePolyline is not null)
        {
            DrawingOverlay.Children.Remove(_activePolyline);
            _activePolyline = null;
        }
        _activePoints.Clear();
        VM?.CancelDraw();
    }

    // --- Eraser (swipe-to-delete) ---

    private void StartErasing(Point pos)
    {
        if (VM is null) return;
        VM.BeginErase();
        ShowEraserCursor(pos);
        VM.EraseElementAt(pos.X, pos.Y, EraserRadius);
    }

    private void ContinueErasing(Point pos)
    {
        if (VM is null || !VM.IsErasing) return;
        MoveEraserCursor(pos);
        VM.EraseElementAt(pos.X, pos.Y, EraserRadius);
    }

    private void EndErasing()
    {
        HideEraserCursor();
        VM?.EndErase();
    }

    private void ShowEraserCursor(Point pos)
    {
        _eraserCursor = new Ellipse
        {
            Width = EraserRadius * 2,
            Height = EraserRadius * 2,
            Stroke = Brushes.Gray,
            StrokeThickness = 2,
            StrokeDashArray = [3, 2],
            Fill = new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(_eraserCursor, pos.X - EraserRadius);
        Canvas.SetTop(_eraserCursor, pos.Y - EraserRadius);
        DrawingOverlay.Children.Add(_eraserCursor);
    }

    private void MoveEraserCursor(Point pos)
    {
        if (_eraserCursor is null) return;
        Canvas.SetLeft(_eraserCursor, pos.X - EraserRadius);
        Canvas.SetTop(_eraserCursor, pos.Y - EraserRadius);
    }

    private void HideEraserCursor()
    {
        if (_eraserCursor is not null)
        {
            DrawingOverlay.Children.Remove(_eraserCursor);
            _eraserCursor = null;
        }
    }

    // --- Drag elements ---
    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (VM is null || !VM.IsEditable) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not Element el) return;

        // Eraser mode: start erasing from this point
        if (VM.IsEraserMode)
        {
            Point erasePos = e.GetPosition(this);
            StartErasing(erasePos);
            CaptureMouse();
            Focus();
            e.Handled = true;
            return;
        }

        Point pos = e.GetPosition(this);
        VM.BeginDrag(el, pos);
        CaptureMouse();
        Focus();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (VM is null) return;

        Point pos = e.GetPosition(this);

        double maxX = ActualWidth > 0 ? ActualWidth : 800;
        double maxY = ActualHeight > 0 ? ActualHeight : 600;
        pos.X = Math.Clamp(pos.X, 0, maxX);
        pos.Y = Math.Clamp(pos.Y, 0, maxY);

        if (VM.IsErasing)
            ContinueErasing(pos);
        else if (VM.IsDrawing)
            ContinueLiveDrawing(pos);
        else if (VM.IsDragging)
            VM.ContinueDrag(pos);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (VM is null) return;

        if (VM.IsErasing)
        {
            EndErasing();
            ReleaseMouseCapture();
        }
        else if (VM.IsDrawing)
        {
            EndLiveDrawing();
            ReleaseMouseCapture();
        }
        else if (VM.IsDragging)
        {
            VM.EndDrag();
            ReleaseMouseCapture();
        }
    }

    // --- Drop image files onto canvas ---
    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && HasImageFiles(e.Data))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (VM is null || !VM.IsEditable) return;

        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            Point dropPos = e.GetPosition(this);

            foreach (var file in files)
            {
                var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                if (Array.Exists(SupportedImageExtensions, x => x == ext))
                {
                    VM.AddImageElement(file, dropPos.X, dropPos.Y);
                    dropPos.X += 20;
                    dropPos.Y += 20;
                }
            }
        }

        e.Handled = true;
    }

    private static bool HasImageFiles(IDataObject data)
    {
        if (data.GetData(DataFormats.FileDrop) is not string[] files) return false;
        return Array.Exists(files, f =>
            Array.Exists(SupportedImageExtensions, supported =>
                supported == System.IO.Path.GetExtension(f).ToLowerInvariant()));
    }

    // --- Resize ---
    private void ResizeHandle_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (VM is null || !VM.IsEditable) return;
        if (sender is not FrameworkElement thumb) return;

        FrameworkElement? fe = thumb;
        while (fe != null && fe.DataContext is not Element)
            fe = fe.Parent as FrameworkElement;

        if (fe?.DataContext is Element el)
        {
            Point mousePos = Mouse.GetPosition(this);
            VM.BeginResize(el, mousePos);
        }
        e.Handled = true;
    }

    private void ResizeHandle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (VM is null || !VM.IsResizing) return;
        Point mousePos = Mouse.GetPosition(this);
        VM.ContinueResize(mousePos);
        e.Handled = true;
    }

    private void ResizeHandle_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (VM?.IsResizing == true) VM.EndResize();
        e.Handled = true;
    }

    // --- Rotate ---
    private void RotateHandle_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (VM is null || !VM.IsEditable) return;
        if (sender is not FrameworkElement thumb) return;

        FrameworkElement? fe = thumb;
        while (fe != null && fe.DataContext is not Element)
            fe = fe.Parent as FrameworkElement;

        if (fe?.DataContext is Element el)
        {
            Point mousePos = Mouse.GetPosition(this);
            VM.BeginRotate(el, mousePos);
        }
        e.Handled = true;
    }

    private void RotateHandle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (VM is null || !VM.IsRotating) return;
        Point mousePos = Mouse.GetPosition(this);
        VM.ContinueRotate(mousePos);
        e.Handled = true;
    }

    private void RotateHandle_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (VM?.IsRotating == true) VM.EndRotate();
        e.Handled = true;
    }

    // --- Keyboard ---
    private void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        if (VM is null) return;

        if (e.Key == Key.Escape)
        {
            if (VM.IsErasing) EndErasing();
            if (VM.IsDrawing) CancelLiveDrawing();
            if (VM.IsDragging) VM.CancelDrag();
            if (VM.IsResizing) VM.CancelResize();
            if (VM.IsRotating) VM.CancelRotate();
            ReleaseMouseCapture();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && VM.SelectedElement is not null)
        {
            VM.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }
}
