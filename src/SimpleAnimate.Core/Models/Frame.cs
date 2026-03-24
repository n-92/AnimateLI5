using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleAnimate.Core.Models;

/// <summary>
/// A single snapshot in time containing positioned elements.
/// </summary>
public partial class Frame : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private int _index;

    /// <summary>
    /// Each frame owns its own copy of element state (position, color, etc.)
    /// so elements can differ between frames for animation.
    /// </summary>
    public List<Element> Elements { get; set; } = [];

    public Frame Clone()
    {
        return new Frame
        {
            Id = Guid.NewGuid().ToString(),
            Index = Index,
            Elements = Elements.Select(e => e.Clone()).ToList()
        };
    }
}
