using Microsoft.JSInterop;

namespace VideoAnonymizer.Web.Services;

public class DownloadService : IDownloadService
{
    private readonly IJSRuntime _js;

    public DownloadService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task DownloadFileAsync(string fileName, string url)
    {
        var module = await _js.InvokeAsync<IJSObjectReference>("import", "./Pages/Home.razor.js");
        await module.InvokeVoidAsync("triggerFileDownload", fileName, url);
        await module.DisposeAsync();
    }
}
