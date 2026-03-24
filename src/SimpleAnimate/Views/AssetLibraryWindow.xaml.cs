using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleAnimate.Views;

public partial class AssetLibraryWindow : Window
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private readonly ObservableCollection<string> _assetPaths;

    public AssetLibraryWindow(ObservableCollection<string> assetPaths)
    {
        InitializeComponent();
        _assetPaths = assetPaths;
        AssetList.ItemsSource = _assetPaths;
        _assetPaths.CollectionChanged += (_, _) => UpdateHintVisibility();
        UpdateHintVisibility();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide instead of close so the window can be reshown with same data
        e.Cancel = true;
        Hide();
    }

    private void UpdateHintVisibility()
    {
        DropHint.Visibility = _assetPaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // --- Drag files INTO the library window (from Explorer) ---

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x69, 0xF0, 0xAE));
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x40, 0xC4, 0xFF));
        e.Handled = true;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x40, 0xC4, 0xFF));

        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            AddImageFiles(files);
        }

        e.Handled = true;
    }

    // --- File picker ---

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
            Title = "Pick pictures!",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            AddImageFiles(dialog.FileNames);
        }
    }

    private void AddImageFiles(string[] files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (Array.Exists(SupportedExtensions, e => e == ext) && !_assetPaths.Contains(file))
            {
                _assetPaths.Add(file);
            }
        }
    }

    // --- Remove asset ---

    private void RemoveAsset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            _assetPaths.Remove(path);
        }
    }

    // --- Drag asset OUT to the canvas ---

    private void Asset_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not string assetPath) return;

        var data = new DataObject(DataFormats.FileDrop, new[] { assetPath });
        DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
    }
}
