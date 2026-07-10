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
        LastResult.Should().NotBeNull();
        LastResult!.ReacquiredCount.Should().Be(1);
        LastResult.StoppedReason.Should().Be("lost_timeout");
        LastResult.Gaps.Should().ContainSingle(gap => gap.StartTimeMs == 1200 && gap.EndTimeMs == 6200);

        LastResult.CreatedDetections.Should().Be(1);
        LastResult.TrackId.Should().Be(7);
    }

    private async Task TrackForwardDirectlyAsync(TrackForwardJob job)
    {
        try
        {
            job.VideoId = VideoId;
            var result = await new ForwardTrackingService(DbFactory, FakeClient)
                .TrackForwardAsync(VideoId, job, CancellationToken.None);

            LastResult = result;
        }
        catch (ArgumentException ex)
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

        public override Task<TrackForwardPythonResponse> TrackForwardAsync(
            TrackForwardPythonRequest body,
            CancellationToken cancellationToken)
        {
            LastRequest = body;
            return Task.FromResult(Response);
        }
    }
}
