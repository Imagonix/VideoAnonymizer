using System.Reflection;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Reqnroll;
using RichardSzalay.MockHttp;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.FakeServices;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Components;

[Binding]
public sealed class ReviewExportStepDefinitions
{
    private BunitContext _context = default!;
    private MockHttpMessageHandler _http = default!;
    private FakeJobHubClient _jobHub = default!;
    private IRenderedComponent<ReviewExportTab> _cut = default!;
    private Guid _videoId;
    private int _startAnonymizationCount;
    private int _downloadCount;
    private bool _exportResultAvailable;
    private bool _isExportRunning;
    private List<AnalyzedFrameDto> _frames = [];
    private int _blur = 150;
    private int _buffer = 400;

    [BeforeScenario("review_export")]
    public void SetUp()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _http = new MockHttpMessageHandler();
        _jobHub = new FakeJobHubClient();
        _startAnonymizationCount = 0;
        _downloadCount = 0;
        _exportResultAvailable = false;
        _isExportRunning = false;
        _videoId = Guid.NewGuid();
        _frames = [];
        _blur = 150;
        _buffer = 400;

        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(_http));
        _context.Services.AddSingleton<IJobHubClient>(_jobHub);
        _context.Services.AddSingleton<IDownloadService>(new FakeDownloadService());
        _context.Render<MudPopoverProvider>();

        _http.When(HttpMethod.Get, $"/{SharedConstants.Paths.Actions}/{_videoId}")
            .Respond("application/json", Json(new ApiResponse<List<EditorActionDto>>
            {
                IsSuccess = true,
                Payload = []
            }));
    }

    [AfterScenario("review_export")]
    public async Task TearDown()
    {
        _http.Dispose();
        await _context.DisposeAsync();
    }

    [Given("the review editor is open with mixed track inclusion")]
    public void GivenTheReviewEditorIsOpenWithMixedTrackInclusion()
    {
        var frame1 = Guid.NewGuid();
        var frame2 = Guid.NewGuid();
        _frames =
        [
            new()
            {
                Id = frame1,
                VideoId = _videoId,
                TimeSeconds = 0,
                DetectedObjects =
                [
                    Obj(frame1, trackId: 1, selected: true),
                    Obj(frame1, trackId: 2, selected: true),
                    Obj(frame1, trackId: 3, selected: false),
                ]
            },
            new()
            {
                Id = frame2,
                VideoId = _videoId,
                TimeSeconds = 1,
                DetectedObjects =
                [
                    Obj(frame2, trackId: 1, selected: true),
                    Obj(frame2, trackId: 2, selected: false),
                    Obj(frame2, trackId: 3, selected: false),
                ]
            }
        ];

        RenderTab();
    }

    [Then("the save state label is {string}")]
    public void ThenTheSaveStateLabelIs(string label)
    {
        _cut.WaitForAssertion(() =>
            _cut.Find("[data-testid='save-state-label']").TextContent.Trim().Should().Be(label));
    }

    [Then("the save state label is not {string} when errors are present")]
    public void ThenTheSaveStateLabelIsNotWhenErrorsArePresent(string forbidden)
    {
        SetCoordinatorFlag("_actionCoordinator", "HasErrors", true);
        _cut.Render();
        _cut.WaitForAssertion(() =>
        {
            var text = _cut.Find("[data-testid='save-state-label']").TextContent;
            text.Should().NotContain(forbidden);
            text.Should().Contain("Save failed");
        });
    }

    [When("editor actions are pending")]
    public void WhenEditorActionsArePending()
    {
        var coordinator = GetPrivateField<object>(_cut.Instance, "_actionCoordinator")!;
        var queue = GetPrivateField<object>(coordinator, "_actionQueue")!;
        SetPrivateField(queue, "_pendingCount", 1);
        _cut.Render();
    }

    [When("undo redo is applying")]
    public void WhenUndoRedoIsApplying()
    {
        SetPrivateField(_cut.Instance, "_editorInputBlocked", true);
        _cut.Render();
    }

    [When("track forward is active")]
    public void WhenTrackForwardIsActive()
    {
        var tracker = GetPrivateField<object>(_cut.Instance, "_trackForwardCoordinator")!;
        SetPrivateField(tracker, "ActiveCount", 1);
        _cut.Render();
    }

    [When("persistence has errors")]
    public void WhenPersistenceHasErrors()
    {
        SetCoordinatorFlag("_actionCoordinator", "HasErrors", true);
        _cut.Render();
    }

    [When("anonymization is running")]
    public void WhenAnonymizationIsRunning()
    {
        _isExportRunning = true;
        RenderTab();
    }

    [When("anonymization succeeds again")]
    public void WhenAnonymizationSucceedsAgain()
    {
        _isExportRunning = false;
        _exportResultAvailable = true;
        RenderTab();
    }

    [Then("export is disabled because {string}")]
    public void ThenExportIsDisabledBecause(string reason)
    {
        _cut.WaitForAssertion(() =>
        {
            _cut.Instance.CanExport.Should().BeFalse();
            _cut.Instance.ExportDisabledReason.Should().Be(reason);
            var button = _cut.Find("[data-testid='export-button']");
            button.HasAttribute("disabled").Should().BeTrue();
            _cut.Markup.Should().Contain(reason);
        });
    }

    [When("the reviewer clicks Export anonymized video")]
    public void WhenTheReviewerClicksExportAnonymizedVideo()
    {
        // Ensure capture has current frames even if the Vue host is not mounted in bUnit.
        SetPrivateField(_cut.Instance, "_capturedFrames", _frames.ToList());
        _cut.Find("[data-testid='export-button']").Click();
    }

    [Then("anonymization is started with the current frames")]
    public void ThenAnonymizationIsStartedWithTheCurrentFrames()
    {
        _cut.WaitForAssertion(() =>
        {
            _startAnonymizationCount.Should().BeGreaterThan(0);
            _cut.Instance.CapturedFrames.Should().NotBeEmpty();
        });
    }

    [Then("no export confirmation is shown")]
    public void ThenNoExportConfirmationIsShown()
    {
        _cut.FindAll("[data-testid='export-confirm-dialog']").Should().BeEmpty();
        _cut.Markup.Should().NotContain("export-confirm");
        _cut.Markup.Should().NotContain("fully anonymized");
        _cut.Markup.Should().NotContain("partially anonymized");
    }

    [Then("the Download button is not shown")]
    public void ThenTheDownloadButtonIsNotShown()
    {
        _cut.FindAll("[data-testid='download-button']").Should().BeEmpty();
    }

    [Given("the review editor finished export with automatic download")]
    public void GivenTheReviewEditorFinishedExportWithAutomaticDownload()
    {
        GivenTheReviewEditorIsOpenWithMixedTrackInclusion();
        _exportResultAvailable = true;
        _isExportRunning = false;
        RenderTab();
    }

    [Given("the review editor finished export with a failed automatic download")]
    public void GivenTheReviewEditorFinishedExportWithAFailedAutomaticDownload()
    {
        // Recovery uses the same compact Download control regardless of auto-download success.
        GivenTheReviewEditorFinishedExportWithAutomaticDownload();
    }

    [Then("the Download button is shown beside Export anonymized video")]
    public void ThenTheDownloadButtonIsShownBesideExport()
    {
        _cut.WaitForAssertion(() =>
        {
            var export = _cut.Find("[data-testid='export-button']");
            var download = _cut.Find("[data-testid='download-button']");
            export.TextContent.Should().Contain("Export anonymized video");
            download.TextContent.Trim().Should().Be("Download");
            download.TextContent.Should().NotContain("again");

            // Same toolbar row: both buttons are present in markup without a separate result panel.
            _cut.FindAll("[data-testid='export-result-banner']").Should().BeEmpty();
            _cut.Markup.Should().NotContain("coverage summary");
        });
    }

    [Then("the Download button is enabled")]
    public void ThenTheDownloadButtonIsEnabled()
    {
        _cut.WaitForAssertion(() =>
        {
            _cut.Instance.CanDownload.Should().BeTrue();
            _cut.Find("[data-testid='download-button']").HasAttribute("disabled").Should().BeFalse();
        });
    }

    [Then("the Download button is disabled because {string}")]
    public void ThenTheDownloadButtonIsDisabledBecause(string reason)
    {
        _cut.WaitForAssertion(() =>
        {
            _cut.Instance.CanDownload.Should().BeFalse();
            _cut.Instance.DownloadDisabledReason.Should().Be(reason);
            _cut.Find("[data-testid='download-button']").HasAttribute("disabled").Should().BeTrue();
        });
    }

    [Then("the editor remains available")]
    public void ThenTheEditorRemainsAvailable()
    {
        _cut.Instance.ShowEditor.Should().BeTrue();
        _cut.Instance.VideoId.Should().Be(_videoId);
        _cut.Markup.Should().NotContain("completion screen");
    }

    [When("the reviewer clicks Download")]
    public async Task WhenTheReviewerClicksDownload()
    {
        await _cut.InvokeAsync(() => _cut.Find("[data-testid='download-button']").Click());
    }

    [Then("another download is requested")]
    public void ThenAnotherDownloadIsRequested()
    {
        _downloadCount.Should().BeGreaterThan(0);
    }

    private void RenderTab()
    {
        _cut = _context.Render<ReviewExportTab>(parameters => parameters
            .Add(p => p.VideoId, _videoId)
            .Add(p => p.VideoSourceUrl, $"http://localhost/video/{_videoId}")
            .Add(p => p.Frames, _frames)
            .Add(p => p.ShowEditor, true)
            .Add(p => p.BlurSizePercent, _blur)
            .Add(p => p.TimeBufferMs, _buffer)
            .Add(p => p.ExportResultAvailable, _exportResultAvailable)
            .Add(p => p.IsExportRunning, _isExportRunning)
            .Add(p => p.StartAnonymization, EventCallback.Factory.Create(this, () =>
            {
                _startAnonymizationCount++;
                return Task.CompletedTask;
            }))
            .Add(p => p.DownloadAnonymized, EventCallback.Factory.Create(this, () =>
            {
                _downloadCount++;
                return Task.CompletedTask;
            })));
    }

    private void SetCoordinatorFlag(string coordinatorField, string propertyName, bool value)
    {
        var coordinator = GetPrivateField<object>(_cut.Instance, coordinatorField)!;
        var prop = coordinator.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop?.SetMethod is not null)
        {
            prop.SetValue(coordinator, value);
            return;
        }

        var field = coordinator.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? coordinator.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field!.SetValue(coordinator, value);
    }

    private static T? GetPrivateField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return (T?)field?.GetValue(target);
    }

    private static void SetPrivateField(object target, string name, object? value)
    {
        var type = target.GetType();
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? type.GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null && type.BaseType is not null)
        {
            field = type.BaseType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? type.BaseType.GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop?.SetMethod is not null)
        {
            prop.SetValue(target, value);
            return;
        }

        field!.SetValue(target, value);
    }

    private static DetectedObjectDto Obj(Guid frameId, int trackId, bool selected) =>
        new()
        {
            Id = Guid.NewGuid(),
            AnalyzedFrameId = frameId,
            Confidence = 0.9,
            ClassName = "face",
            Selected = selected,
            TrackId = trackId,
            X = 1,
            Y = 2,
            Width = 10,
            Height = 10
        };

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
