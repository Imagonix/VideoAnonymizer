using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
    public double BlurSizePercent { get; set; } = 120;

    [Parameter]
    public int TimeBufferMs { get; set; } = 300;

    private ElementReference _hostElement;
    private IJSObjectReference? _hostModule;
    private bool _mounted;
    private bool _loadFailed;

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
        if (!firstRender)
        {
            if (_mounted)
                await UpdateAsync();

            return;
        }

        _hostModule = await JS.InvokeAsync<IJSObjectReference>(
            "import",
            "/_content/VideoAnonymizer.Web.Modules/js/videoEditorHost.js");

        await _hostModule.InvokeVoidAsync(
            "mountVideoEditor",
            _hostElement,
            BuildProps());

        _mounted = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_mounted)
            await UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        if (_hostModule is null)
            return;

        Frames = (await GetFramesAsync()).ToList();
        await _hostModule.InvokeVoidAsync(
            "updateVideoEditor",
            _hostElement,
            BuildProps());
    }

    private object BuildProps()
    {
        return new
        {
            videoId = VideoId,
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