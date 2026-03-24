namespace SimpleAnimate.Core.Models;

/// <summary>
/// Root document — a saved animation project.
/// </summary>
public class Project
{
    public string Name { get; set; } = "My Animation";
    public int CanvasWidth { get; set; } = 800;
    public int CanvasHeight { get; set; } = 600;
    public int FramesPerSecond { get; set; } = 6;
    public List<Frame> Frames { get; set; } = [new Frame { Index = 1 }];
    public string? FilePath { get; set; }
}
