using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.ViewModels;

public partial class CanvasViewModel : ObservableObject
{
    private readonly IElementService _elementService;
    private readonly IUndoService _undoService;

    [ObservableProperty]
    private ObservableCollection<Element> _elements = new();

    [ObservableProperty]
    private Element? _selectedElement;

    [ObservableProperty]
    private string _currentColor = "#FF5252";

    [ObservableProperty]
    private ElementKind _activeTool = ElementKind.Rectangle;

    [ObservableProperty]
    private bool _isSelectMode;

    [ObservableProperty]
    private bool _isEraserMode;

    [ObservableProperty]
    private bool _isEditable = true;

    [ObservableProperty]
    private double _currentStrokeWidth = 3;

    [ObservableProperty]
    private double _canvasActualWidth = 800;

    [ObservableProperty]
    private double _canvasActualHeight = 600;

    private Frame? _currentFrame;

    // Drag state
    private bool _isDragging;
    private Point _dragStartMouse;
    private double _dragStartX;
    private double _dragStartY;

    // Resize state
    private bool _isResizing;
    private Point _resizeStartMouse;
    private double _resizeStartWidth;
    private double _resizeStartHeight;

    // Rotate state
    private bool _isRotating;
    private double _rotateStartAngle;
    private double _rotateStartRotation;

    // Drawing state
    private bool _isDrawing;

    public CanvasViewModel(IElementService elementService, IUndoService undoService)
    {
        _elementService = elementService;
        _undoService = undoService;
    }

    public void AddElement(ElementKind kind, double x, double y)
    {
        if (!IsEditable) return;

        const double size = 80;
        var element = _elementService.CreateElement(kind, x - size / 2, y - size / 2);
        element.Width = size;
        element.Height = size;
        element.FillColor = CurrentColor;
        var action = new AddElementAction(Elements, element);
        _undoService.Execute(action);
        SelectedElement = element;
    }

    public void AddImageElement(string imagePath, double x = 100, double y = 100)
    {
        if (!IsEditable) return;

        var element = _elementService.CreateElement(ElementKind.Stamp, x, y);
        element.Width = 100;
        element.Height = 100;
        element.AssetPath = imagePath;
        var action = new AddElementAction(Elements, element);
        _undoService.Execute(action);
        SelectedElement = element;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (!IsEditable) return;
        if (SelectedElement is null) return;

        var action = new RemoveElementAction(Elements, SelectedElement);
        _undoService.Execute(action);
        SelectedElement = null;
    }


    public void ChangeSelectedColor(string color)
    {
        if (!IsEditable) return;
        if (SelectedElement is null) return;

        var oldColor = SelectedElement.FillColor;
        if (oldColor == color) return;

        var action = new ChangeColorAction(SelectedElement, oldColor, color);
        _undoService.Execute(action);
    }

    public void SaveCurrentFrame()
    {
        if (_currentFrame is null) return;
        _currentFrame.Elements = Elements.ToList();
    }

    public void LoadFrame(Frame frame)
    {
        SaveCurrentFrame();
        _currentFrame = frame;
        Elements.Clear();
        foreach (var el in frame.Elements)
            Elements.Add(el);
        SelectedElement = null;
    }

    // === Drag ===

    public void BeginDrag(Element element, Point mousePos)
    {
        if (!IsEditable) return;
        _isDragging = true;
        _dragStartMouse = mousePos;
        _dragStartX = element.X;
        _dragStartY = element.Y;
        SelectedElement = element;
    }

    public void ContinueDrag(Point mousePos)
    {
        if (!_isDragging || SelectedElement is null) return;

        double dx = mousePos.X - _dragStartMouse.X;
        double dy = mousePos.Y - _dragStartMouse.Y;
        SelectedElement.X = _dragStartX + dx;
        SelectedElement.Y = _dragStartY + dy;
    }

    public void EndDrag()
    {
        if (!_isDragging || SelectedElement is null)
        {
            _isDragging = false;
            return;
        }
        _isDragging = false;

        double newX = SelectedElement.X;
        double newY = SelectedElement.Y;
        if (newX == _dragStartX && newY == _dragStartY) return;

        SelectedElement.X = _dragStartX;
        SelectedElement.Y = _dragStartY;
        var action = new MoveElementAction(SelectedElement, _dragStartX, _dragStartY, newX, newY);
        _undoService.Execute(action);
    }

    public void CancelDrag()
    {
        if (!_isDragging || SelectedElement is null)
        {
            _isDragging = false;
            return;
        }
        SelectedElement.X = _dragStartX;
        SelectedElement.Y = _dragStartY;
        _isDragging = false;
    }

    public bool IsDragging => _isDragging;

    // === Resize ===

    public void BeginResize(Element element, Point mousePos)
    {
        if (!IsEditable) return;
        _isResizing = true;
        _resizeStartMouse = mousePos;
        _resizeStartWidth = element.Width;
        _resizeStartHeight = element.Height;
        SelectedElement = element;
    }

    public void ContinueResize(Point mousePos)
    {
        if (!_isResizing || SelectedElement is null) return;

        double dx = mousePos.X - _resizeStartMouse.X;
        double dy = mousePos.Y - _resizeStartMouse.Y;

        double newW = Math.Max(10, _resizeStartWidth + dx);
        double newH = Math.Max(10, _resizeStartHeight + dy);

        SelectedElement.Width = newW;
        SelectedElement.Height = newH;
    }

    public void EndResize()
    {
        if (!_isResizing || SelectedElement is null)
        {
            _isResizing = false;
            return;
        }
        _isResizing = false;

        double newW = SelectedElement.Width;
        double newH = SelectedElement.Height;
        if (newW == _resizeStartWidth && newH == _resizeStartHeight) return;

        SelectedElement.Width = _resizeStartWidth;
        SelectedElement.Height = _resizeStartHeight;
        var action = new ResizeElementAction(SelectedElement, _resizeStartWidth, _resizeStartHeight, newW, newH);
        _undoService.Execute(action);
    }

    public void CancelResize()
    {
        if (!_isResizing || SelectedElement is null)
        {
            _isResizing = false;
            return;
        }
        SelectedElement.Width = _resizeStartWidth;
        SelectedElement.Height = _resizeStartHeight;
        _isResizing = false;
    }

    public bool IsResizing => _isResizing;

    // === Rotate ===

    public void BeginRotate(Element element, Point mousePos)
    {
        if (!IsEditable) return;
        _isRotating = true;
        _rotateStartRotation = element.Rotation;

        double cx = element.X + element.Width / 2;
        double cy = element.Y + element.Height / 2;
        _rotateStartAngle = Math.Atan2(mousePos.Y - cy, mousePos.X - cx) * 180 / Math.PI;
        SelectedElement = element;
    }

    public void ContinueRotate(Point mousePos)
    {
        if (!_isRotating || SelectedElement is null) return;

        double cx = SelectedElement.X + SelectedElement.Width / 2;
        double cy = SelectedElement.Y + SelectedElement.Height / 2;
        double currentAngle = Math.Atan2(mousePos.Y - cy, mousePos.X - cx) * 180 / Math.PI;
        double delta = currentAngle - _rotateStartAngle;

        SelectedElement.Rotation = _rotateStartRotation + delta;
    }

    public void EndRotate()
    {
        if (!_isRotating || SelectedElement is null)
        {
            _isRotating = false;
            return;
        }
        _isRotating = false;

        double newRotation = SelectedElement.Rotation;
        if (newRotation == _rotateStartRotation) return;

        SelectedElement.Rotation = _rotateStartRotation;
        var action = new RotateElementAction(SelectedElement, _rotateStartRotation, newRotation);
        _undoService.Execute(action);
    }

    public void CancelRotate()
    {
        if (!_isRotating || SelectedElement is null)
        {
            _isRotating = false;
            return;
        }
        SelectedElement.Rotation = _rotateStartRotation;
        _isRotating = false;
    }

    public bool IsRotating => _isRotating;

    // === Erase (swipe-to-delete) ===

    private bool _isErasing;

    public bool IsErasing => _isErasing;

    public void BeginErase()
    {
        if (!IsEditable) return;
        _isErasing = true;
    }

    public void EraseElementAt(double x, double y, double radius)
    {
        if (!_isErasing) return;

        for (int i = Elements.Count - 1; i >= 0; i--)
        {
            var el = Elements[i];
            // Check if eraser circle overlaps element bounding box
            double closestX = Math.Clamp(x, el.X, el.X + el.Width);
            double closestY = Math.Clamp(y, el.Y, el.Y + el.Height);
            double dx = x - closestX;
            double dy = y - closestY;
            if (dx * dx + dy * dy <= radius * radius)
            {
                var action = new RemoveElementAction(Elements, el);
                _undoService.Execute(action);
                if (SelectedElement == el)
                    SelectedElement = null;
                return; // one at a time for better control
            }
        }
    }

    public void EndErase()
    {
        _isErasing = false;
    }

    public void CancelErase()
    {
        _isErasing = false;
    }

    // === Draw (freehand brush) ===

    public void BeginDraw()
    {
        if (!IsEditable) return;
        _isDrawing = true;
    }

    public void FinalizeDraw(List<StrokePoint> points, double strokeWidth, string? color = null)
    {
        _isDrawing = false;
        if (points.Count < 2) return;

        var element = _elementService.CreateElement(ElementKind.Drawing, 0, 0);
        element.FillColor = color ?? CurrentColor;
        element.StrokeWidth = strokeWidth;
        element.StrokePoints = points;

        NormalizeDrawingElement(element);

        var action = new AddElementAction(Elements, element);
        _undoService.Execute(action);
        SelectedElement = element;
    }

    public void CancelDraw()
    {
        _isDrawing = false;
    }

    public bool IsDrawing => _isDrawing;

    private static void NormalizeDrawingElement(Element el)
    {
        if (el.StrokePoints is null || el.StrokePoints.Count == 0) return;

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var p in el.StrokePoints)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        double pad = el.StrokeWidth / 2 + 1;
        minX -= pad; minY -= pad;
        maxX += pad; maxY += pad;

        el.StrokePoints = el.StrokePoints
            .Select(p => new StrokePoint(p.X - minX, p.Y - minY))
            .ToList();

        el.X = minX;
        el.Y = minY;
        el.Width = Math.Max(1, maxX - minX);
        el.Height = Math.Max(1, maxY - minY);
    }

    // === Safe Undo / Redo ===

    public void SafeUndo()
    {
        if (_isDragging) CancelDrag();
        if (_isResizing) CancelResize();
        if (_isRotating) CancelRotate();
        if (_isDrawing) CancelDraw();
        if (_isErasing) CancelErase();
        _undoService.Undo();
    }

    public void SafeRedo()
    {
        if (_isDragging) CancelDrag();
        if (_isResizing) CancelResize();
        if (_isRotating) CancelRotate();
        if (_isDrawing) CancelDraw();
        if (_isErasing) CancelErase();
        _undoService.Redo();
    }

    // === Undoable Actions ===

    private sealed class AddElementAction(ObservableCollection<Element> collection, Element element) : IUndoableAction
    {
        public string Description => "Add element";
        public void Execute() => collection.Add(element);
        public void Undo() => collection.Remove(element);
    }

    private sealed class RemoveElementAction(ObservableCollection<Element> collection, Element element) : IUndoableAction
    {
        private int _index;
        public string Description => "Remove element";
        public void Execute() { _index = collection.IndexOf(element); collection.Remove(element); }
        public void Undo() => collection.Insert(_index, element);
    }

    private sealed class MoveElementAction(Element el, double oldX, double oldY, double newX, double newY) : IUndoableAction
    {
        public string Description => "Move element";
        public void Execute() { el.X = newX; el.Y = newY; }
        public void Undo() { el.X = oldX; el.Y = oldY; }
    }

    private sealed class ResizeElementAction(Element el, double oldW, double oldH, double newW, double newH) : IUndoableAction
    {
        public string Description => "Resize element";
        public void Execute() { el.Width = newW; el.Height = newH; }
        public void Undo() { el.Width = oldW; el.Height = oldH; }
    }

    private sealed class ChangeColorAction(Element el, string oldColor, string newColor) : IUndoableAction
    {
        public string Description => "Change color";
        public void Execute() => el.FillColor = newColor;
        public void Undo() => el.FillColor = oldColor;
    }

    private sealed class RotateElementAction(Element el, double oldAngle, double newAngle) : IUndoableAction
    {
        public string Description => "Rotate element";
        public void Execute() => el.Rotation = newAngle;
        public void Undo() => el.Rotation = oldAngle;
    }
}
