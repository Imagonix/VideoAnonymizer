using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NUnit.Framework;
using Reqnroll;
using VideoAnonymizer.ApiService.Controllers;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class LocalVideoPersistenceStepDefinitions
{
    // TODO use ScenarioContext for lokal variables
    private SqliteConnection _connection = default!;
    private ServiceProvider _services = default!;
    private ServiceProvider? _restartedServices;
    private IDbContextFactory<VideoAnonymizerDbContext> _dbFactory = default!;
    private string _contentRoot = string.Empty;

    private RecordingMessagePublisher _publisher = default!;
    private Guid _videoId;
    private Guid _otherVideoId;
    private Guid _frameId;
    private Guid _secondFrameId;
    private Guid _foreignFrameId;
    private Guid _existingObjectId;
    private Guid _addedObjectId;
    private Guid _secondObjectId;
    private Guid _foreignObjectId;
    private string _uploadedFileName = string.Empty;
    private int _detectionIntervalMs;
    private string _originalPath = string.Empty;
    private string _anonymizedPath = string.Empty;
    private IActionResult _lastResult = default!;
    private IActionResult _lastOriginalFileResult = default!;
    private IActionResult _lastAnonymizedFileResult = default!;
    private List<VideoDto> _listedVideos = [];
    private List<AnalyzedFrameDto> _listedFrames = [];

    [BeforeScenario("api_persistence")]
    public async Task SetUp()
    {
        _contentRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "persistence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);

        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<VideoAnonymizerDbContext>(options => options.UseSqlite(_connection));
        _services = services.BuildServiceProvider();
        _dbFactory = _services.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    [AfterScenario("api_persistence")]
    public async Task TearDown()
    {
        if (_restartedServices is not null)
        {
            await _restartedServices.DisposeAsync();
        }

        await _services.DisposeAsync();
        await _connection.DisposeAsync();
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Given("a reviewer uploads {string} for object detection every {int} ms")]
    public async Task GivenAReviewerUploadsForObjectDetectionEveryMs(string fileName, int detectionIntervalMs)
    {
        _publisher = new RecordingMessagePublisher();
        _uploadedFileName = fileName;
        _detectionIntervalMs = detectionIntervalMs;
        var upload = CreateVideoUpload(fileName);

        _lastResult = await CreateVideosController(_publisher)
            .Analyze(upload, CancellationToken.None, detectionIntervalMs);

        _videoId = GetOkPayload<Guid>(_lastResult);
    }

    [When("the reviewer opens the saved videos list")]
    public async Task WhenTheReviewerOpensTheSavedVideosList()
    {
        _listedVideos = GetOkPayload<List<VideoDto>>(await CreateVideosController().GetVideos());
    }

    [Then("the uploaded video is listed with the default anonymization settings")]
    public void ThenTheUploadedVideoIsListedWithDefaultAnonymizationSettings()
    {
        _listedVideos.Should().ContainSingle(video =>
            video.Id == _videoId
            && video.OriginalFileName == _uploadedFileName
            && video.BlurSizePercent == 120
            && video.TimeBufferMs == 300);
    }

    [Then("an analysis job is published for the stored video")]
    public void ThenAnAnalysisJobIsPublishedForTheStoredVideo()
    {
        var published = _publisher.Messages.Should()
            .ContainSingle(message => message.RoutingKey == RabbitMQConstants.RoutingKeys.Analyze)
            .Subject;

        var analyze = published.Payload.Should().BeOfType<AnalyzeVideo>().Subject;
        analyze.VideoId.Should().Be(_videoId);
        analyze.CaptureIntervalMs.Should().Be(_detectionIntervalMs);
        File.Exists(analyze.Path).Should().BeTrue();
    }

    [Given("an imported video named {string}")]
    public async Task GivenAnImportedVideoNamed(string originalFileName)
    {
        _videoId = await SeedVideoAsync(originalFileName: originalFileName);
    }

    [When("the reviewer changes the blur size to {int} percent and the time buffer to {int} ms")]
    public async Task WhenTheReviewerChangesTheBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        _lastResult = await CreateVideosController().UpdateVideoSettings(_videoId, new AnonymizationSettingsDto
        {
            BlurSizePercent = blurSizePercent,
            TimeBufferMs = timeBufferMs
        });
    }

    [Then("the saved videos list shows blur size {int} percent and time buffer {int} ms")]
    public async Task ThenTheSavedVideosListShowsBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        _lastResult.Should().BeOfType<OkObjectResult>();
        var videos = GetOkPayload<List<VideoDto>>(await CreateVideosController().GetVideos());
        videos.Should().ContainSingle(video =>
            video.Id == _videoId
            && video.BlurSizePercent == blurSizePercent
            && video.TimeBufferMs == timeBufferMs);
    }

    [Then("the database stores blur size {int} percent and time buffer {int} ms")]
    public async Task ThenTheDatabaseStoresBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var persisted = await db.Videos.SingleAsync(v => v.Id == _videoId);
        persisted.BlurSizePercent.Should().Be(blurSizePercent);
        persisted.TimeBufferMs.Should().Be(timeBufferMs);
    }

    [Given("a reviewed video has one detected face")]
    public async Task GivenAReviewedVideoHasOneDetectedFace()
    {
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _existingObjectId = Guid.NewGuid();

        await SeedVideoAsync(_videoId, [CreateFrame(_frameId, _videoId, [CreateObject(_existingObjectId, _frameId, trackId: 1)])]);
    }

    [When("the reviewer adds another face, moves it, deselects it, and deletes the original face")]
    public async Task WhenTheReviewerAddsMovesDeselectsAndDeletesFaces()
    {
        _addedObjectId = Guid.NewGuid();
        var controller = CreateDetectedObjectsController();
        var added = CreateObjectDto(_addedObjectId, _frameId, trackId: 2, x: 50, y: 60);

        var addResult = await controller.AddDetectedObject(_videoId, _frameId, added);
        addResult.Should().BeOfType<CreatedAtActionResult>();

        var updated = CreateObjectDto(_addedObjectId, _frameId, trackId: 2, selected: false, x: 70, y: 80, width: 42, height: 43);
        var updateResult = await controller.UpdateDetectedObject(_videoId, _frameId, _addedObjectId, updated);
        updateResult.Should().BeOfType<OkObjectResult>();

        var deleteResult = await controller.DeleteDetectedObject(_videoId, _frameId, _existingObjectId);
        deleteResult.Should().BeOfType<OkObjectResult>();
    }

    [Then("reopening the analyzed video shows only the edited face")]
    public async Task ThenReopeningTheAnalyzedVideoShowsOnlyTheEditedFace()
    {
        _listedFrames = GetOkPayload<List<AnalyzedFrameDto>>(await CreateVideosController().GetAnalyzedVideo(_videoId));
        _listedFrames.Should().ContainSingle();

        var objects = _listedFrames.Single().DetectedObjects;
        objects.Should().ContainSingle();
        objects.Single().Should().BeEquivalentTo(CreateObjectDto(
            _addedObjectId,
            _frameId,
            trackId: 2,
            selected: false,
            x: 70,
            y: 80,
            width: 42,
            height: 43));
    }

    [Given("a reviewed video and another video both have detected faces")]
    public async Task GivenAReviewedVideoAndAnotherVideoBothHaveDetectedFaces()
    {
        _videoId = Guid.NewGuid();
        _otherVideoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _secondFrameId = Guid.NewGuid();
        _foreignFrameId = Guid.NewGuid();
        _existingObjectId = Guid.NewGuid();
        _secondObjectId = Guid.NewGuid();
        _foreignObjectId = Guid.NewGuid();

        await SeedVideoAsync(_videoId,
        [
            CreateFrame(_frameId, _videoId, [CreateObject(_existingObjectId, _frameId, trackId: 1)]),
            CreateFrame(_secondFrameId, _videoId, [CreateObject(_secondObjectId, _secondFrameId, trackId: 2)])
        ]);
        await SeedVideoAsync(_otherVideoId, [CreateFrame(_foreignFrameId, _otherVideoId, [CreateObject(_foreignObjectId, _foreignFrameId, trackId: 9)])]);
    }

    [When("a bulk edit includes a face from the other video")]
    public async Task WhenABulkEditIncludesAFaceFromTheOtherVideo()
    {
        _lastResult = await CreateDetectedObjectsController().BulkUpdateDetectedObjects(_videoId,
        [
            CreateObjectDto(_existingObjectId, _frameId, trackId: 20),
            CreateObjectDto(_foreignObjectId, _foreignFrameId, trackId: 20)
        ]);
    }

    [Then("no face in the reviewed video is partially changed")]
    public async Task ThenNoFaceInTheReviewedVideoIsPartiallyChanged()
    {
        _lastResult.Should().BeOfType<NotFoundResult>();
        await AssertDetectedObjectAsync(_existingObjectId, obj => obj.TrackId.Should().Be(1));
    }

    [When("the reviewer bulk edits only faces from the reviewed video")]
    public async Task WhenTheReviewerBulkEditsOnlyFacesFromTheReviewedVideo()
    {
        _lastResult = await CreateDetectedObjectsController().BulkUpdateDetectedObjects(_videoId,
        [
            CreateObjectDto(_existingObjectId, _frameId, trackId: 20, selected: false),
            CreateObjectDto(_secondObjectId, _secondFrameId, trackId: 20, selected: false)
        ]);
    }

    [Then("all reviewed video faces are saved together")]
    public async Task ThenAllReviewedVideoFacesAreSavedTogether()
    {
        _lastResult.Should().BeOfType<OkObjectResult>();
        await AssertDetectedObjectAsync(_existingObjectId, obj =>
        {
            obj.TrackId.Should().Be(20);
            obj.Selected.Should().BeFalse();
        });
        await AssertDetectedObjectAsync(_secondObjectId, obj =>
        {
            obj.TrackId.Should().Be(20);
            obj.Selected.Should().BeFalse();
        });
    }

    [When("object changes use mismatched route and body identifiers")]
    public async Task WhenObjectChangesUseMismatchedRouteAndBodyIdentifiers()
    {
        var controller = CreateDetectedObjectsController();

        var mismatchedAdd = await controller.AddDetectedObject(
            _videoId,
            _frameId,
            CreateObjectDto(Guid.NewGuid(), Guid.NewGuid(), trackId: 1));

        var mismatchedUpdate = await controller.UpdateDetectedObject(
            _videoId,
            _frameId,
            Guid.NewGuid(),
            CreateObjectDto(_existingObjectId, _frameId, trackId: 3));

        var wrongVideo = await controller.UpdateDetectedObject(
            Guid.NewGuid(),
            _frameId,
            _existingObjectId,
            CreateObjectDto(_existingObjectId, _frameId, trackId: 3));

        mismatchedAdd.Should().BeOfType<BadRequestObjectResult>();
        mismatchedUpdate.Should().BeOfType<BadRequestObjectResult>();
        wrongVideo.Should().BeOfType<NotFoundResult>();
    }

    [Then("the object changes are rejected")]
    public async Task ThenTheObjectChangesAreRejected()
    {
        await AssertDetectedObjectAsync(_existingObjectId, obj => obj.TrackId.Should().Be(1));
    }

    [Given("a saved video has original and anonymized file paths")]
    public async Task GivenASavedVideoHasOriginalAndAnonymizedFilePaths()
    {
        _videoId = Guid.NewGuid();
        _originalPath = WriteVideoFile("source.mp4");
        _anonymizedPath = WriteVideoFile("source_anonymized.mp4");
        await SeedVideoAsync(_videoId, sourcePath: _originalPath, anonymizedPath: _anonymizedPath);
    }

    [When("the viewer requests the original and anonymized files")]
    public async Task WhenTheViewerRequestsTheOriginalAndAnonymizedFiles()
    {
        var controller = CreateVideosController();
        _lastOriginalFileResult = await controller.GetOriginalVideo(_videoId);
        _lastAnonymizedFileResult = await controller.GetAnonymizedVideo(_videoId);
    }

    [Then("the API streams both persisted video files")]
    public void ThenTheApiStreamsBothPersistedVideoFiles()
    {
        var originalFile = _lastOriginalFileResult.Should().BeOfType<PhysicalFileResult>().Subject;
        originalFile.FileName.Should().Be(_originalPath);
        originalFile.FileDownloadName.Should().Be("source.mp4");
        originalFile.ContentType.Should().StartWith("video/");

        var anonymizedFile = _lastAnonymizedFileResult.Should().BeOfType<PhysicalFileResult>().Subject;
        anonymizedFile.FileName.Should().Be(_anonymizedPath);
        anonymizedFile.FileDownloadName.Should().Be("source_anonymized.mp4");
        anonymizedFile.ContentType.Should().StartWith("video/");
    }

    [Given("a file-backed local database contains a reviewed video")]
    public async Task GivenAFileBackedLocalDatabaseContainsAReviewedVideo()
    {
        var dbPath = Path.Combine(_contentRoot, "restart-proof.db");
        _videoId = Guid.NewGuid();
        _frameId = Guid.NewGuid();
        _existingObjectId = Guid.NewGuid();

        await using var firstProvider = CreateFileBackedSqliteProvider(dbPath);
        var dbFactory = firstProvider.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        db.Videos.Add(CreateVideo(_videoId,
            originalFileName: "after-restart.mp4",
            frames: [CreateFrame(_frameId, _videoId, [CreateObject(_existingObjectId, _frameId, trackId: 12)])],
            blurSizePercent: 190,
            timeBufferMs: 700));
        await db.SaveChangesAsync();
    }

    [When("the API services are recreated")]
    public void WhenTheApiServicesAreRecreated()
    {
        var dbPath = Path.Combine(_contentRoot, "restart-proof.db");
        _restartedServices = CreateFileBackedSqliteProvider(dbPath);
    }

    [Then("the saved video, settings, frame and face are still available")]
    public async Task ThenTheSavedVideoSettingsFrameAndFaceAreStillAvailable()
    {
        var restartedFactory = _restartedServices!.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
        var videoDataService = new VideoDataService(restartedFactory);

        var videos = await videoDataService.GetVideos();
        videos.Should().ContainSingle(video =>
            video.Id == _videoId
            && video.OriginalFileName == "after-restart.mp4"
            && video.BlurSizePercent == 190
            && video.TimeBufferMs == 700);

        var frames = await videoDataService.GetAnalyzedVideo(_videoId);
        frames.Should().ContainSingle(frame =>
            frame.Id == _frameId
            && frame.DetectedObjects.Single().Id == _existingObjectId
            && frame.DetectedObjects.Single().TrackId == 12);
    }

    private VideosController CreateVideosController(RecordingMessagePublisher? publisher = null) =>
        new(
            publisher ?? new RecordingMessagePublisher(),
            new TestWebHostEnvironment(_contentRoot),
            new VideoDataService(_dbFactory));

    private DetectedObjectsController CreateDetectedObjectsController() =>
        new(new DetectedObjectDataService(_dbFactory));

    private async Task<Guid> SeedVideoAsync(
        string originalFileName = "sample.mp4",
        string? sourcePath = null,
        string? anonymizedPath = null)
    {
        var videoId = Guid.NewGuid();
        await SeedVideoAsync(videoId, sourcePath: sourcePath, anonymizedPath: anonymizedPath, originalFileName: originalFileName);
        return videoId;
    }

    private async Task SeedVideoAsync(
        Guid videoId,
        IReadOnlyList<AnalyzedFrame>? frames = null,
        string? sourcePath = null,
        string? anonymizedPath = null,
        string originalFileName = "sample.mp4")
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Videos.Add(CreateVideo(videoId, originalFileName, frames, sourcePath, anonymizedPath));
        await db.SaveChangesAsync();
    }

    private Video CreateVideo(
        Guid videoId,
        string originalFileName = "sample.mp4",
        IReadOnlyList<AnalyzedFrame>? frames = null,
        string? sourcePath = null,
        string? anonymizedPath = null,
        int blurSizePercent = 120,
        int timeBufferMs = 300)
    {
        return new Video
        {
            Id = videoId,
            SourcePath = sourcePath ?? WriteVideoFile($"{videoId}.mp4"),
            AnonomizedPath = anonymizedPath,
            OriginalFileName = originalFileName,
            BlurSizePercent = blurSizePercent,
            TimeBufferMs = timeBufferMs,
            AnalyzedFrames = frames?.ToList() ?? []
        };
    }

    private static AnalyzedFrame CreateFrame(Guid frameId, Guid videoId, IReadOnlyList<DetectedObject> objects) =>
        new()
        {
            Id = frameId,
            VideoId = videoId,
            TimeSeconds = 1.25,
            DetectedObjects = objects.ToList()
        };

    private static DetectedObject CreateObject(Guid objectId, Guid frameId, int trackId, bool selected = true) =>
        new()
        {
            Id = objectId,
            AnalyzedFrameId = frameId,
            Confidence = 0.95,
            ClassName = "face",
            Selected = selected,
            TrackId = trackId,
            X = 10,
            Y = 20,
            Width = 30,
            Height = 40
        };

    private static DetectedObjectDto CreateObjectDto(
        Guid objectId,
        Guid frameId,
        int trackId,
        bool selected = true,
        int x = 10,
        int y = 20,
        int width = 30,
        int height = 40) =>
        new()
        {
            Id = objectId,
            AnalyzedFrameId = frameId,
            Confidence = 0.95,
            ClassName = "face",
            Selected = selected,
            TrackId = trackId,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };

    private static T GetOkPayload<T>(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeAssignableTo<ApiResponse<T>>().Subject;
        response.Payload.Should().NotBeNull();
        return response.Payload!;
    }

    private async Task AssertDetectedObjectAsync(Guid objectId, Action<DetectedObject> assertion)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.DetectedObjects.SingleAsync(o => o.Id == objectId);
        assertion(entity);
    }

    private FormFile CreateVideoUpload(string fileName)
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "video", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "video/mp4"
        };
    }

    private string WriteVideoFile(string fileName)
    {
        var path = Path.Combine(_contentRoot, fileName);
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5]);
        return path;
    }

    private static ServiceProvider CreateFileBackedSqliteProvider(string dbPath)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<VideoAnonymizerDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}", sqlite => sqlite.MigrationsAssembly("VideoAnonymizer.Database.SQLite")));
        return services.BuildServiceProvider();
    }

    private sealed class RecordingMessagePublisher : IMessagePublisher
    {
        public List<(string RoutingKey, object? Payload)> Messages { get; } = [];

        public Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default)
        {
            Messages.Add((routingKey, message));
            return Task.CompletedTask;
        }
    }

    private sealed class TestWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "VideoAnonymizer.ApiService.Tests";
        public string WebRootPath { get; set; } = contentRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
