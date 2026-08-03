using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Reqnroll;
using RichardSzalay.MockHttp;
using VideoAnonymizer.Web.Pages;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.FakeServices;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Pages;

[Binding]
public sealed class MvpSurroundingFlowStepDefinitions
{
    private BunitContext _context = default!;
    private MockHttpMessageHandler _http = default!;
    private FakeJobHubClient _jobHub = default!;
    private IRenderedComponent<Home> _home = default!;
    private Guid _listedVideoId;
    private bool _deleteRequested;

    [BeforeScenario("mvp_surrounding_flow")]
    public void SetUp()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _http = new MockHttpMessageHandler();
        _jobHub = new FakeJobHubClient();
        _listedVideoId = Guid.NewGuid();
        _deleteRequested = false;

        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(_http));
        _context.Services.AddSingleton<IJobHubClient>(_jobHub);
        _context.Services.AddSingleton<IDownloadService>(new FakeDownloadService());
        _context.Render<MudPopoverProvider>();

        RespondAppState();
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Videos}")
            .Respond("application/json", Json(new ApiResponse<List<VideoDto>>
            {
                IsSuccess = true,
                Payload = []
            }));
    }

    [AfterScenario("mvp_surrounding_flow")]
    public async Task TearDown()
    {
        _http.Dispose();
        await _context.DisposeAsync();
    }

    [Given("the home page lists videos in imported analyzed and exported states")]
    public void GivenTheHomePageListsVideosInImportedAnalyzedAndExportedStates()
    {
        _http.Clear();
        RespondAppState();
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Videos}")
            .Respond("application/json", Json(new ApiResponse<List<VideoDto>>
            {
                IsSuccess = true,
                Payload =
                [
                    new VideoDto
                    {
                        Id = Guid.NewGuid(),
                        OriginalFileName = "imported.mp4",
                        HasAnalysis = false,
                        HasAnonymizedOutput = false,
                        Status = VideoListStatuses.Imported
                    },
                    new VideoDto
                    {
                        Id = Guid.NewGuid(),
                        OriginalFileName = "analyzed.mp4",
                        HasAnalysis = true,
                        HasAnonymizedOutput = false,
                        Status = VideoListStatuses.ReadyToReview
                    },
                    new VideoDto
                    {
                        Id = Guid.NewGuid(),
                        OriginalFileName = "exported.mp4",
                        HasAnalysis = true,
                        HasAnonymizedOutput = true,
                        Status = VideoListStatuses.Exported
                    }
                ]
            }));

        _home = _context.Render<Home>();
        _home.WaitForAssertion(() =>
            _home.FindAll("[data-testid='existing-video-row']").Count.Should().Be(3));
    }

    [Then("the listed video statuses are {string}, {string}, and {string}")]
    public void ThenTheListedVideoStatusesAre(string first, string second, string third)
    {
        var statuses = _home.FindAll("[data-testid='video-status']")
            .Select(e => e.TextContent.Trim())
            .ToList();

        statuses.Should().Equal(first, second, third);
    }

    [Given("the home page lists an imported video {string} ready to review")]
    public void GivenTheHomePageListsAnImportedVideoReadyToReview(string fileName)
    {
        _http.Clear();
        RespondAppState();

        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Videos}")
            .Respond(_ =>
            {
                // First load shows the video; after delete reload returns empty.
                var payload = !_deleteRequested
                    ? new List<VideoDto>
                    {
                        new()
                        {
                            Id = _listedVideoId,
                            OriginalFileName = fileName,
                            HasAnalysis = true,
                            HasAnonymizedOutput = false,
                            BlurSizePercent = 120,
                            TimeBufferMs = 300,
                            Status = VideoListStatuses.ReadyToReview
                        }
                    }
                    : [];

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(Json(new ApiResponse<List<VideoDto>>
                    {
                        IsSuccess = true,
                        Payload = payload
                    }))
                };
            });

        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Analyzed}/{_listedVideoId}")
            .Respond("application/json", Json(new ApiResponse<List<AnalyzedFrameDto>>
            {
                IsSuccess = true,
                Payload =
                [
                    new AnalyzedFrameDto
                    {
                        Id = Guid.NewGuid(),
                        VideoId = _listedVideoId,
                        TimeSeconds = 0,
                        DetectedObjects =
                        [
                            new DetectedObjectDto
                            {
                                Id = Guid.NewGuid(),
                                AnalyzedFrameId = Guid.NewGuid(),
                                ClassName = "face",
                                Confidence = 0.9,
                                Selected = true,
                                TrackId = 1,
                                X = 1,
                                Y = 1,
                                Width = 10,
                                Height = 10
                            }
                        ]
                    }
                ]
            }));

        _http.When(HttpMethod.Get, $"*/{SharedConstants.Paths.Actions}/{_listedVideoId}")
            .Respond("application/json", Json(new ApiResponse<List<EditorActionDto>>
            {
                IsSuccess = true,
                Payload = []
            }));

        _http.When(HttpMethod.Delete, $"/{SharedConstants.Paths.Video}/{_listedVideoId}")
            .Respond(_ =>
            {
                _deleteRequested = true;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(Json(new ApiResponse<DeleteVideoResultDto>
                    {
                        IsSuccess = true,
                        Payload = new DeleteVideoResultDto
                        {
                            VideoId = _listedVideoId,
                            DatabaseDeleted = true,
                            SourceFileDeleted = true,
                            AnonymizedFileDeleted = true
                        },
                        Message = "Working copy deleted."
                    }))
                };
            });

        _home = _context.Render<Home>();
        _home.WaitForAssertion(() =>
            _home.FindAll("[data-testid='existing-video-row']").Count.Should().BeGreaterThan(0));
    }

    [Then("the existing video row is keyboard focusable")]
    public void ThenTheExistingVideoRowIsKeyboardFocusable()
    {
        var row = _home.Find("[data-testid='existing-video-row']");
        row.GetAttribute("tabindex").Should().Be("0");
        row.GetAttribute("role").Should().Be("button");
        row.GetAttribute("aria-label").Should().Contain("Open");
    }

    [Then("the delete working copy action is keyboard accessible")]
    public void ThenTheDeleteWorkingCopyActionIsKeyboardAccessible()
    {
        var deleteButton = _home.Find("[data-testid='delete-working-copy']");
        deleteButton.GetAttribute("aria-label").Should().Be("Delete working copy");
        deleteButton.TagName.Should().BeEquivalentTo("button");
    }

    [When("the reviewer activates the existing video row with the keyboard")]
    public async Task WhenTheReviewerActivatesTheExistingVideoRowWithTheKeyboard()
    {
        var row = _home.Find("[data-testid='existing-video-row']");
        await row.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        _home.Render();
    }

    [Then("the review workspace opens for the listed video")]
    public void ThenTheReviewWorkspaceOpensForTheListedVideo()
    {
        _home.WaitForAssertion(() =>
        {
            _home.FindAll("[data-testid='export-button']").Count.Should().BeGreaterThan(0);
        }, TimeSpan.FromSeconds(5));
    }

    [Then("the delete action is labeled {string}")]
    public void ThenTheDeleteActionIsLabeled(string label)
    {
        var deleteButton = _home.Find("[data-testid='delete-working-copy']");
        deleteButton.GetAttribute("aria-label").Should().Be(label);
        _home.Markup.Should().Contain(label);
    }

    [Then("the imported videos UI does not claim local or cloud-only storage")]
    public void ThenTheImportedVideosUiDoesNotClaimLocalOrCloudOnlyStorage()
    {
        var markup = _home.Markup;
        markup.Should().NotContain("local copy");
        markup.Should().NotContain("Local copy");
        markup.Should().NotContain("cloud storage");
        markup.Should().NotContain("Cloud storage");
        markup.Should().NotContain("on this machine only");
        markup.Should().Contain("Delete working copy");
    }

    [When("the reviewer deletes the working copy for the listed video")]
    public async Task WhenTheReviewerDeletesTheWorkingCopyForTheListedVideo()
    {
        await _home.InvokeAsync(() => _home.Find("[data-testid='delete-working-copy']").Click());
        _home.Render();
    }

    [Then("the working copy delete request is sent")]
    public void ThenTheWorkingCopyDeleteRequestIsSent()
    {
        _home.WaitForAssertion(() => _deleteRequested.Should().BeTrue(), TimeSpan.FromSeconds(3));
    }

    [Then("the listed video is no longer shown")]
    public void ThenTheListedVideoIsNoLongerShown()
    {
        _home.WaitForAssertion(() =>
        {
            _home.FindAll("[data-testid='existing-video-row']").Should().BeEmpty();
        }, TimeSpan.FromSeconds(5));
    }

    private void RespondAppState()
    {
        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.AppState}")
            .Respond("application/json", Json(new AppStateDto
            {
                ObjectDetectionApiRunning = true,
                InterpolateTrackedObjects = true,
                IsStandalone = true
            }));

        _http.When(HttpMethod.Get, $"*/{SharedConstants.Paths.AppState}")
            .Respond("application/json", Json(new AppStateDto
            {
                ObjectDetectionApiRunning = true,
                InterpolateTrackedObjects = true,
                IsStandalone = true
            }));
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
