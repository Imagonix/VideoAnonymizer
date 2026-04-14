using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.Web.Modules.Components;

public partial class VideoEditor : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired] public Guid VideoId { get; set; }
    [Parameter] public string VideoSourceUrl { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<AnalyzedFrameDto> Frames { get; set; } = Array.Empty<AnalyzedFrameDto>();

    private ElementReference _hostElement;
    private IJSObjectReference? _hostModule;
    private bool _mounted;
    private bool _loadFailed;

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
            frames = Frames
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