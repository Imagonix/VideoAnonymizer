using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Reqnroll;
using VideoAnonymizer.Web.Components;
using VideoAnonymizer.Web.Components.ReviewExport;
using VideoAnonymizer.Web.Modules.Components;
using VideoAnonymizer.Web.Services;
using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Shared.DTO;
using VideoAnonymizer.Web.Tests.TestDoubles;

namespace VideoAnonymizer.Web.Tests.Components;

[Binding]
public sealed class ReviewExportTabPersistenceStepDefinitions(ScenarioContext scenarioContext)
{
    private BunitContext _context = default!;
    private BunitJSModuleInterop _editorModule = default!;
    private RecordingHttpMessageHandler _http = default!;
    private FakeJobHubClient _jobHubClient = default!;
    private IRenderedComponent<ReviewExportTab> _cut = default!;
    private Guid _videoId;
    private Guid _frameId;
    private Guid _faceId;
    private Guid _addedFaceId;
    private int _faceSaveAssertionIndex;
    private int _settingsSaveAssertionIndex;
    private int _requestCountBeforeRedo;
    private Guid _trackingJobId;
    private Guid _streamedFaceId;

    private Guid RestoredTrackedFaceId
    {
        get => scenarioContext.Get<Guid>(nameof(RestoredTrackedFaceId));
        set => scenarioContext.Set(value, nameof(RestoredTrackedFaceId));
    }

    private List<Guid> StreamedTrackedFaceIds
    {
        get => scenarioContext.Get<List<Guid>>(nameof(StreamedTrackedFaceIds));
        set => scenarioContext.Set(value, nameof(StreamedTrackedFaceIds));
    }

    [BeforeScenario("review_editor_persistence", Order = 0)]
    public void SetUp()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _http = new RecordingHttpMessageHandler();
        _jobHubClient = new FakeJobHubClient();

        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IHttpClientFactory>(new RecordingHttpClientFactory(_http));
        _context.Services.AddSingleton<IJobHubClient>(_jobHubClient);
        _context.Render<MudBlazor.MudPopoverProvider>();
    }

    [BeforeScenario("tracking_ui", Order = 1)]
    public void SetUpTrackingFailureEditorModule()
    {
        _editorModule = _context.JSInterop.SetupModule(
            "/_content/VideoAnonymizer.Web.Modules/js/videoEditorHost.js");
        _editorModule.Mode = JSRuntimeMode.Loose;
    }

    [AfterScenario("review_editor_persistence")]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        _http.Dispose();
    }

    [Given("the review editor is open for a persisted video with an empty frame")]
    public void GivenTheReviewEditorIsOpenForAPersistedVideoWithAnEmptyFrame()
    {
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _cut = RenderReviewTab(_videoId, _frameId);
    }

    [Given("the review editor is open for a persisted video with one face at x {int}")]
    public void GivenTheReviewEditorIsOpenForAPersistedVideoWithOneFaceAtX(int x)
    {
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _faceId = Guid.NewGuid();
        _cut = RenderReviewTab(_videoId, _frameId, CreateObject(_faceId, _frameId, trackId: 3, x: x));
    }

    [Given("the review editor is open with blur size {int} percent and time buffer {int} ms")]
    public void GivenTheReviewEditorIsOpenWithBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _cut = RenderReviewTab(_videoId, _frameId, blurSizePercent: blurSizePercent, timeBufferMs: timeBufferMs);
    }

    [Given("the review editor has saved a newly added face")]
    public async Task GivenTheReviewEditorHasSavedANewlyAddedFace()
    {
        GivenTheReviewEditorIsOpenForAPersistedVideoWithAnEmptyFrame();
        await WhenTheReviewerAddsAFace();
        ThenTheNewFaceIsPostedToPersistence();
    }

    [Given("the review editor has undone a newly added face")]
    public async Task GivenTheReviewEditorHasUndoneANewlyAddedFace()
    {
        await GivenTheReviewEditorHasSavedANewlyAddedFace();
        await WhenTheReviewerUndoesTheLastReviewAction();
        ThenTheNewFaceIsDeletedFromPersistence();
    }

    [Given("the review editor is reopened with a completed tracking action")]
    public void GivenTheReviewEditorIsReopenedWithACompletedTrackingAction()
    {
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _faceId = Guid.NewGuid();
        RestoredTrackedFaceId = Guid.NewGuid();

        var seed = CreateObject(_faceId, _frameId, trackId: 3);
        var trackedFace = CreateObject(RestoredTrackedFaceId, _frameId, trackId: 3);
        _http.ActionHistory =
        [
            new EditorActionDto
            {
                Id = Guid.NewGuid(),
                VideoId = _videoId,
                ActionType = "track-forward",
                SequenceNumber = 1,
                CreatedAt = DateTime.UtcNow,
                Data = JsonSerializer.Serialize(new
                {
                    SeedFrameId = _frameId.ToString(),
                    Seed = seed,
                    CreatedObjects = new[] { trackedFace },
                    TrackId = 3,
                    IsPartial = false
                })
            }
        ];

        _cut = RenderReviewTab(_videoId, _frameId, trackedFace);
    }

    [Given("the review editor has saved a face moved from x {int} to x {int}")]
    public async Task GivenTheReviewEditorHasSavedAFaceMovedFromXToX(int originalX, int movedX)
    {
        GivenTheReviewEditorIsOpenForAPersistedVideoWithOneFaceAtX(originalX);
        await WhenTheReviewerMovesTheFaceToX(movedX);
        ThenTheFaceIsSavedAtX(movedX);
    }

    [Given("the review editor has undone a face move from x {int} to x {int}")]
    public async Task GivenTheReviewEditorHasUndoneAFaceMoveFromXToX(int originalX, int movedX)
    {
        await GivenTheReviewEditorHasSavedAFaceMovedFromXToX(originalX, movedX);
        await WhenTheReviewerUndoesTheLastReviewAction();
        ThenTheFaceIsSavedAtX(originalX);
    }

    [Given("the review editor has saved blur size {int} percent from {int} percent with time buffer {int} ms")]
    public async Task GivenTheReviewEditorHasSavedBlurSizeFromPercentWithTimeBuffer(int changedBlurSizePercent, int originalBlurSizePercent, int timeBufferMs)
    {
        GivenTheReviewEditorIsOpenWithBlurSizeAndTimeBuffer(originalBlurSizePercent, timeBufferMs);
        await WhenTheReviewerChangesTheBlurSizeToPercent(changedBlurSizePercent);
        ThenTheSettingsAreSavedWithBlurSizeAndTimeBuffer(changedBlurSizePercent, timeBufferMs);
    }

    [Given("the review editor has undone blur size {int} percent back to {int} percent with time buffer {int} ms")]
    public async Task GivenTheReviewEditorHasUndoneBlurSizeBackToPercentWithTimeBuffer(int changedBlurSizePercent, int originalBlurSizePercent, int timeBufferMs)
    {
        await GivenTheReviewEditorHasSavedBlurSizeFromPercentWithTimeBuffer(changedBlurSizePercent, originalBlurSizePercent, timeBufferMs);
        await WhenTheReviewerUndoesTheLastReviewAction();
        ThenTheSettingsAreSavedWithBlurSizeAndTimeBuffer(originalBlurSizePercent, timeBufferMs);
    }

    [Given("the review editor has moved a face, undone the move, and added another face")]
    public async Task GivenTheReviewEditorHasMovedAFaceUndoneTheMoveAndAddedAnotherFace()
    {
        await GivenTheReviewEditorHasUndoneAFaceMoveFromXToX(originalX: 10, movedX: 90);
        await WhenTheReviewerAddsAFace();
    }

    [Given("tracking has streamed a new face into the review editor")]
    public async Task GivenTrackingHasStreamedANewFaceIntoTheReviewEditor()
    {
        GivenTheReviewEditorIsOpenForAPersistedVideoWithOneFaceAtX(10);

        await _cut.InvokeAsync(() => Editor.OnTrackForward(
            _videoId.ToString(),
            _frameId.ToString(),
            CreateObject(_faceId, _frameId, trackId: 3)));

        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Should().ContainSingle(r =>
                r.Method == HttpMethod.Post && r.Path.Contains($"/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}"))
                .Subject;
            _trackingJobId = GetJsonGuid(request.Body, "jobId");
        });

        _streamedFaceId = Guid.NewGuid();
        await _jobHubClient.RaiseTrackForwardProgressAsync(new TrackForwardProgressMessage
        {
            JobId = _trackingJobId,
            VideoId = _videoId,
            Status = SharedConstants.SignalR.Status.Completed,
            TrackId = 3,
            CreatedObjects = [CreateObject(_streamedFaceId, _frameId, trackId: 3)]
        });

        _cut.WaitForAssertion(() =>
            _editorModule.Invocations["applyDetectedObjectChanges"].Should().ContainSingle());
    }

    [Given("tracking has completed after streaming faces in two batches into the review editor")]
    public async Task GivenTrackingHasCompletedAfterStreamingFacesInTwoBatchesIntoTheReviewEditor()
    {
        GivenTheReviewEditorIsOpenForAPersistedVideoWithOneFaceAtX(10);

        await _cut.InvokeAsync(() => Editor.OnTrackForward(
            _videoId.ToString(),
            _frameId.ToString(),
            CreateObject(_faceId, _frameId, trackId: 3)));

        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Should().ContainSingle(r =>
                r.Method == HttpMethod.Post && r.Path.Contains($"/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}"))
                .Subject;
            _trackingJobId = GetJsonGuid(request.Body, "jobId");
        });

        StreamedTrackedFaceIds = [Guid.NewGuid(), Guid.NewGuid()];
        foreach (var faceId in StreamedTrackedFaceIds)
        {
            await _jobHubClient.RaiseTrackForwardProgressAsync(new TrackForwardProgressMessage
            {
                JobId = _trackingJobId,
                VideoId = _videoId,
                Status = SharedConstants.SignalR.Status.Completed,
                TrackId = 3,
                CreatedObjects = [CreateObject(faceId, _frameId, trackId: 3)]
            });
        }

        await _jobHubClient.RaiseTrackForwardCompletedAsync(new TrackForwardCompletedMessage
        {
            JobId = _trackingJobId,
            VideoId = _videoId,
            Status = SharedConstants.SignalR.Status.Completed,
            Result = new TrackForwardResponseDto
            {
                TrackId = 3,
                CreatedDetections = StreamedTrackedFaceIds.Count,
                StoppedReason = "end_of_video"
            },
            CreatedObjects = []
        });
    }

    [When("the reviewer adds a face")]
    public async Task WhenTheReviewerAddsAFace()
    {
        _addedFaceId = Guid.NewGuid();
        var editor = Editor;
        var addRoute = AddRoute;
        var expectedPosts = Requests.Count(r => r.Method == HttpMethod.Post && r.Path == addRoute) + 1;

        await _cut.InvokeAsync(() => editor.OnDetectedObjectAdded(
            _videoId.ToString(),
            _frameId.ToString(),
            CreateObject(_addedFaceId, _frameId, trackId: 5)));

        _cut.WaitForAssertion(() =>
            Requests.Count(r => r.Method == HttpMethod.Post && r.Path == addRoute).Should().Be(expectedPosts));
    }

    [Then("the new face is posted to persistence")]
    public void ThenTheNewFaceIsPostedToPersistence()
    {
        _cut.WaitForAssertion(() => Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post && r.Path == AddRoute));
    }

    [Then("the new face is posted to persistence again")]
    public void ThenTheNewFaceIsPostedToPersistenceAgain()
    {
        _cut.WaitForAssertion(() =>
            Requests.Count(r => r.Method == HttpMethod.Post && r.Path == AddRoute).Should().Be(2));
    }

    [When("the reviewer undoes the last review action")]
    public async Task WhenTheReviewerUndoesTheLastReviewAction()
    {
        await _cut.InvokeAsync(Editor.OnUndo);
    }

    [Then("the new face is deleted from persistence")]
    public void ThenTheNewFaceIsDeletedFromPersistence()
    {
        _cut.WaitForAssertion(() => Requests.Should().ContainSingle(r => r.Method == HttpMethod.Delete && r.Path == ObjectRoute(_addedFaceId)));
    }

    [Then("the restored tracked face is removed from persistence")]
    public void ThenTheRestoredTrackedFaceIsRemovedFromPersistence()
    {
        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Should().ContainSingle(r =>
                r.Method == HttpMethod.Post
                && r.Path == $"/{SharedConstants.Paths.Video}/{_videoId}/{SharedConstants.Paths.DetectedObjects}/delete").Subject;
            using var document = JsonDocument.Parse(request.Body);
            var objectIds = document.RootElement.GetProperty("objectIds")
                .EnumerateArray()
                .Select(element => element.GetGuid());
            objectIds.Should().Equal(RestoredTrackedFaceId);
        });
    }

    [Then("all streamed tracked faces are removed from persistence")]
    public void ThenAllStreamedTrackedFacesAreRemovedFromPersistence()
    {
        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Should().ContainSingle(r =>
                r.Method == HttpMethod.Post
                && r.Path == $"/{SharedConstants.Paths.Video}/{_videoId}/{SharedConstants.Paths.DetectedObjects}/delete").Subject;
            using var document = JsonDocument.Parse(request.Body);
            var objectIds = document.RootElement.GetProperty("objectIds")
                .EnumerateArray()
                .Select(element => element.GetGuid());
            objectIds.Should().BeEquivalentTo(StreamedTrackedFaceIds);
        });
    }

    [When("the reviewer redoes the review action")]
    public async Task WhenTheReviewerRedoesTheReviewAction()
    {
        await _cut.InvokeAsync(Editor.OnRedo);
    }

    [When("the reviewer moves the face to x {int}")]
    public async Task WhenTheReviewerMovesTheFaceToX(int x)
    {
        var before = CreateObject(_faceId, _frameId, trackId: 3, x: 10);
        var moved = CreateObject(_faceId, _frameId, trackId: 3, x: x);

        await _cut.InvokeAsync(() => Editor.OnDetectedObjectUpdated(
            _videoId.ToString(),
            _frameId.ToString(),
            moved,
            "move",
            [before]));
    }

    [Then("the face is saved at x {int}")]
    public void ThenTheFaceIsSavedAtX(int x)
    {
        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Where(r => r.Method == HttpMethod.Put && r.Path == ObjectRoute(_faceId)).ElementAt(_faceSaveAssertionIndex);
            GetJsonInt(request.Body, "x").Should().Be(x);
        });
        _faceSaveAssertionIndex++;
    }

    [When("the reviewer changes the blur size to {int} percent")]
    public async Task WhenTheReviewerChangesTheBlurSizeToPercent(int blurSizePercent)
    {
        var settings = _cut.FindComponent<ReviewExportSettings>().Instance;
        await _cut.InvokeAsync(() => settings.BlurSizePercentChanged.InvokeAsync(blurSizePercent));
    }

    [Then("the settings are saved with blur size {int} percent and time buffer {int} ms")]
    public void ThenTheSettingsAreSavedWithBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        _cut.WaitForAssertion(() =>
        {
            var request = Requests.Where(r => r.Method == HttpMethod.Put && r.Path == SettingsRoute).ElementAt(_settingsSaveAssertionIndex);
            GetJsonInt(request.Body, "blurSizePercent").Should().Be(blurSizePercent);
            GetJsonInt(request.Body, "timeBufferMs").Should().Be(timeBufferMs);
        });
        _settingsSaveAssertionIndex++;
    }

    [When("the reviewer tries to redo")]
    public async Task WhenTheReviewerTriesToRedo()
    {
        _requestCountBeforeRedo = Requests.Count;
        await _cut.InvokeAsync(Editor.OnRedo);
        await Task.Delay(100);
    }

    [When("tracking fails after retaining the streamed face")]
    public async Task WhenTrackingFailsAfterRetainingTheStreamedFace()
    {
        await _jobHubClient.RaiseTrackForwardCompletedAsync(new TrackForwardCompletedMessage
        {
            JobId = _trackingJobId,
            VideoId = _videoId,
            Status = "failed",
            Error = "Python tracking failed.",
            Result = new TrackForwardResponseDto
            {
                TrackId = 3,
                CreatedDetections = 1,
                StoppedReason = "technical_failure"
            },
            CreatedObjects = [CreateObject(_streamedFaceId, _frameId, trackId: 3)]
        });
    }

    [Then("no extra persistence request is sent")]
    public void ThenNoExtraPersistenceRequestIsSent()
    {
        Requests.Count.Should().Be(_requestCountBeforeRedo);
    }

    [Then("the streamed face remains in the review editor")]
    public void ThenTheStreamedFaceRemainsInTheReviewEditor()
    {
        _cut.WaitForAssertion(() =>
        {
            var invocations = _editorModule.Invocations["applyDetectedObjectChanges"];
            invocations.Should().ContainSingle();
            var changes = invocations[0].Arguments[1]
                .Should().BeOfType<DetectedObjectChangeSet>().Subject;
            changes.ObjectsToRemove.Should().BeEmpty();
            changes.ObjectsToAdd.Should().ContainSingle(obj => obj.Id == _streamedFaceId);
            changes.ObjectsToUpdate.Should().BeEmpty();
        });
    }

    [Then("a warning says tracking can continue from the last occurrence")]
    public void ThenAWarningSaysTrackingCanContinueFromTheLastOccurrence()
    {
        var snackbar = _context.Services.GetRequiredService<ISnackbar>();
        snackbar.ShownSnackbars.Should().ContainSingle(item =>
            item.Severity == Severity.Warning
            && item.Message != null
            && item.Message.Contains("were kept", StringComparison.Ordinal)
            && item.Message.Contains("last occurrence", StringComparison.Ordinal));
    }

    [Then("the partial tracking action is persisted")]
    public void ThenThePartialTrackingActionIsPersisted()
    {
        var request = Requests.Should().ContainSingle(r =>
            r.Method == HttpMethod.Post
            && r.Path.EndsWith($"/{SharedConstants.Paths.Actions}", StringComparison.Ordinal)).Subject;
        using var requestDocument = JsonDocument.Parse(request.Body);
        var data = requestDocument.RootElement.GetProperty("data").GetString();
        using var dataDocument = JsonDocument.Parse(data!);
        dataDocument.RootElement.GetProperty("IsPartial").GetBoolean().Should().BeTrue();
    }

    private IReadOnlyList<RequestLog> Requests => _http.Requests;
    private VideoEditor Editor => _cut.FindComponent<VideoEditor>().Instance;
    private string AddRoute => $"/{SharedConstants.Paths.Video}/{_videoId}/{SharedConstants.Paths.AnalyzedFrame}/{_frameId}/{SharedConstants.Paths.DetectedObject}";
    private string SettingsRoute => $"/{SharedConstants.Paths.Video}/{_videoId}/{SharedConstants.Paths.VideoSettings}";

    private string ObjectRoute(Guid objectId) => $"{AddRoute}/{objectId}";

    private IRenderedComponent<ReviewExportTab> RenderReviewTab(
        Guid videoId,
        Guid frameId,
        DetectedObjectDto? detectedObject = null,
        int blurSizePercent = 120,
        int timeBufferMs = 300)
    {
        var frames = new List<AnalyzedFrameDto>
        {
            new()
            {
                Id = frameId,
                VideoId = videoId,
                TimeSeconds = 1.0,
                DetectedObjects = detectedObject is null ? [] : [detectedObject]
            }
        };

        return _context.Render<ReviewExportTab>(parameters => parameters
            .Add(p => p.VideoId, videoId)
            .Add(p => p.VideoSourceUrl, $"https://localhost:5001/{SharedConstants.Paths.Video}/{videoId}")
            .Add(p => p.Frames, frames)
            .Add(p => p.ShowEditor, true)
            .Add(p => p.BlurSizePercent, blurSizePercent)
            .Add(p => p.TimeBufferMs, timeBufferMs)
            .Add(p => p.BlurSizePercentChanged, EventCallback.Factory.Create<int>(this, _ => Task.CompletedTask))
            .Add(p => p.TimeBufferMsChanged, EventCallback.Factory.Create<int>(this, _ => Task.CompletedTask))
            .Add(p => p.StartAnonymization, EventCallback.Factory.Create(this, () => Task.CompletedTask)));
    }

    private static DetectedObjectDto CreateObject(Guid objectId, Guid frameId, int trackId, int x = 10) =>
        new()
        {
            Id = objectId,
            AnalyzedFrameId = frameId,
            Confidence = 0.95,
            ClassName = "face",
            Selected = true,
            TrackId = trackId,
            X = x,
            Y = 20,
            Width = 30,
            Height = 40
        };

    private static int GetJsonInt(string body, string propertyName)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.TryGetProperty(propertyName, out var value))
        {
            return value.GetInt32();
        }

        var pascalName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        return root.GetProperty(pascalName).GetInt32();
    }

    private static Guid GetJsonGuid(string body, string propertyName)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.TryGetProperty(propertyName, out var value))
        {
            return value.GetGuid();
        }

        var pascalName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        return root.GetProperty(pascalName).GetGuid();
    }

    private sealed class RecordingHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://localhost:5001")
            };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly ConcurrentQueue<RequestLog> _requests = [];

        public IReadOnlyList<RequestLog> Requests => _requests.ToArray();
        public IReadOnlyList<EditorActionDto> ActionHistory { get; set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            _requests.Enqueue(new RequestLog(request.Method, request.RequestUri!.PathAndQuery, body));

            if (request.Method == HttpMethod.Get
                && request.RequestUri.AbsolutePath.EndsWith(
                    $"/{SharedConstants.Paths.Actions}",
                    StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { Payload = ActionHistory }),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.RequestUri.AbsolutePath.EndsWith(
                    $"/{SharedConstants.Paths.Tracks}/{SharedConstants.Paths.TrackForward}",
                    StringComparison.Ordinal))
            {
                var jobId = GetJsonGuid(body, "jobId");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { Payload = new { JobId = jobId } }),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            if (request.Method == HttpMethod.Post
                && request.RequestUri.AbsolutePath.EndsWith(
                    $"/{SharedConstants.Paths.Actions}",
                    StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { Payload = new { Id = Guid.NewGuid() } }),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record RequestLog(HttpMethod Method, string Path, string Body);
}
