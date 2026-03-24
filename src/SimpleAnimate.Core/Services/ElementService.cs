using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Core.Services;

public class ElementService : IElementService
{
    public Element CreateElement(ElementKind kind, double x, double y)
    {
        return new Element
        {
            Kind = kind,
            X = x,
            Y = y,
            Name = kind.ToString()
        };
    }

    public void MoveElement(Element element, double newX, double newY)
    {
        element.X = newX;
        element.Y = newY;
    }

    public void ResizeElement(Element element, double newWidth, double newHeight)
    {
        element.Width = Math.Max(10, newWidth);
        element.Height = Math.Max(10, newHeight);
    }

    public void RotateElement(Element element, double newRotation)
    {
        element.Rotation = newRotation % 360;
    }

    public void ChangeColor(Element element, string newFillColor)
    {
        element.FillColor = newFillColor;
    }
}
