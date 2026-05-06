using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public partial class VideoEditor : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid VideoId { get; set; }
    [Parameter]
    public string VideoSourceUrl { get; set; } = string.Empty;
    [Parameter]
    public IReadOnlyList<AnalyzedFrameDto> Frames { get; set; } = Array.Empty<AnalyzedFrameDto>();
    [Parameter]
    public int BlurSizePercent { get; set; } = 120;

    [Parameter]
    public int TimeBufferMs { get; set; } = 0;

    [Parameter]
    public EventCallback<VideoEditorAction> OnAction { get; set; }

    private ElementReference _hostElement;
    private IJSObjectReference? _hostModule;
    private bool _mounted;
    private bool _loadFailed;
    private int _lastBlurSizePercent;
    private int _lastTimeBufferMs;
    private DotNetObjectReference<VideoEditor>? _dotNetRef;

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

    public async Task PushChangesToVue(DetectedObjectChangeSet changes)
    {
        if (!_mounted || _hostModule is null) return;
        try
        {
            await _hostModule.InvokeVoidAsync("applyDetectedObjectChanges", _hostElement, changes);
        }
        catch
        {
        }
    }

    public async Task<IReadOnlyList<AnalyzedFrameDto>> GetFramesAsync()
    {
        if (!_mounted || _hostModule is null)
            return Frames.ToArray();

        var editedFrames = await _hostModule.InvokeAsync<AnalyzedFrameDto[]?>(
            "getFrames",
            _hostElement);

        return editedFrames ?? Array.Empty<AnalyzedFrameDto>();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _hostModule = await JS.InvokeAsync<IJSObjectReference>(
                    "import",
                    "/_content/VideoAnonymizer.Web.Modules/js/videoEditorHost.js");

                _dotNetRef = DotNetObjectReference.Create(this);

                await _hostModule.InvokeVoidAsync(
                    "mountVideoEditor",
                    _hostElement,
                    BuildProps(),
                    _dotNetRef);

                _lastBlurSizePercent = BlurSizePercent;
                _lastTimeBufferMs = TimeBufferMs;
                _mounted = true;
            }
            catch
            {
                _loadFailed = true;
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_mounted || _hostModule is null)
            return;

        if (BlurSizePercent != _lastBlurSizePercent || TimeBufferMs != _lastTimeBufferMs)
        {
            _lastBlurSizePercent = BlurSizePercent;
            _lastTimeBufferMs = TimeBufferMs;

            await _hostModule.InvokeVoidAsync(
                "updateVideoEditorSettings",
                _hostElement,
                new { blurSizePercent = BlurSizePercent, timeBufferMs = TimeBufferMs });
        }
    }

    private object BuildProps()
    {
        return new
        {
            videoId = VideoId.ToString(),
            videoSourceUrl = VideoSourceUrl,
            frames = Frames,
            anonymizationSettings = new
            {
                blurSizePercent = BlurSizePercent,
                timeBufferMs = TimeBufferMs
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();

        try
        {
            if (_hostModule is not null)
            {
                await _hostModule.InvokeVoidAsync("unmountVideoEditor", _hostElement);
                await _hostModule.DisposeAsync();
            }
        }
        catch
        {
        }
    }
}
