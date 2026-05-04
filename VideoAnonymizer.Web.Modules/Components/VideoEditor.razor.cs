using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VideoAnonymizer.Web.Modules.Actions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public sealed record DetectedObjectChangeSet
{
    public required IReadOnlyList<DetectedObjectDto> ObjectsToUpdate { get; init; }
    public required IReadOnlyList<string> ObjectsToRemove { get; init; }
    public required IReadOnlyList<DetectedObjectDto> ObjectsToAdd { get; init; }
}

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
    public EventCallback<bool> IsProcessingChanged { get; set; }

    [Parameter]
    public EventCallback<VideoEditorAction> OnAction { get; set; }

    private ElementReference _hostElement;
    private IJSObjectReference? _hostModule;
    private bool _mounted;
    private bool _loadFailed;
    private int _lastBlurSizePercent;
    private int _lastTimeBufferMs;
    private DotNetObjectReference<VideoEditor>? _dotNetRef;

    private readonly Channel<Func<Task>> _operationChannel = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _queueCts = new();
    private Task _processingTask = Task.CompletedTask;
    private int _pendingOperationCount;
    private bool _isIdle = true;

    protected override void OnInitialized()
    {
        _processingTask = ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _operationChannel.Reader;
        try
        {
            while (await reader.WaitToReadAsync(_queueCts.Token))
            {
                while (reader.TryRead(out var operation))
                {
                    try
                    {
                        await operation();
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void EnqueueOperation(Func<Task> operation)
    {
        Interlocked.Increment(ref _pendingOperationCount);
        _ = NotifyProcessingStateAsync();
        _operationChannel.Writer.TryWrite(WrapOperation(operation));
    }

    private Func<Task> WrapOperation(Func<Task> operation)
    {
        return async () =>
        {
            try
            {
                await operation();
            }
            finally
            {
                Interlocked.Decrement(ref _pendingOperationCount);
                await NotifyProcessingStateAsync();
            }
        };
    }

    private async Task NotifyProcessingStateAsync()
    {
        try
        {
            var isIdle = _pendingOperationCount == 0;
            _isIdle = isIdle;
            await InvokeAsync(() => IsProcessingChanged.InvokeAsync(isIdle));
            await NotifyVueIsIdleAsync();
        }
        catch
        {
        }
    }

    private async Task NotifyVueIsIdleAsync()
    {
        if (!_mounted || _hostModule is null) return;
        try
        {
            await _hostModule.InvokeVoidAsync("updateVideoEditorIsIdle", _hostElement, _isIdle);
        }
        catch
        {
        }
    }

    [JSInvokable]
    public async Task OnDetectedObjectAdded(string videoId, string analyzedFrameId, DetectedObjectDto dto, DetectedObjectDto[] beforeState, DetectedObjectDto[] afterState)
    {
        EnqueueOperation(() => OnAction.InvokeAsync(new ObjectAddedAction
        {
            VideoId = videoId,
            AnalyzedFrameId = analyzedFrameId,
            Object = dto,
            BeforeState = beforeState,
            AfterState = afterState
        }));
    }

    [JSInvokable]
    public async Task OnDetectedObjectUpdated(string videoId, string analyzedFrameId, DetectedObjectDto dto, string operationType, DetectedObjectDto[] beforeState, DetectedObjectDto[] afterState)
    {
        EnqueueOperation(() => OnAction.InvokeAsync(new ObjectUpdatedAction
        {
            VideoId = videoId,
            AnalyzedFrameId = analyzedFrameId,
            Object = dto,
            OperationType = operationType,
            BeforeState = beforeState,
            AfterState = afterState
        }));
    }

    [JSInvokable]
    public async Task OnDetectedObjectsBulkUpdated(string videoId, DetectedObjectDto[] dtos, string operationType, DetectedObjectDto[] beforeState, DetectedObjectDto[] afterState)
    {
        EnqueueOperation(() => OnAction.InvokeAsync(new ObjectsBulkUpdatedAction
        {
            VideoId = videoId,
            Objects = dtos,
            OperationType = operationType,
            BeforeState = beforeState,
            AfterState = afterState
        }));
    }

    [JSInvokable]
    public async Task OnUndo()
    {
        if (!_isIdle) return;
        EnqueueOperation(() => OnAction.InvokeAsync(new UndoAction()));
    }

    [JSInvokable]
    public async Task OnRedo()
    {
        if (!_isIdle) return;
        EnqueueOperation(() => OnAction.InvokeAsync(new RedoAction()));
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
            isIdle = _isIdle,
            anonymizationSettings = new
            {
                blurSizePercent = BlurSizePercent,
                timeBufferMs = TimeBufferMs
            }
        };
    }

    public async ValueTask DisposeAsync()
    {
        _queueCts.Cancel();
        try { await _processingTask; } catch { }

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
