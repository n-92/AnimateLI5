namespace SimpleAnimate.Core.Interfaces;

/// <summary>
/// Tracks undoable actions via a command stack.
/// </summary>
public interface IUndoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Execute(IUndoableAction action);
    void Undo();
    void Redo();
    void Clear();

    event EventHandler? StateChanged;
}

public interface IUndoableAction
{
    string Description { get; }
    void Execute();
    void Undo();
}
