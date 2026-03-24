using SimpleAnimate.Core.Interfaces;
using SimpleAnimate.Core.Services;

namespace SimpleAnimate.Tests;

public class UndoServiceTests
{
    private readonly UndoService _sut = new();

    [Fact]
    public void Execute_MakesUndoAvailable()
    {
        _sut.Execute(new FakeAction("test"));

        Assert.True(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Undo_RevertsAction()
    {
        var action = new FakeAction("test");
        _sut.Execute(action);

        _sut.Undo();

        Assert.True(action.WasUndone);
        Assert.False(_sut.CanUndo);
        Assert.True(_sut.CanRedo);
    }

    [Fact]
    public void Redo_ReappliesAction()
    {
        var action = new FakeAction("test");
        _sut.Execute(action);
        _sut.Undo();

        _sut.Redo();

        Assert.Equal(2, action.ExecuteCount);
        Assert.True(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Execute_AfterUndo_ClearsRedoStack()
    {
        _sut.Execute(new FakeAction("first"));
        _sut.Undo();
        _sut.Execute(new FakeAction("second"));

        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        _sut.Execute(new FakeAction("test"));

        _sut.Clear();

        Assert.False(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void StateChanged_FiresOnExecuteUndoRedo()
    {
        int eventCount = 0;
        _sut.StateChanged += (_, _) => eventCount++;

        _sut.Execute(new FakeAction("test"));
        _sut.Undo();
        _sut.Redo();

        Assert.Equal(3, eventCount);
    }

    private class FakeAction(string description) : IUndoableAction
    {
        public string Description => description;
        public int ExecuteCount { get; private set; }
        public bool WasUndone { get; private set; }

        public void Execute() => ExecuteCount++;
        public void Undo() => WasUndone = true;
    }
}
