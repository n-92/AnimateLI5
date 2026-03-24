using SimpleAnimate.Core.Models;
using SimpleAnimate.Core.Services;

namespace SimpleAnimate.Tests;

public class ElementServiceTests
{
    private readonly ElementService _sut = new();

    [Fact]
    public void CreateElement_SetsKindAndPosition()
    {
        var element = _sut.CreateElement(ElementKind.Ellipse, 100, 200);

        Assert.Equal(ElementKind.Ellipse, element.Kind);
        Assert.Equal(100, element.X);
        Assert.Equal(200, element.Y);
    }

    [Fact]
    public void MoveElement_UpdatesPosition()
    {
        var element = _sut.CreateElement(ElementKind.Rectangle, 0, 0);

        _sut.MoveElement(element, 50, 75);

        Assert.Equal(50, element.X);
        Assert.Equal(75, element.Y);
    }

    [Fact]
    public void ResizeElement_ClampsMinimum()
    {
        var element = _sut.CreateElement(ElementKind.Rectangle, 0, 0);

        _sut.ResizeElement(element, 5, -10);

        Assert.Equal(10, element.Width);
        Assert.Equal(10, element.Height);
    }

    [Fact]
    public void ChangeColor_UpdatesFill()
    {
        var element = _sut.CreateElement(ElementKind.Star, 0, 0);

        _sut.ChangeColor(element, "#00FF00");

        Assert.Equal("#00FF00", element.FillColor);
    }
}
