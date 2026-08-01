using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.VideoProcessor.Analysis.Tracking;
using DetectionClient = VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class ForwardTrackingStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    private SqliteConnection Connection
    {
        get => _scenarioContext.Get<SqliteConnection>(nameof(Connection));
        set => _scenarioContext.Set(value, nameof(Connection));
    }

    private ServiceProvider Services
    {
        get => _scenarioContext.Get<ServiceProvider>(nameof(Services));
        set => _scenarioContext.Set(value, nameof(Services));
    }

    private IDbContextFactory<VideoAnonymizerDbContext> DbFactory
    {
        get => _scenarioContext.Get<IDbContextFactory<VideoAnonymizerDbContext>>(nameof(DbFactory));
        set => _scenarioContext.Set(value, nameof(DbFactory));
    }

    private string ContentRoot
    {
        get => _scenarioContext.Get<string>(nameof(ContentRoot));
        set => _scenarioContext.Set(value, nameof(ContentRoot));
    }

    private FakeForwardTrackingClient FakeClient
    {
        get => _scenarioContext.Get<FakeForwardTrackingClient>(nameof(FakeClient));
        set => _scenarioContext.Set(value, nameof(FakeClient));
    }

    private Guid VideoId
    {
        get => _scenarioContext.Get<Guid>(nameof(VideoId));
        set => _scenarioContext.Set(value, nameof(VideoId));
    }

    private Guid SeedObjectId
    {
        get => _scenarioContext.Get<Guid>(nameof(SeedObjectId));
        set => _scenarioContext.Set(value, nameof(SeedObjectId));
    }

    private Guid FirstFutureFrameId
    {
        get => _scenarioContext.Get<Guid>(nameof(FirstFutureFrameId));
        set => _scenarioContext.Set(value, nameof(FirstFutureFrameId));
    }

    private Guid SecondFutureFrameId
    {
        get => _scenarioContext.Get<Guid>(nameof(SecondFutureFrameId));
        set => _scenarioContext.Set(value, nameof(SecondFutureFrameId));
    }

    private TrackForwardResult? LastResult
    {
        get => _scenarioContext.Get<TrackForwardResult>(nameof(LastResult));
        set => _scenarioContext.Set(value, nameof(LastResult));
    }

    private Exception? LastException
    {
        get => _scenarioContext.TryGetValue<Exception>(nameof(LastException), out var ex) ? ex : null;
        set
        {
            if (value is not null)
                _scenarioContext.Set(value, nameof(LastException));
            else
                _scenarioContext.Remove(nameof(LastException));
        }
    }

    private string TrackingStreamBody
    {
        get => _scenarioContext.Get<string>(nameof(TrackingStreamBody));
        set => _scenarioContext.Set(value, nameof(TrackingStreamBody));
    }

    private List<TrackForwardStreamEvent> TrackingStreamEvents
    {
        get => _scenarioContext.Get<List<TrackForwardStreamEvent>>(nameof(TrackingStreamEvents));
        set => _scenarioContext.Set(value, nameof(TrackingStreamEvents));
    }

    public ForwardTrackingStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario("forward_tracking")]
    public async Task SetUp()
    {
        ContentRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "forward-tracking-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ContentRoot);

        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<VideoAnonymizerDbContext>(options => options.UseSqlite(Connection));
        Services = services.BuildServiceProvider();
        DbFactory = Services.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();

        await using var db = await DbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        FakeClient = new FakeForwardTrackingClient();
    }

    [AfterScenario("forward_tracking")]
    public async Task TearDown()
    {
        if (_scenarioContext.TryGetValue<ServiceProvider>(nameof(Services), out var services))
        {
            await services.DisposeAsync();
        }

        if (_scenarioContext.TryGetValue<SqliteConnection>(nameof(Connection), out var connection))
        {
            await connection.DisposeAsync();
        }

        SqliteConnection.ClearAllPools();

        if (_scenarioContext.TryGetValue<string>(nameof(ContentRoot), out var contentRoot)
            && Directory.Exists(contentRoot))
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Given("a reviewed video has trackable analyzed frames")]
    public async Task GivenAReviewedVideoHasTrackableAnalyzedFrames()
    {
        VideoId = Guid.NewGuid();
        var seedFrameId = Guid.NewGuid();
        FirstFutureFrameId = Guid.NewGuid();
        SecondFutureFrameId = Guid.NewGuid();
        SeedObjectId = Guid.NewGuid();

        await using var db = await DbFactory.CreateDbContextAsync();
        db.Videos.Add(new Video
        {
            Id = VideoId,
            SourcePath = WriteVideoFile($"{VideoId}.mp4"),
            OriginalFileName = "track-forward.mp4",
            BlurSizePercent = 120,
            TimeBufferMs = 300,
            AnalyzedFrames =
            [
                CreateFrame(seedFrameId, VideoId, frameIndex: 0, timeSeconds: 0.0,
                [
                    CreateObject(SeedObjectId, seedFrameId, trackId: 7, x: 10, y: 20)
                ]),
                CreateFrame(FirstFutureFrameId, VideoId, frameIndex: 10, timeSeconds: 1.0, []),
                CreateFrame(SecondFutureFrameId, VideoId, frameIndex: 20, timeSeconds: 2.0, [])
            ]
        });
        await db.SaveChangesAsync();
    }

    [Given("the Python forward tracker returns boxes on analyzed frames and a decoded-only frame")]
    public void GivenThePythonForwardTrackerReturnsBoxesOnAnalyzedFramesAndADecodedOnlyFrame()
    {
        FakeClient.Response = CreatePythonResponse(
        [
            CreatePythonDetection(frameIndex: 10, x: 12),
            CreatePythonDetection(frameIndex: 11, x: 13),
            CreatePythonDetection(frameIndex: 20, x: 16)
        ]);
    }

    [Given("the Python forward tracker returns boxes on analyzed frames")]
    public void GivenThePythonForwardTrackerReturnsBoxesOnAnalyzedFrames()
    {
        FakeClient.Response = CreatePythonResponse(
        [
            CreatePythonDetection(frameIndex: 10, x: 12),
            CreatePythonDetection(frameIndex: 20, x: 80)
        ]);
    }

    [Given("a later analyzed frame already has an overlapping face from another track")]
    public async Task GivenALaterAnalyzedFrameAlreadyHasAnOverlappingFaceFromAnotherTrack()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var frame = await db.AnalyzedFrames.SingleAsync(frame => frame.Id == FirstFutureFrameId);
        db.DetectedObjects.Add(CreateObject(Guid.NewGuid(), frame.Id, trackId: 99, x: 12, y: 20));
        await db.SaveChangesAsync();
    }

    [Given("the Python forward tracker reports reacquisition and lost timeout")]
    public void GivenThePythonForwardTrackerReportsReacquisitionAndLostTimeout()
    {
        FakeClient.Response = CreatePythonResponse(
            [CreatePythonDetection(frameIndex: 10, x: 12, reacquired: true)],
            reacquiredCount: 1,
            stoppedReason: "lost_timeout");
        FakeClient.Response.Gaps.Add(new TrackForwardPythonGap { StartTimeMs = 1200, EndTimeMs = 6200 });
    }

    [Given("the Python tracking stream reports an error")]
    public void GivenThePythonTrackingStreamReportsAnError()
    {
        TrackingStreamBody = "data: {\"type\":\"error\",\"message\":\"Could not open video.\"}\n\n";
    }

    [Given("the Python tracking stream ends without completion")]
    public void GivenThePythonTrackingStreamEndsWithoutCompletion()
    {
        TrackingStreamBody = "data: {\"type\":\"progress\",\"frameIndex\":1,\"timeMs\":40}\n\n";
    }

    [Given("the Python tracking stream completes normally")]
    public void GivenThePythonTrackingStreamCompletesNormally()
    {
        TrackingStreamBody =
            "data: {\"type\":\"progress\",\"frameIndex\":1,\"timeMs\":40}\n\n" +
            "data: {\"type\":\"complete\",\"stoppedReason\":\"lost_timeout\",\"reacquiredCount\":2}\n\n";
    }

    [Given("the Python tracking stream completes twice")]
    public void GivenThePythonTrackingStreamCompletesTwice()
    {
        const string CompleteEvent = "data: {\"type\":\"complete\",\"stoppedReason\":\"end_of_video\",\"reacquiredCount\":0}\n\n";
        TrackingStreamBody = CompleteEvent + CompleteEvent;
    }

    [Given("the seed face does not have a track id")]
    public async Task GivenTheSeedFaceDoesNotHaveATrackId()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var seed = await db.DetectedObjects.SingleAsync(obj => obj.Id == SeedObjectId);
        seed.TrackId = null;
        await db.SaveChangesAsync();
    }

    [Given("the Python forward tracker fails after returning one box")]
    public void GivenThePythonForwardTrackerFailsAfterReturningOneBox()
    {
        FakeClient.Response = CreatePythonResponse(
        [
            CreatePythonDetection(frameIndex: 10, x: 12),
            CreatePythonDetection(frameIndex: 20, x: 16)
        ]);
        FakeClient.FailAfterDetectionCount = 1;
    }

    [When("the reviewer tracks the seed face forward")]
    public async Task WhenTheReviewerTracksTheSeedFaceForward()
    {
        await TrackForwardDirectlyAsync(new TrackForwardJob
        {
            SeedDetectionId = SeedObjectId
        });
    }

    [When("track forward is requested with replace conflicts")]
    public async Task WhenTrackForwardIsRequestedWithReplaceConflicts()
    {
        await TrackForwardDirectlyAsync(new TrackForwardJob
        {
            SeedDetectionId = SeedObjectId,
            ConflictMode = "replace"
        });
    }

    [When("the tracking stream is read")]
    public async Task WhenTheTrackingStreamIsRead()
    {
        TrackingStreamEvents = [];
        LastException = null;

        using var httpClient = new HttpClient(new SseResponseHandler(TrackingStreamBody));
        var client = new DetectionClient("http://localhost/", httpClient);

        try
        {
            await foreach (var streamEvent in client.TrackForwardStreamingAsync(
                               new TrackForwardPythonRequest(), CancellationToken.None))
            {
                TrackingStreamEvents.Add(streamEvent);
            }
        }
        catch (Exception ex)
        {
            LastException = ex;
        }
    }

    [Then("generated faces use the seed track id")]
    public async Task ThenGeneratedFacesUseTheSeedTrackId()
    {
        LastResult.Should().NotBeNull();
        LastResult!.CreatedDetections.Should().Be(2);

        await using var db = await DbFactory.CreateDbContextAsync();
        var generated = await db.DetectedObjects
            .Where(obj => obj.AnalyzedFrameId == FirstFutureFrameId || obj.AnalyzedFrameId == SecondFutureFrameId)
            .ToListAsync();

        generated.Should().HaveCount(2);
        generated.Should().OnlyContain(obj => obj.TrackId == 7);
    }

    [Then("the Python tracker is asked to persist only analyzed frames")]
    public void ThenThePythonTrackerIsAskedToPersistOnlyAnalyzedFrames()
    {
        FakeClient.LastRequest.Should().NotBeNull();
        FakeClient.LastRequest!.PersistFrameIndexes.Should().Equal(10, 20);
        FakeClient.LastRequest.PersistEveryMs.Should().Be(1000);
    }

    [Then("only the non-conflicting generated face is saved")]
    public async Task ThenOnlyTheNonConflictingGeneratedFaceIsSaved()
    {
        LastResult.Should().NotBeNull();
        LastResult!.CreatedDetections.Should().Be(1);
        LastResult.SkippedConflicts.Should().Be(1);

        await using var db = await DbFactory.CreateDbContextAsync();
        var firstFutureTrackIds = await db.DetectedObjects
            .Where(obj => obj.AnalyzedFrameId == FirstFutureFrameId)
            .Select(obj => obj.TrackId)
            .ToListAsync();
        firstFutureTrackIds.Should().BeEquivalentTo([99]);

        var secondFutureTrackIds = await db.DetectedObjects
            .Where(obj => obj.AnalyzedFrameId == SecondFutureFrameId)
            .Select(obj => obj.TrackId)
            .ToListAsync();
        secondFutureTrackIds.Should().BeEquivalentTo([7]);
    }

    [Then("the track forward request is rejected")]
    public void ThenTheTrackForwardRequestIsRejected()
    {
        LastException.Should().BeOfType<ArgumentException>();
        FakeClient.LastRequest.Should().BeNull();
    }

    [Then("the response includes the reacquisition summary")]
    public void ThenTheResponseIncludesTheReacquisitionSummary()
    {
        LastException.Should().BeNull();
        LastResult.Should().NotBeNull();
        LastResult!.ReacquiredCount.Should().Be(1);
        LastResult.StoppedReason.Should().Be("lost_timeout");
        LastResult.Gaps.Should().ContainSingle(gap => gap.StartTimeMs == 1200 && gap.EndTimeMs == 6200);

        LastResult.CreatedDetections.Should().Be(1);
        LastResult.TrackId.Should().Be(7);
    }

    [Then("the tracking stream fails with the Python error")]
    public void ThenTheTrackingStreamFailsWithThePythonError()
    {
        LastException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("Could not open video.");
        TrackingStreamEvents.Should().BeEmpty();
    }

    [Then("the tracking stream fails because completion is missing")]
    public void ThenTheTrackingStreamFailsBecauseCompletionIsMissing()
    {
        LastException.Should().BeOfType<InvalidDataException>()
            .Which.Message.Should().Contain("ended before its terminal complete event");
        TrackingStreamEvents.Should().ContainSingle(streamEvent => streamEvent.Type == "progress");
    }

    [Then("the tracking stream returns its completion metadata")]
    public void ThenTheTrackingStreamReturnsItsCompletionMetadata()
    {
        LastException.Should().BeNull();
        TrackingStreamEvents.Should().HaveCount(2);
        TrackingStreamEvents[^1].Type.Should().Be("complete");
        TrackingStreamEvents[^1].StoppedReason.Should().Be("lost_timeout");
        TrackingStreamEvents[^1].ReacquiredCount.Should().Be(2);
    }

    [Then("the tracking stream fails because completion is duplicated")]
    public void ThenTheTrackingStreamFailsBecauseCompletionIsDuplicated()
    {
        LastException.Should().BeOfType<InvalidDataException>()
            .Which.Message.Should().Contain("after its terminal complete event");
        TrackingStreamEvents.Should().ContainSingle(streamEvent => streamEvent.Type == "complete");
    }

    [Then("the streamed face remains in persistence")]
    public async Task ThenTheStreamedFaceRemainsInPersistence()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var generated = await db.DetectedObjects.SingleAsync(obj =>
            obj.AnalyzedFrameId == FirstFutureFrameId || obj.AnalyzedFrameId == SecondFutureFrameId);
        generated.TrackId.Should().Be(1);
    }

    [Then("the seed face keeps its assigned track id")]
    public async Task ThenTheSeedFaceKeepsItsAssignedTrackId()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var trackId = await db.DetectedObjects
            .Where(obj => obj.Id == SeedObjectId)
            .Select(obj => obj.TrackId)
            .SingleAsync();
        trackId.Should().Be(1);
    }

    [Then("the tracking failure reports the retained face")]
    public async Task ThenTheTrackingFailureReportsTheRetainedFace()
    {
        var failure = LastException.Should().BeOfType<TrackForwardFailedException>().Subject;
        failure.TrackId.Should().Be(1);
        failure.CreatedObjectIds.Should().ContainSingle();
        failure.InnerException.Should().BeOfType<InvalidOperationException>();

        await using var db = await DbFactory.CreateDbContextAsync();
        var retainedId = await db.DetectedObjects
            .Where(obj => obj.AnalyzedFrameId == FirstFutureFrameId || obj.AnalyzedFrameId == SecondFutureFrameId)
            .Select(obj => obj.Id)
            .SingleAsync();
        failure.CreatedObjectIds.Should().Equal(retainedId);
    }

    private async Task TrackForwardDirectlyAsync(TrackForwardJob job)
    {
        try
        {
            job.VideoId = VideoId;
            var service = new ForwardTrackingService(DbFactory, FakeClient);
            LastResult = await service.TrackForwardIncrementalAsync(
                VideoId, job,
                _ => Task.CompletedTask,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            LastException = ex;
        }
    }

    private static TrackForwardPythonResponse CreatePythonResponse(
        IReadOnlyList<TrackForwardPythonDetectionResult> detections,
        int reacquiredCount = 0,
        string stoppedReason = "end_of_video") =>
        new()
        {
            TrackId = 7,
            Detections = detections.ToList(),
            ReacquiredCount = reacquiredCount,
            StoppedReason = stoppedReason
        };

    private static TrackForwardPythonDetectionResult CreatePythonDetection(
        int frameIndex,
        int x,
        bool reacquired = false) =>
        new()
        {
            FrameIndex = frameIndex,
            TimeMs = frameIndex * 100,
            ClassName = "face",
            Confidence = reacquired ? 0.90 : 0.80,
            X = x,
            Y = 20,
            Width = 30,
            Height = 40,
            BlurShape = "ellipse",
            TrackId = 7,
            Reacquired = reacquired
        };

    private static AnalyzedFrame CreateFrame(
        Guid frameId,
        Guid videoId,
        int frameIndex,
        double timeSeconds,
        IReadOnlyList<DetectedObject> objects) =>
        new()
        {
            Id = frameId,
            VideoId = videoId,
            FrameIndex = frameIndex,
            TimeSeconds = timeSeconds,
            DetectedObjects = objects.ToList()
        };

    private static DetectedObject CreateObject(
        Guid objectId,
        Guid frameId,
        int trackId,
        int x,
        int y) =>
        new()
        {
            Id = objectId,
            AnalyzedFrameId = frameId,
            Confidence = 0.95,
            ClassName = "face",
            BlurShape = "ellipse",
            Selected = true,
            TrackId = trackId,
            X = x,
            Y = y,
            Width = 30,
            Height = 40
        };

    private string WriteVideoFile(string fileName)
    {
        var path = Path.Combine(ContentRoot, fileName);
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5]);
        return path;
    }

    private sealed class FakeForwardTrackingClient : DetectionClient
    {
        public FakeForwardTrackingClient()
            : base("http://localhost/", new HttpClient())
        {
        }

        public TrackForwardPythonRequest? LastRequest { get; private set; }

        public TrackForwardPythonResponse Response { get; set; } = CreatePythonResponse([]);

        public int? FailAfterDetectionCount { get; set; }

        public override Task<TrackForwardPythonResponse> TrackForwardAsync(
            TrackForwardPythonRequest body,
            CancellationToken cancellationToken)
        {
            LastRequest = body;
            return Task.FromResult(Response);
        }

        public override async IAsyncEnumerable<TrackForwardStreamEvent> TrackForwardStreamingAsync(
            TrackForwardPythonRequest body,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            LastRequest = body;
            var yieldedDetections = 0;
            foreach (var detection in Response.Detections)
            {
                yield return new TrackForwardStreamEvent
                {
                    Type = "detection",
                    Detection = new TrackForwardPythonDetectionResult
                    {
                        FrameIndex = detection.FrameIndex,
                        TimeMs = detection.TimeMs,
                        ClassName = detection.ClassName,
                        Confidence = detection.Confidence,
                        X = detection.X,
                        Y = detection.Y,
                        Width = detection.Width,
                        Height = detection.Height,
                        BlurShape = detection.BlurShape,
                        TrackId = detection.TrackId,
                        Reacquired = detection.Reacquired
                    }
                };

                yieldedDetections++;
                if (FailAfterDetectionCount == yieldedDetections)
                {
                    throw new InvalidOperationException("Python tracking stream failed after returning a detection.");
                }
            }
            foreach (var gap in Response.Gaps)
            {
                yield return new TrackForwardStreamEvent
                {
                    Type = "gap",
                    Gap = new TrackForwardPythonGap
                    {
                        StartTimeMs = gap.StartTimeMs,
                        EndTimeMs = gap.EndTimeMs
                    }
                };
            }
            yield return new TrackForwardStreamEvent
            {
                Type = "complete",
                StoppedReason = Response.StoppedReason,
                ReacquiredCount = Response.ReacquiredCount
            };
        }
    }

    private sealed class SseResponseHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return Task.FromResult(response);
        }
    }
}
