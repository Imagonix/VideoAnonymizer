using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Utils;

namespace VideoAnonymizer.Web.Pages
{
    public partial class Home : ComponentBase
    {
        private IDisposable? _videoAnalyzedSubscription;
        private IDisposable? _videoAnonymizedSubscription;
        private IDisposable? _jobProgressSubscription;

        private ReviewExportTab? _reviewExportTab;

        private IBrowserFile? _selectedFile;
        private Guid? _currentVideoId;
        private Guid? _anonymizeVideoJobId;

        private int _activeTabIndex = 0;

        public bool IsAnonymized { get; private set; }

        private bool IsBusy { get; set; }
        private string? StatusText { get; set; }
        private int? ProgressPercent { get; set; }
        private string? SelectedFileName { get; set; }
        private List<AnalyzedFrameDto> _analyzedFrames = [];
        private bool _showEditor;
        private string? _videoSourceUrl;
        private List<VideoDto>? _existingVideos;

        private int _blurSizePercent = 120;
        private int _timeBufferMs = 300;

        private int DetectionIntervalMs { get; set; } = 100;

        [Parameter]
        public AppStateDto? InitialAppState { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadExistingVideosAsync();

            _videoAnalyzedSubscription = JobHubClient.OnVideoAnalyzed(async message =>
            {
                if (_currentVideoId.IsNullOrEmpty())
                    return;

                if (!message.JobId.Equals(_currentVideoId))
                    return;

                StatusText = "Video analyzed. Loading editor...";
                await LoadAnalyzedFramesAsync(_currentVideoId);
                await LoadExistingVideosAsync();
                _showEditor = true;
                _activeTabIndex = 1;
                _selectedFile = null;
                Snackbar.Add("Video processed. You can now review detected objects.", Severity.Success);

                await InvokeAsync(StateHasChanged);
            });

            _videoAnonymizedSubscription = JobHubClient.OnVideoAnonymized(async message =>
            {
                if (!_anonymizeVideoJobId.IsNullOrEmpty() && message.JobId != _anonymizeVideoJobId)
                    return;

                _anonymizeVideoJobId = message.JobId;
                IsAnonymized = true;
                ProgressPercent = 100;
                IsBusy = false;
                StatusText = "Video anonymized successfully. Downloading...";
                Snackbar.Add("Video anonymized successfully. Download starting...", Severity.Success);

                await InvokeAsync(StateHasChanged);
                await DownloadAsync();
            });

            _jobProgressSubscription = JobHubClient.OnJobProgress(async message =>
            {
                if (!IsProgressForCurrentJob(message))
                    return;

                ProgressPercent = message.ProgressPercent is null
                    ? null
                    : Math.Clamp(message.ProgressPercent.Value, 0, 100);

                if (!string.IsNullOrWhiteSpace(message.Status))
                {
                    StatusText = message.Status;
                }

                await InvokeAsync(StateHasChanged);
            });

            await JobHubClient.StartAsync();
        }

        private Task OnFileSelected(IBrowserFile? file)
        {
            _selectedFile = file;
            SelectedFileName = file?.Name;

            IsAnonymized = false;
            _currentVideoId = null;
            _anonymizeVideoJobId = null;
            _showEditor = false;
            _videoSourceUrl = null;
            _analyzedFrames = [];
            _activeTabIndex = 0;
            ProgressPercent = null;

            return Task.CompletedTask;
        }

        private async Task DetectObjectsAsync()
        {
            if (_selectedFile is null)
                return;

            try
            {
                IsBusy = true;
                ProgressPercent = null;
                StatusText = "Uploading and analyzing video...";

                await InvokeAsync(StateHasChanged);

                using var content = new MultipartFormDataContent();
                await using var stream = _selectedFile.OpenReadStream(long.MaxValue);

                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(_selectedFile.ContentType);
                content.Add(fileContent, "video", _selectedFile.Name);

                using var httpClient = HttpClientFactory.CreateClient("ApiService");

                var url =
                    $"/{SharedConstants.Paths.Analyze}?detectionIntervalMs={DetectionIntervalMs.ToString(CultureInfo.InvariantCulture)}";

                var response = await httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

                if (result is null || result.Payload.IsNullOrEmpty())
                    throw new InvalidOperationException("Analyze endpoint did not return a video id.");

                _currentVideoId = result.Payload;
                StatusText = "Video uploaded. Analyzing frames...";
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ProgressPercent = null;
                StatusText = "Upload failed.";
                Snackbar.Add($"Upload failed: {ex.Message}", Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadExistingVideosAsync()
        {
            try
            {
                using var httpClient = HttpClientFactory.CreateClient("ApiService");
                var response = await httpClient.GetAsync($"/{SharedConstants.Paths.Videos}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<VideoDto>>>();
                _existingVideos = result?.Payload;
            }
            catch
            {
                _existingVideos = null;
            }
        }

        private async Task OnExistingVideoSelected(Guid videoId)
        {
            await LoadExistingVideosAsync();
            _currentVideoId = videoId;
            _selectedFile = null;
            var videoDto = _existingVideos?.FirstOrDefault(v => v.Id == videoId);
            SelectedFileName = videoDto?.OriginalFileName;
            _blurSizePercent = videoDto?.BlurSizePercent ?? 120;
            _timeBufferMs = videoDto?.TimeBufferMs ?? 300;
            IsAnonymized = false;
            _anonymizeVideoJobId = null;
            _showEditor = false;
            _videoSourceUrl = null;
            _analyzedFrames = [];

            StatusText = "Opening Video...";
            IsBusy = true;
            await InvokeAsync(StateHasChanged);

            await LoadAnalyzedFramesAsync(videoId);

            if (_analyzedFrames.Count > 0)
            {
                _showEditor = true;
                _activeTabIndex = 1;
                StatusText = "Video loaded. You can review detected objects.";
            }
            else
            {
                StatusText = "No analysis found for this video. Upload and detect objects to analyze it.";
            }

            IsBusy = false;
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
                ProgressPercent = null;
                StatusText = "Analysis loaded. You can now review detected objects.";

                _videoSourceUrl = $"{httpClient.BaseAddress}{SharedConstants.Paths.Video}/{videoId}";
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ProgressPercent = null;
                StatusText = "Loading editor failed.";
                Snackbar.Add($"Loading editor failed: {ex.Message}", Severity.Error);
            }
        }

        private async Task StartAnonymizationAsync()
        {
            if (_currentVideoId.IsNullOrEmpty() || _reviewExportTab is null)
                return;

            try
            {
                IsBusy = true;
                ProgressPercent = null;
                StatusText = "Starting anonymization...";

                var frames = _reviewExportTab.CapturedFrames.ToList();

                await InvokeAsync(StateHasChanged);

                var request = new AnonymizeVideoRequestDto()
                {
                    Frames = frames,
                    Settings = new()
                    {
                        BlurSizePercent = _reviewExportTab.BlurSizePercent,
                        TimeBufferMs = _reviewExportTab.TimeBufferMs,
                    }

                };


                using var httpClient = HttpClientFactory.CreateClient("ApiService");
                var response = await httpClient.PostAsJsonAsync(
                    $"/{SharedConstants.Paths.Anonymize}/{_currentVideoId}",
                    request);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

                if (result is not null && !result.Payload.IsNullOrEmpty())
                {
                    _anonymizeVideoJobId = result.Payload;
                }

                if (ProgressPercent is null)
                {
                    StatusText = "Anonymization started. Waiting for completion...";
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ProgressPercent = null;
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
            _jobProgressSubscription?.Dispose();
            await JobHubClient.StopAsync();
        }

        private bool IsProgressForCurrentJob(LongRunningJobProgressMessage message)
        {
            if (!IsBusy)
                return false;

            if (!_currentVideoId.IsNullOrEmpty() && message.JobId == _currentVideoId)
                return true;

            if (!_anonymizeVideoJobId.IsNullOrEmpty() && message.JobId == _anonymizeVideoJobId)
                return true;

            return !_currentVideoId.IsNullOrEmpty() && message.VideoId == _currentVideoId;
        }
    }
}
