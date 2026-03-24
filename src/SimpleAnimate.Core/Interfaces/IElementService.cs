using SimpleAnimate.Core.Models;

namespace SimpleAnimate.Core.Interfaces;

public interface IElementService
{
    Element CreateElement(ElementKind kind, double x, double y);
    void MoveElement(Element element, double newX, double newY);
    void ResizeElement(Element element, double newWidth, double newHeight);
    void RotateElement(Element element, double newRotation);
    void ChangeColor(Element element, string newFillColor);
}
