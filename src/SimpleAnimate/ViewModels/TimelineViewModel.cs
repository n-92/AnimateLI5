using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.ViewModels;

public partial class TimelineViewModel : ObservableObject
{
    private Project? _project;
    private DispatcherTimer? _playbackTimer;

    [ObservableProperty]
    private Frame? _selectedFrame;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private int _currentFrameIndex;

    [ObservableProperty]
    private int _framesPerSecond = 6;

    public ObservableCollection<Frame> Frames { get; } = [];

    public event EventHandler<Frame>? FrameSelected;

    public void LoadProject(Project project)
    {
        StopIfPlaying();
        _project = project;
        FramesPerSecond = project.FramesPerSecond > 0 ? project.FramesPerSecond : 6;
        Frames.Clear();
        foreach (var frame in project.Frames)
            Frames.Add(frame);

        RenumberFrames();

        if (Frames.Count > 0)
            SelectFrame(Frames[0]);
    }

    public void StopIfPlaying()
    {
        if (IsPlaying)
            StopPlayback();
    }

    [RelayCommand]
    private void SelectFrame(Frame? frame)
    {
        if (frame is null) return;
        SelectedFrame = frame;
        CurrentFrameIndex = Frames.IndexOf(frame);
        FrameSelected?.Invoke(this, frame);
    }

    [RelayCommand]
    private void AddFrame()
    {
        if (_project is null) return;

        StopIfPlaying();

        var newFrame = new Frame();
        _project.Frames.Add(newFrame);
        Frames.Add(newFrame);
        RenumberFrames();
        SelectFrame(newFrame);
    }

    [RelayCommand]
    private void DuplicateFrame()
    {
        if (_project is null || SelectedFrame is null) return;

        StopIfPlaying();

        var copy = SelectedFrame.Clone();
        _project.Frames.Add(copy);
        Frames.Add(copy);
        RenumberFrames();
        SelectFrame(copy);
    }

    [RelayCommand]
    private void DeleteFrame()
    {
        if (_project is null || SelectedFrame is null || Frames.Count <= 1) return;

        StopIfPlaying();

        var index = CurrentFrameIndex;
        var frame = SelectedFrame;
        _project.Frames.Remove(frame);
        Frames.Remove(frame);

        RenumberFrames();

        SelectFrame(Frames[Math.Min(index, Frames.Count - 1)]);
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsPlaying)
            StopPlayback();
        else
            StartPlayback();
    }

    [RelayCommand]
    private void IncreaseFps()
    {
        if (FramesPerSecond < 30)
        {
            FramesPerSecond++;
            ApplyFpsChange();
        }
    }

    [RelayCommand]
    private void DecreaseFps()
    {
        if (FramesPerSecond > 1)
        {
            FramesPerSecond--;
            ApplyFpsChange();
        }
    }

    private void ApplyFpsChange()
    {
        if (_project is not null)
            _project.FramesPerSecond = FramesPerSecond;

        if (IsPlaying && _playbackTimer is not null)
        {
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond);
        }
    }

    private void RenumberFrames()
    {
        for (int i = 0; i < Frames.Count; i++)
            Frames[i].Index = i + 1;
    }

    private void StartPlayback()
    {
        if (_project is null || Frames.Count <= 1) return;

        IsPlaying = true;
        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / (FramesPerSecond > 0 ? FramesPerSecond : 6))
        };
        _playbackTimer.Tick += OnPlaybackTick;
        _playbackTimer.Start();
    }

    private void StopPlayback()
    {
        IsPlaying = false;
        _playbackTimer?.Stop();
        _playbackTimer = null;
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (Frames.Count == 0) return;
        var nextIndex = (CurrentFrameIndex + 1) % Frames.Count;
        SelectFrame(Frames[nextIndex]);
    }
}
