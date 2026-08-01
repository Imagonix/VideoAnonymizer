using MudBlazor;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class EditorActionCoordinator : IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISnackbar _snackbar;
    private readonly Func<VideoEditor?> _getVideoEditor;
    private readonly Func<AnonymizationSettingsDto, Task> _applySettingsFromHistory;
    private readonly Func<TrackForwardAction, Task> _startTracking;
    private readonly Func<Task> _trackingCompleted;
    private readonly Func<bool, Task> _processingChanged;
    private readonly Func<Task> _stateChanged;
    private readonly VideoEditorActionQueue _actionQueue;
    private readonly VideoEditorUndoRedoState _undoRedoState = new();

    public EditorActionCoordinator(
        IHttpClientFactory httpClientFactory,
        ISnackbar snackbar,
        Func<VideoEditor?> getVideoEditor,
        Func<AnonymizationSettingsDto, Task> applySettingsFromHistory,
        Func<TrackForwardAction, Task> startTracking,
        Func<Task> trackingCompleted,
        Func<bool, Task> processingChanged,
        Func<Task> stateChanged)
    {
        _httpClientFactory = httpClientFactory;
        _snackbar = snackbar;
        _getVideoEditor = getVideoEditor;
        _applySettingsFromHistory = applySettingsFromHistory;
        _startTracking = startTracking;
        _trackingCompleted = trackingCompleted;
        _processingChanged = processingChanged;
        _stateChanged = stateChanged;
        _actionQueue = new VideoEditorActionQueue(_processingChanged);
    }

    public List<ActionHistoryItem> ActionHistory { get; } = [];
    public bool HasPendingActions => _actionQueue.HasPendingActions;
    public bool HasErrors { get; private set; }

    public async Task LoadAsync(Guid videoId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("ApiService");
            var persister = new VideoEditorActionPersister(client);
            var actions = await persister.LoadActionHistoryAsync(videoId);
            _undoRedoState.DeserializeActions(videoId, actions);

            ActionHistory.Clear();
            for (var i = 0; i < _undoRedoState.Count; i++)
            {
                if (_undoRedoState.GetDisplayItem(i) is { } displayItem)
                    ActionHistory.Add(displayItem);
            }
        }
        catch
        {
        }
    }

    public void Enqueue(VideoEditorAction action, Guid videoId)
    {
        _actionQueue.Enqueue(() => ProcessAsync(action, videoId));
    }

    public ActionHistoryItem AddAction(string description)
    {
        var item = new ActionHistoryItem(description, DateTime.UtcNow);
        ActionHistory.Add(item);
        if (ActionHistory.Count > 20)
            ActionHistory.RemoveAt(0);

        return item;
    }

    public void AddUndoRedoAction(VideoEditorAction action, ActionHistoryItem item, Guid actionId) =>
        _undoRedoState.Add(action, item, actionId);

    private async Task ProcessAsync(VideoEditorAction action, Guid videoId)
    {
        if (action is TrackForwardAction trackingAction)
        {
            await _startTracking(trackingAction);
            return;
        }

        using var client = _httpClientFactory.CreateClient("ApiService");
        var persister = new VideoEditorActionPersister(client);

        switch (action)
        {
            case UndoAction:
                await ApplyUndoRedoAsync(persister, videoId, isRedo: false);
                break;

            case RedoAction:
                await ApplyUndoRedoAsync(persister, videoId, isRedo: true);
                break;

            default:
                await PersistActionAsync(persister, videoId, action);
                break;
        }
    }

    private async Task PersistActionAsync(VideoEditorActionPersister persister, Guid videoId, VideoEditorAction action)
    {
        var item = AddAction(VideoEditorActionDescriptions.Get(action));

        try
        {
            await persister.SaveAsync(action);
            item.Status = ActionStatus.Success;
            var actionId = await persister.RecordActionAsync(videoId, action);
            _undoRedoState.Add(action, item, actionId);
            await _stateChanged();
        }
        catch
        {
            MarkActionFailed(item);
        }
    }

    private async Task ApplyUndoRedoAsync(VideoEditorActionPersister persister, Guid videoId, bool isRedo)
    {
        var operation = _undoRedoState.Begin(isRedo);
        if (operation is null)
            return;

        try
        {
            await persister.ApplyUndoRedoAsync(
                operation.Action,
                isRedo,
                _getVideoEditor(),
                _applySettingsFromHistory,
                operation.DisplayItem);

            if (operation.ActionId != Guid.Empty)
                await persister.ToggleActionUndoneAsync(videoId, operation.ActionId, !isRedo);

            _undoRedoState.Complete(operation, isRedo);

            if (operation.Action is TrackForwardAction && !isRedo)
                await _trackingCompleted();

            await _stateChanged();
        }
        catch
        {
            _undoRedoState.Rollback(isRedo);
            _snackbar.Add(isRedo ? "Failed to redo changes." : "Failed to undo changes.", Severity.Error);
            await _stateChanged();
        }
    }

    private void MarkActionFailed(ActionHistoryItem item)
    {
        item.Status = ActionStatus.Failed;
        HasErrors = true;
        _snackbar.Add("Failed to save changes. Please refresh the page.", Severity.Error);
    }

    public ValueTask DisposeAsync() => _actionQueue.DisposeAsync();
}
