using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Utils;

namespace VideoAnonymizer.Web.Pages
{
    public partial class Home : ComponentBase
    {
        private IDisposable? _videoAnalyzedSubscription;
        private IDisposable? _videoAnonymizedSubscription;

        private ReviewExportTab _reviewExportTab;

        private IBrowserFile? _selectedFile;
        private Guid? _currentVideoId;
        private Guid? _anonymizeVideoJobId;

        private int _activeTabIndex = 0;

        public bool IsAnonymized { get; private set; }

        private bool IsBusy { get; set; }
        private string? StatusText { get; set; }
        private string? SelectedFileName { get; set; }
        private List<AnalyzedFrameDto> _analyzedFrames = [];
        private bool _showEditor;
        private string? _videoSourceUrl;

        protected override async Task OnInitializedAsync()
        {
            _videoAnalyzedSubscription = JobHubClient.OnVideoAnalyzed(async message =>
            {
                if (_currentVideoId.IsNullOrEmpty())
                    return;

                if (!message.JobId.Equals(_currentVideoId))
                    return;

                StatusText = "Video analyzed. Loading editor...";
                await LoadAnalyzedFramesAsync(_currentVideoId);
                _showEditor = true;
                _activeTabIndex = 1;
                Snackbar.Add("Video processed. You can now review detected objects.", Severity.Success);

                await InvokeAsync(StateHasChanged);
            });

            _videoAnonymizedSubscription = JobHubClient.OnVideoAnonymized(async message =>
            {
                _anonymizeVideoJobId = message.JobId;
                IsAnonymized = true;
                IsBusy = false;
                StatusText = "Video anonymized successfully.";
                Snackbar.Add("Video anonymized successfully. You can now download it.", Severity.Success);

                await InvokeAsync(StateHasChanged);
            });

            await JobHubClient.StartAsync();
        }

        private async Task OnFileSelected(IBrowserFile? file)
        {
            if (file is null)
                return;

            _selectedFile = file;
            SelectedFileName = file.Name;
            IsAnonymized = false;
            _currentVideoId = null;
            _anonymizeVideoJobId = null;
            IsBusy = true;
            StatusText = "Uploading and analyzing video...";

            await InvokeAsync(StateHasChanged);

            try
            {
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream(long.MaxValue);

                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "video", file.Name);

                using var httpClient = HttpClientFactory.CreateClient("ApiService");
                var response = await httpClient.PostAsync($"/{SharedConstants.Paths.Analyze}", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

                if (result is null || result.Payload.IsNullOrEmpty())
                    throw new InvalidOperationException("Analyze endpoint did not return a video id.");

                _currentVideoId = result.Payload;
                StatusText = "Video uploaded. Analyzing frames ...";
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusText = "Upload failed.";
                Snackbar.Add($"Upload failed: {ex.Message}", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadAnalyzedFramesAsync(Guid? videoId)
        {
            if (videoId.IsNullOrEmpty())
                return;

            try
            {
                using var httpClient = HttpClientFactory.CreateClient("ApiService");

                var analyzedResponse = await httpClient.GetAsync($"/{SharedConstants.Paths.Analyzed}/{videoId}");
                analyzedResponse.EnsureSuccessStatusCode();

                var result = await analyzedResponse.Content.ReadFromJsonAsync<ApiResponse<List<AnalyzedFrameDto>>>();

                _analyzedFrames = result?.Payload ?? [];
                IsBusy = false;
                StatusText = "Analysis loaded. You can now review detected objects.";

                _videoSourceUrl = $"{httpClient.BaseAddress}{SharedConstants.Paths.Video}/{videoId}";
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusText = "Loading editor failed.";
                Snackbar.Add($"Loading editor failed: {ex.Message}", Severity.Error);
            }
        }

        private async Task StartAnonymizationAsync()
        {
            if (_currentVideoId.IsNullOrEmpty())
                return;

            try
            {
                IsBusy = true;

                _analyzedFrames = _reviewExportTab.Frames.ToList();

                using var httpClient = HttpClientFactory.CreateClient("ApiService");
                var response = await httpClient.PostAsJsonAsync(
                    $"/{SharedConstants.Paths.Anonymize}/{_currentVideoId}",
                    _analyzedFrames);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

                if (result is not null && !result.Payload.IsNullOrEmpty())
                {
                    _anonymizeVideoJobId = result.Payload;
                }

                StatusText = "Anonymization started. Waiting for completion...";
            }
            catch (Exception ex)
            {
                IsBusy = false;
                StatusText = "Anonymization failed.";
                Snackbar.Add($"Anonymization failed: {ex.Message}", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task DownloadAsync()
        {
            if (_anonymizeVideoJobId.IsNullOrEmpty())
                return;

            try
            {
                using var httpClient = HttpClientFactory.CreateClient("ApiService");
                var url = $"{httpClient.BaseAddress}{SharedConstants.Paths.Anonymized}/{_currentVideoId}";

                StatusText = "Download started.";
                await InvokeAsync(StateHasChanged);

                await DownloadService.DownloadFileAsync($"anonymized-{SelectedFileName}", url);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _videoAnalyzedSubscription?.Dispose();
            _videoAnonymizedSubscription?.Dispose();
            await JobHubClient.StopAsync();
        }
    }
}
