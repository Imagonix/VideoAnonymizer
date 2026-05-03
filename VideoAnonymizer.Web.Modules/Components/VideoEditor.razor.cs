using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public partial class VideoEditor : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

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
                        // Swallow per-operation failures so queue keeps moving
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [JSInvokable]
    public async Task OnDetectedObjectAdded(string videoId, string analyzedFrameId, DetectedObjectDto dto)
    {
        await _operationChannel.Writer.WriteAsync(async () =>
        {
            using var client = HttpClientFactory.CreateClient("ApiService");
            var response = await client.PostAsJsonAsync(
                $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.AnalyzedFrame}/{dto.AnalyzedFrameId}/{SharedConstants.Paths.DetectedObject}/", dto);
            response.EnsureSuccessStatusCode();
        });
    }

    [JSInvokable]
    public async Task OnDetectedObjectUpdated(string videoId, string analyzedFrameId, DetectedObjectDto dto)
    {
        await _operationChannel.Writer.WriteAsync(async () =>
        {
            using var client = HttpClientFactory.CreateClient("ApiService");
            var response = await client.PutAsJsonAsync(
                $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.AnalyzedFrame}/{dto.AnalyzedFrameId}/{SharedConstants.Paths.DetectedObject}/{dto.Id}", dto);
            response.EnsureSuccessStatusCode();
        });
    }

    [JSInvokable]
    public async Task OnDetectedObjectsBulkUpdated(string videoId, DetectedObjectDto[] dtos)
    {
        await _operationChannel.Writer.WriteAsync(async () =>
        {
            using var client = HttpClientFactory.CreateClient("ApiService");
            foreach (var chunk in dtos.Chunk(8))
            {
                await Task.WhenAll(chunk.Select(dto =>
                    client.PutAsJsonAsync(
                        $"/{SharedConstants.Paths.Video}/{videoId}/{SharedConstants.Paths.AnalyzedFrame}/{dto.AnalyzedFrameId}/{SharedConstants.Paths.DetectedObject}/{dto.Id}", dto)));
            }
        });
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
