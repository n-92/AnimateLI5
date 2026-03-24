using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Models;
using SimpleAnimate.Services;
using SimpleAnimate.Views;

namespace SimpleAnimate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IUndoService _undoService;

    [ObservableProperty]
    private Project _currentProject;

    [ObservableProperty]
    private string _title = "AnimateLI5";

    public CanvasViewModel Canvas { get; }
    public TimelineViewModel Timeline { get; }
    public ToolbarViewModel Toolbar { get; }

    public MainViewModel(
        IProjectService projectService,
        IUndoService undoService,
        CanvasViewModel canvasViewModel,
        TimelineViewModel timelineViewModel,
        ToolbarViewModel toolbarViewModel)
    {
        _projectService = projectService;
        _undoService = undoService;
        _currentProject = projectService.CreateNew();

        Canvas = canvasViewModel;
        Timeline = timelineViewModel;
        Toolbar = toolbarViewModel;

        Canvas.LoadFrame(_currentProject.Frames[0]);
        Timeline.LoadProject(_currentProject);
        Timeline.FrameSelected += OnFrameSelected;
        Toolbar.ToolSelected += OnToolSelected;
        Toolbar.ColorSelected += OnColorSelected;
        Toolbar.MoveToolSelected += OnMoveToolSelected;
        Toolbar.EraserToolSelected += OnEraserToolSelected;
        Toolbar.BrushSizeSelected += OnBrushSizeSelected;
        Toolbar.ExportVideoRequested += OnExportVideoRequested;

        Timeline.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TimelineViewModel.IsPlaying))
            {
                Canvas.IsEditable = !Timeline.IsPlaying;
                if (Timeline.IsPlaying)
                {
                    Canvas.CancelDrag();
                    Canvas.CancelResize();
                    Canvas.CancelDraw();
                    Canvas.CancelErase();
                }
            }
        };
    }

    private void OnFrameSelected(object? sender, Frame frame)
    {
        Canvas.LoadFrame(frame);
    }

    private void OnToolSelected(object? sender, ElementKind tool)
    {
        Canvas.ActiveTool = tool;
        Canvas.IsSelectMode = false;
        Canvas.IsEraserMode = false;
    }

    private void OnMoveToolSelected(object? sender, EventArgs e)
    {
        Canvas.IsSelectMode = true;
        Canvas.IsEraserMode = false;
    }

    private void OnEraserToolSelected(object? sender, EventArgs e)
    {
        Canvas.IsEraserMode = true;
        Canvas.IsSelectMode = false;
    }

    private void OnColorSelected(object? sender, string color)
    {
        Canvas.ChangeSelectedColor(color);
    }

    private void OnBrushSizeSelected(object? sender, double size)
    {
        Canvas.CurrentStrokeWidth = size;
    }

    private async void OnExportVideoRequested(object? sender, EventArgs e)
    {
        await ExportVideoAsync();
    }

    [RelayCommand]
    private void NewProject()
    {
        Canvas.CancelDrag();
        Canvas.CancelResize();
        Canvas.CancelDraw();
        Canvas.CancelErase();
        Timeline.StopIfPlaying();
        _undoService.Clear();
        CurrentProject = _projectService.CreateNew();
        Canvas.LoadFrame(CurrentProject.Frames[0]);
        Timeline.LoadProject(CurrentProject);
        Title = "AnimateLI5";
    }

    [RelayCommand]
    private void Undo()
    {
        Canvas.SafeUndo();
    }

    [RelayCommand]
    private void Redo()
    {
        Canvas.SafeRedo();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "AnimateLI5|*.sanimate",
            DefaultExt = ".sanimate"
        };

        if (dialog.ShowDialog() == true)
        {
            Canvas.SaveCurrentFrame();
            await _projectService.SaveAsync(CurrentProject, dialog.FileName);
            Title = $"AnimateLI5 \u2014 {System.IO.Path.GetFileNameWithoutExtension(dialog.FileName)}";
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "AnimateLI5|*.sanimate"
        };

        if (dialog.ShowDialog() == true)
        {
            Canvas.CancelDrag();
            Canvas.CancelResize();
            Canvas.CancelDraw();
            Canvas.CancelErase();
            Timeline.StopIfPlaying();
            _undoService.Clear();
            CurrentProject = await _projectService.LoadAsync(dialog.FileName);
            Canvas.LoadFrame(CurrentProject.Frames[0]);
            Timeline.LoadProject(CurrentProject);
            Title = $"AnimateLI5 \u2014 {System.IO.Path.GetFileNameWithoutExtension(dialog.FileName)}";
        }
    }

    private async Task ExportVideoAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "MP4 Video|*.mp4",
            DefaultExt = ".mp4",
            FileName = "my_animation"
        };

        if (dialog.ShowDialog() != true) return;

        Canvas.SaveCurrentFrame();
        Timeline.StopIfPlaying();

        var outputPath = dialog.FileName;
        var project = CurrentProject;
        var fps = project.FramesPerSecond > 0 ? project.FramesPerSecond : 6;
        var width = (int)Canvas.CanvasActualWidth;
        var height = (int)Canvas.CanvasActualHeight;
        var totalFrames = project.Frames.Count;

        // h264 requires even dimensions
        if (width % 2 != 0) width++;
        if (height % 2 != 0) height++;

        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"AnimateLI5_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        // Show progress window
        var progressWindow = new ExportProgressWindow
        {
            Owner = Application.Current.MainWindow
        };
        progressWindow.Show();

        try
        {
            // Phase 1: Render frames (0% - 50%)
            for (int i = 0; i < totalFrames; i++)
            {
                var bitmap = FrameRenderer.RenderFrame(project.Frames[i], width, height);
                var framePath = System.IO.Path.Combine(tempDir, $"frame_{i:D4}.png");
                FrameRenderer.SaveBitmapAsPng(bitmap, framePath);

                var renderPercent = (double)(i + 1) / totalFrames * 50;
                progressWindow.UpdateProgress(renderPercent, $"Rendering frame {i + 1} of {totalFrames}...");
            }

            // Phase 2: ffmpeg encoding (50% - 100%)
            progressWindow.UpdateProgress(50, "Encoding video...");

            var ffmpegResult = await RunFfmpegWithProgressAsync(
                tempDir, outputPath, fps, totalFrames, progressWindow);

            progressWindow.Close();

            if (ffmpegResult.ExitCode == 0)
            {
                MessageBox.Show("Video exported successfully!", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (ffmpegResult.Error == "ffmpeg not found")
            {
                MessageBox.Show(
                    "ffmpeg was not found on your system.\n\n" +
                    "To export video, please install ffmpeg:\n" +
                    "1. Download from ffmpeg.org\n" +
                    "2. Add ffmpeg to your system PATH\n" +
                    "3. Restart the application",
                    "ffmpeg Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"Export failed. ffmpeg exited with code {ffmpegResult.ExitCode}.",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch
        {
            progressWindow.Close();
            throw;
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
            catch { /* best effort cleanup */ }
        }
    }

    private async Task<(int ExitCode, string Error)> RunFfmpegWithProgressAsync(
        string tempDir, string outputPath, int fps, int totalFrames,
        ExportProgressWindow progressWindow)
    {
        try
        {
            var ffmpegPath = FindFfmpeg();
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -framerate {fps} -i \"{tempDir}\\frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);
            if (process is null)
                return (ExitCode: -1, Error: "Could not start ffmpeg process.");

            var frameRegex = FfmpegFrameRegex();
            var errorOutput = new System.Text.StringBuilder();

            // Read stderr asynchronously line-by-line for progress
            await Task.Run(() =>
            {
                var buffer = new char[256];
                var lineBuffer = new System.Text.StringBuilder();

                while (!process.StandardError.EndOfStream)
                {
                    int read = process.StandardError.Read(buffer, 0, buffer.Length);
                    if (read <= 0) continue;

                    var chunk = new string(buffer, 0, read);
                    errorOutput.Append(chunk);
                    lineBuffer.Append(chunk);

                    // ffmpeg uses \r to update progress on the same line
                    var text = lineBuffer.ToString();
                    if (text.Contains('\r') || text.Contains('\n'))
                    {
                        var match = frameRegex.Match(text);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int currentFrame))
                        {
                            var encodePercent = 50 + (double)currentFrame / totalFrames * 50;
                            if (encodePercent > 100) encodePercent = 100;

                            progressWindow.Dispatcher.Invoke(() =>
                                progressWindow.UpdateProgress(encodePercent,
                                    $"Encoding frame {currentFrame} of {totalFrames}..."));
                        }

                        // Keep only the last segment (after last \r or \n)
                        var lastBreak = Math.Max(text.LastIndexOf('\r'), text.LastIndexOf('\n'));
                        lineBuffer.Clear();
                        if (lastBreak + 1 < text.Length)
                            lineBuffer.Append(text[(lastBreak + 1)..]);
                    }
                }
            });

            await process.WaitForExitAsync();

            // Show 100% briefly
            progressWindow.UpdateProgress(100, "Done!");
            await Task.Delay(300);

            return (ExitCode: process.ExitCode, Error: errorOutput.ToString());
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (ExitCode: -1, Error: "ffmpeg not found");
        }
    }

    [GeneratedRegex(@"frame=\s*(\d+)")]
    private static partial Regex FfmpegFrameRegex();

    private static string FindFfmpeg()
    {
        // Check PATH first
        try
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "ffmpeg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });
            if (result is not null)
            {
                var path = result.StandardOutput.ReadLine();
                result.WaitForExit();
                if (result.ExitCode == 0 && !string.IsNullOrEmpty(path))
                    return path.Trim();
            }
        }
        catch { }

        // Check common install locations
        string[] candidates =
        [
            @"C:\Windows\ffmpeg\bin\ffmpeg.exe",
            @"C:\ffmpeg\bin\ffmpeg.exe",
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin", "ffmpeg.exe"),
        ];

        foreach (var candidate in candidates)
        {
            if (System.IO.File.Exists(candidate))
                return candidate;
        }

        return "ffmpeg"; // fallback
    }
}
