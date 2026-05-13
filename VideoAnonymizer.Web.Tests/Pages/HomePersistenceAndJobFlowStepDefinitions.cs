using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Reqnroll;
using RichardSzalay.MockHttp;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Pages;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.FakeServices;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Pages;

[Binding]
public sealed class HomePersistenceAndJobFlowStepDefinitions
{
    private BunitContext _context = default!;
    private MockHttpMessageHandler _http = default!;
    private FakeJobHubClient _jobHub = default!;
    private FakeDownloadService _downloadService = default!;
    private IRenderedComponent<Home> _cut = default!;

    private Guid _videoId;
    private Guid _anonymizeJobId;
    private Guid _frameId;
    private Guid _objectId;
    private string _savedFileName = string.Empty;

    [BeforeScenario("home_persistence")]
    public void SetUp()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _http = new MockHttpMessageHandler();
        _jobHub = new FakeJobHubClient();
        _downloadService = new FakeDownloadService();

        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(_http));
        _context.Services.AddSingleton<IJobHubClient>(_jobHub);
        _context.Services.AddSingleton<IDownloadService>(_downloadService);
        _context.Render<MudPopoverProvider>();
    }

    [AfterScenario("home_persistence")]
    public async Task TearDown()
    {
        _http.Dispose();
        await _context.DisposeAsync();
    }

    [Given("the saved video list contains {string} with blur size {int} percent and time buffer {int} ms")]
    public void GivenTheSavedVideoListContainsVideoWithSettings(string fileName, int blurSizePercent, int timeBufferMs)
    {
        _videoId = Guid.NewGuid();
        _savedFileName = fileName;
        RespondVideos(
        [
            new VideoDto
            {
                Id = _videoId,
                OriginalFileName = fileName,
                BlurSizePercent = blurSizePercent,
                TimeBufferMs = timeBufferMs
            }
        ]);
    }

    [Given("the saved video has one analyzed frame with one face")]
    public void GivenTheSavedVideoHasOneAnalyzedFrameWithOneFace()
    {
        _frameId = Guid.NewGuid();
        _objectId = Guid.NewGuid();
        var frames = new List<AnalyzedFrameDto>
        {
            CreateFrame(_videoId, _frameId, [CreateObject(_objectId, _frameId, trackId: 4)])
        };

        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Analyzed}/{_videoId}")
            .Respond("application/json", Json(new ApiResponse<List<AnalyzedFrameDto>> { IsSuccess = true, Payload = frames }));
    }

    [When("the reviewer opens the saved video from the library")]
    public void WhenTheReviewerOpensTheSavedVideoFromTheLibrary()
    {
        RenderHome();
        _cut.WaitForAssertion(() => _cut.Markup.Should().Contain(_savedFileName));

        var row = _cut.FindAll("div")
            .Single(element => element.TextContent.Contains(_savedFileName)
                && (element.GetAttribute("style")?.Contains("cursor: pointer") ?? false));
        row.Click();
    }

    [Then("the review tab opens with the persisted frame, face, blur size {int} percent and time buffer {int} ms")]
    public void ThenTheReviewTabOpensWithPersistedFrameFaceAndSettings(int blurSizePercent, int timeBufferMs)
    {
        _cut.WaitForAssertion(() =>
        {
            var review = _cut.FindComponent<ReviewExportTab>().Instance;
            review.ShowEditor.Should().BeTrue();
            review.VideoId.Should().Be(_videoId);
            review.VideoSourceUrl.Should().Contain($"/{SharedConstants.Paths.Video}/{_videoId}");
            review.BlurSizePercent.Should().Be(blurSizePercent);
            review.TimeBufferMs.Should().Be(timeBufferMs);
            review.Frames.Should().ContainSingle(frame =>
                frame.Id == _frameId
                && frame.DetectedObjects.Single().Id == _objectId);
        });
    }

    [Then("the upload form does not mark the saved video as a freshly selected file")]
    public void ThenTheUploadFormDoesNotMarkTheSavedVideoAsFreshlySelected()
    {
        _cut.Markup.Should().NotContain($"Select Video: {_savedFileName}");
    }

    [Given("the reviewer starts object detection for {string}")]
    public void GivenTheReviewerStartsObjectDetectionFor(string fileName)
    {
        _videoId = Guid.NewGuid();
        _anonymizeJobId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _objectId = Guid.NewGuid();

        RespondVideos([]);
        _http.Expect(HttpMethod.Post, $"/{SharedConstants.Paths.Analyze}*")
            .Respond("application/json", Json(new ApiResponse<Guid> { IsSuccess = true, Payload = _videoId }));
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Analyzed}/{_videoId}")
            .Respond("application/json", Json(new ApiResponse<List<AnalyzedFrameDto>>
            {
                IsSuccess = true,
                Payload =
                [
                    CreateFrame(_videoId, _frameId, [CreateObject(_objectId, _frameId, trackId: 1)])
                ]
            }));

        RenderHome();

        var dummyVideo = InputFileContent.CreateFromBinary(
            [0, 1, 2, 3, 4, 5],
            fileName,
            contentType: "video/mp4");

        _cut.FindComponent<MudFileUpload<IBrowserFile>>()
            .FindComponent<InputFile>()
            .UploadFiles([dummyVideo]);

        _cut.FindAll("button").Single(button => button.TextContent.Contains("Detect Objects")).Click();
        _cut.WaitForAssertion(() => _http.VerifyNoOutstandingExpectation());
    }

    [Given("the reviewer is reviewing analyzed results for {string}")]
    public async Task GivenTheReviewerIsReviewingAnalyzedResultsFor(string fileName)
    {
        GivenTheReviewerStartsObjectDetectionFor(fileName);
        await WhenTheCurrentAnalysisCompletionMessageArrives();
        ThenTheReviewerIsMovedToReview();
    }

    [Given("the reviewer has started anonymization for analyzed results in {string}")]
    public async Task GivenTheReviewerHasStartedAnonymizationForAnalyzedResultsIn(string fileName)
    {
        await GivenTheReviewerIsReviewingAnalyzedResultsFor(fileName);
        await StartAnonymizationAsync(raiseUnrelatedCompletion: false);
    }

    [When("an unrelated analysis completion message arrives")]
    public async Task WhenAnUnrelatedAnalysisCompletionMessageArrives()
    {
        await _jobHub.RaiseVideoAnalyzedAsync(new LongRunningJobFinishedMessage
        {
            JobId = Guid.NewGuid(),
            Status = "completed"
        });
    }

    [Then("the reviewer is not moved to review")]
    public void ThenTheReviewerIsNotMovedToReview()
    {
        _cut.FindComponents<ReviewExportTab>().Should().BeEmpty();
    }

    [When("the current analysis completion message arrives")]
    public async Task WhenTheCurrentAnalysisCompletionMessageArrives()
    {
        await _jobHub.RaiseVideoAnalyzedAsync(new LongRunningJobFinishedMessage
        {
            JobId = _videoId,
            Status = "completed"
        });
    }

    [Then("the reviewer is moved to review")]
    public void ThenTheReviewerIsMovedToReview()
    {
        _cut.WaitForAssertion(() => _cut.FindComponent<ReviewExportTab>().Instance.ShowEditor.Should().BeTrue());
    }

    [When("the reviewer starts anonymization and an unrelated export completion message arrives")]
    public async Task WhenTheReviewerStartsAnonymizationAndAnUnrelatedExportCompletionArrives()
    {
        await StartAnonymizationAsync(raiseUnrelatedCompletion: true);
    }

    private async Task StartAnonymizationAsync(bool raiseUnrelatedCompletion)
    {
        _http.Expect(HttpMethod.Post, $"/{SharedConstants.Paths.Anonymize}/{_videoId}")
            .Respond("application/json", Json(new ApiResponse<Guid> { IsSuccess = true, Payload = _anonymizeJobId }));

        _cut.FindAll("button").Single(button => button.TextContent.Contains("Export")).Click();
        _cut.WaitForAssertion(() => _http.VerifyNoOutstandingExpectation());

        if (raiseUnrelatedCompletion)
        {
            await _jobHub.RaiseVideoAnonymizedAsync(new LongRunningJobFinishedMessage
            {
                JobId = Guid.NewGuid(),
                Status = "completed"
            });
        }
    }

    [Then("no anonymized video is downloaded")]
    public void ThenNoAnonymizedVideoIsDownloaded()
    {
        _downloadService.DownloadCallCount.Should().Be(0);
    }

    [When("the current export completion message arrives")]
    public async Task WhenTheCurrentExportCompletionMessageArrives()
    {
        await _jobHub.RaiseVideoAnonymizedAsync(new LongRunningJobFinishedMessage
        {
            JobId = _anonymizeJobId,
            Status = "completed"
        });
    }

    [Then("the anonymized video is downloaded for the current video")]
    public void ThenTheAnonymizedVideoIsDownloadedForTheCurrentVideo()
    {
        _cut.WaitForAssertion(() =>
        {
            _downloadService.DownloadCallCount.Should().Be(1);
            _downloadService.LastDownloadedUrl.Should().Contain($"/{SharedConstants.Paths.Anonymized}/{_videoId}");
        });
    }

    private void RenderHome()
    {
        _cut ??= _context.Render<Home>();
    }

    private void RespondVideos(List<VideoDto> videos)
    {
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Videos}")
            .Respond("application/json", Json(new ApiResponse<List<VideoDto>> { IsSuccess = true, Payload = videos }));
    }

    private static AnalyzedFrameDto CreateFrame(Guid videoId, Guid frameId, IReadOnlyList<DetectedObjectDto> objects) =>
        new()
        {
            Id = frameId,
            VideoId = videoId,
            TimeSeconds = 1.0,
            DetectedObjects = objects.ToList()
        };

    private static DetectedObjectDto CreateObject(Guid objectId, Guid frameId, int trackId) =>
        new()
        {
            Id = objectId,
            AnalyzedFrameId = frameId,
            Confidence = 0.95,
            ClassName = "face",
            Selected = true,
            TrackId = trackId,
            X = 10,
            Y = 20,
            Width = 30,
            Height = 40
        };

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
