using System.Text.Json;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class VideoEditorUndoRedoState
{
    private readonly List<VideoEditorAction> _history = [];
    private readonly List<ActionHistoryItem?> _displayItems = [];
    private readonly List<Guid> _actionIds = [];
    private int _historyIndex = -1;

    public int HistoryIndex => _historyIndex;
    public int Count => _history.Count;

    public ActionHistoryItem? GetDisplayItem(int index)
    {
        if (index < 0 || index >= _displayItems.Count)
            return null;
        return _displayItems[index];
    }

    public void Add(VideoEditorAction action, ActionHistoryItem? displayItem = null, Guid actionId = default)
    {
        if (_historyIndex < _history.Count - 1)
        {
            var keep = _historyIndex + 1;
            _history.RemoveRange(keep, _history.Count - keep);
            _displayItems.RemoveRange(keep, _displayItems.Count - keep);
            _actionIds.RemoveRange(keep, _actionIds.Count - keep);
        }

        _history.Add(action);
        _displayItems.Add(displayItem);
        _actionIds.Add(actionId);
        _historyIndex = _history.Count - 1;
    }

    public void SetActionId(VideoEditorAction action, Guid actionId)
    {
        var idx = _history.IndexOf(action);
        if (idx >= 0)
            _actionIds[idx] = actionId;
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

        return new PendingUndoRedo(_history[_historyIndex], _displayItems[_historyIndex], _actionIds[_historyIndex]);
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

    public void DeserializeActions(Guid videoId, List<EditorActionDto> actionDtos)
    {
        _history.Clear();
        _displayItems.Clear();
        _actionIds.Clear();
        _historyIndex = -1;

        foreach (var dto in actionDtos.OrderBy(a => a.SequenceNumber))
        {
            var action = ReconstructAction(videoId, dto.ActionType, dto.Data);
            if (action is null) continue;

            var description = GetDescription(dto.ActionType, dto.Data);
            var createdObjectDtos = ExtractCreatedDtos(dto.ActionType, dto.Data);
            var item = new ActionHistoryItem(description, dto.CreatedAt)
            {
                Status = GetStatus(dto.ActionType, dto.Data),
                Undone = dto.Undone,
                CreatedObjectIds = createdObjectDtos.Select(obj => obj.Id).ToList(),
                CreatedObjectDtos = createdObjectDtos
            };

            _history.Add(action);
            _displayItems.Add(item);
            _actionIds.Add(dto.Id);
        }

        _historyIndex = _displayItems.FindLastIndex(i => i is not null && !i.Undone);
    }

    private static VideoEditorAction? ReconstructAction(Guid videoId, string actionType, string data)
    {
        try
        {
            return actionType switch
            {
                "add" => DeserializeAdd(videoId, data),
                "update" => DeserializeUpdate(videoId, data),
                "bulk-update" => DeserializeBulkUpdate(videoId, data),
                "delete" => DeserializeDelete(videoId, data),
                "settings" => DeserializeSettings(videoId, data),
                "track-forward" => DeserializeTrackForward(videoId, data),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static VideoEditorAction DeserializeAdd(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataAdd>(data)!;
        return new ObjectAddedAction
        {
            VideoId = videoId.ToString(),
            AnalyzedFrameId = d.FrameId,
            Object = d.Object
        };
    }

    private static VideoEditorAction DeserializeUpdate(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataUpdate>(data)!;
        return new ObjectUpdatedAction
        {
            VideoId = videoId.ToString(),
            AnalyzedFrameId = d.FrameId,
            Object = d.After,
            OperationType = d.OperationType,
            BeforeState = [d.Before]
        };
    }

    private static VideoEditorAction DeserializeBulkUpdate(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataBulkUpdate>(data)!;
        return new ObjectsBulkUpdatedAction
        {
            VideoId = videoId.ToString(),
            Objects = d.After,
            OperationType = d.OperationType,
            BeforeState = d.Before
        };
    }

    private static VideoEditorAction DeserializeDelete(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataDelete>(data)!;
        return new ObjectDeletedAction
        {
            VideoId = videoId.ToString(),
            AnalyzedFrameId = d.FrameId,
            Object = d.Object
        };
    }

    private static VideoEditorAction DeserializeSettings(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataSettings>(data)!;
        return new SettingsUpdatedAction
        {
            VideoId = videoId,
            BeforeState = d.Before,
            AfterState = d.After
        };
    }

    private static VideoEditorAction DeserializeTrackForward(Guid videoId, string data)
    {
        var d = JsonSerializer.Deserialize<ActionDataTrackForward>(data)!;
        return new TrackForwardAction
        {
            VideoId = videoId.ToString(),
            AnalyzedFrameId = d.SeedFrameId,
            Object = d.Seed
        };
    }

    private static string GetDescription(string actionType, string data)
    {
        return actionType switch
        {
            "add" => "Added bounding box",
            "update" => "Updated bounding box",
            "bulk-update" => "Bulk updated objects",
            "delete" => "Deleted bounding box",
            "settings" => "Changed settings",
            "track-forward" => "Tracked object forward",
            _ => actionType
        };
    }

    private static ActionStatus GetStatus(string actionType, string data)
    {
        if (actionType != "track-forward") return ActionStatus.Success;

        try
        {
            var d = JsonSerializer.Deserialize<ActionDataTrackForward>(data)!;
            return d.IsPartial ? ActionStatus.Partial : ActionStatus.Success;
        }
        catch
        {
            return ActionStatus.Success;
        }
    }

    private static List<DetectedObjectDto> ExtractCreatedDtos(string actionType, string data)
    {
        if (actionType != "track-forward") return [];
        try
        {
            var d = JsonSerializer.Deserialize<ActionDataTrackForward>(data)!;
            return d.CreatedObjects;
        }
        catch
        {
            return [];
        }
    }
}

public sealed record PendingUndoRedo(VideoEditorAction Action, ActionHistoryItem? DisplayItem, Guid ActionId);
