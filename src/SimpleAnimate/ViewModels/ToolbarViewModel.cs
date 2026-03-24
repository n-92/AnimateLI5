using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.ViewModels;

public partial class ToolbarViewModel : ObservableObject
{
    private readonly IUndoService _undoService;
    private Views.AssetLibraryWindow? _assetLibraryWindow;

    [ObservableProperty]
    private ElementKind _selectedTool = ElementKind.Rectangle;

    [ObservableProperty]
    private string _selectedColor = "#FF4081";

    [ObservableProperty]
    private bool _isMoveToolActive;

    [ObservableProperty]
    private double _brushSize = 3;

    public event EventHandler<ElementKind>? ToolSelected;
    public event EventHandler<string>? ColorSelected;
    public event EventHandler? MoveToolSelected;
    public event EventHandler? EraserToolSelected;
    public event EventHandler<double>? BrushSizeSelected;
    public event EventHandler? ExportVideoRequested;

    public ObservableCollection<string> AssetPaths { get; } = new();

    public string[] ColorPalette { get; } =
    [
        "#FF4081", "#FF5252", "#FF6D00", "#FFD600",
        "#69F0AE", "#40C4FF", "#7C4DFF",
        "#FFFFFF", "#000000"
    ];

    public ToolbarViewModel(IUndoService undoService)
    {
        _undoService = undoService;
    }

    [RelayCommand]
    private void SelectTool(ElementKind tool)
    {
        SelectedTool = tool;
        IsMoveToolActive = false;
        ToolSelected?.Invoke(this, tool);
    }

    [RelayCommand]
    private void SelectMoveTool()
    {
        IsMoveToolActive = true;
        MoveToolSelected?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectEraserTool()
    {
        IsMoveToolActive = false;
        EraserToolSelected?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectBrushSize(double size)
    {
        BrushSize = size;
        BrushSizeSelected?.Invoke(this, size);
    }

    [RelayCommand]
    private void OpenAssetLibrary()
    {
        if (_assetLibraryWindow is not null)
        {
            _assetLibraryWindow.Show();
            _assetLibraryWindow.Activate();
            return;
        }

        _assetLibraryWindow = new Views.AssetLibraryWindow(AssetPaths)
        {
            Owner = Application.Current.MainWindow
        };
        _assetLibraryWindow.Show();
    }

    [RelayCommand]
    private void SelectColor(string color)
    {
        SelectedColor = color;
        ColorSelected?.Invoke(this, color);
    }

    [RelayCommand]
    private void ExportVideo()
    {
        ExportVideoRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Undo() => _undoService.Undo();

    [RelayCommand]
    private void Redo() => _undoService.Redo();
}
