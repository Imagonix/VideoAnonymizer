using VideoAnonymizer.Web.Modules.Actions;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class VideoEditorUndoRedoState
{
    private readonly List<VideoEditorAction> _history = [];
    private readonly List<ActionHistoryItem?> _displayItems = [];
    private int _historyIndex = -1;

    public void Add(VideoEditorAction action, ActionHistoryItem? displayItem = null)
    {
        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            _displayItems.RemoveRange(_historyIndex + 1, _displayItems.Count - _historyIndex - 1);
        }

        _history.Add(action);
        _displayItems.Add(displayItem);
        _historyIndex = _history.Count - 1;
    }

    public PendingUndoRedo? Begin(bool isRedo)
    {
        if (isRedo)
        {
            if (_historyIndex >= _history.Count - 1)
                return null;

            _historyIndex++;
        }
        else if (_historyIndex < 0)
        {
            return null;
        }

        return new PendingUndoRedo(_history[_historyIndex], _displayItems[_historyIndex]);
    }

    public void Complete(PendingUndoRedo operation, bool isRedo)
    {
        if (operation.DisplayItem is not null)
        {
            operation.DisplayItem.Undone = !isRedo;
        }

        if (!isRedo)
        {
            _historyIndex--;
        }
    }

    public void Rollback(bool isRedo)
    {
        if (isRedo)
        {
            _historyIndex--;
        }
    }
}

public sealed record PendingUndoRedo(VideoEditorAction Action, ActionHistoryItem? DisplayItem);
