using Microsoft.JSInterop;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public partial class VideoEditor
{
    [JSInvokable]
    public Task OnDetectedObjectAdded(string videoId, string analyzedFrameId, DetectedObjectDto dto)
    {
        return OnAction.InvokeAsync(new ObjectAddedAction
        {
            VideoId = videoId,
            AnalyzedFrameId = analyzedFrameId,
            Object = dto
        });
    }

    [JSInvokable]
    public Task OnDetectedObjectUpdated(string videoId, string analyzedFrameId, DetectedObjectDto dto, string operationType, DetectedObjectDto[] beforeState)
    {
        return OnAction.InvokeAsync(new ObjectUpdatedAction
        {
            VideoId = videoId,
            AnalyzedFrameId = analyzedFrameId,
            Object = dto,
            OperationType = operationType,
            BeforeState = beforeState
        });
    }

    [JSInvokable]
    public Task OnDetectedObjectsBulkUpdated(string videoId, DetectedObjectDto[] dtos, string operationType, DetectedObjectDto[] beforeState)
    {
        return OnAction.InvokeAsync(new ObjectsBulkUpdatedAction
        {
            VideoId = videoId,
            Objects = dtos,
            OperationType = operationType,
            BeforeState = beforeState
        });
    }

    [JSInvokable]
    public Task OnDetectedObjectDeleted(string videoId, string analyzedFrameId, DetectedObjectDto dto)
    {
        return OnAction.InvokeAsync(new ObjectDeletedAction
        {
            VideoId = videoId,
            AnalyzedFrameId = analyzedFrameId,
            Object = dto
        });
    }

    [JSInvokable]
    public Task OnUndo()
    {
        return OnAction.InvokeAsync(new UndoAction());
    }

    [JSInvokable]
    public Task OnRedo()
    {
        return OnAction.InvokeAsync(new RedoAction());
    }
}
