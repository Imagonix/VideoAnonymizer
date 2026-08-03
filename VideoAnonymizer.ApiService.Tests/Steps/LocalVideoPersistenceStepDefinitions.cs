using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using VideoAnonymizer.VideoProcessor.Anonymization;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class LocalVideoPersistenceStepDefinitions
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

    private ServiceProvider RestartedServices
    {
        get => _scenarioContext.Get<ServiceProvider>(nameof(RestartedServices));
        set => _scenarioContext.Set(value, nameof(RestartedServices));
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

    private RecordingMessagePublisher Publisher
    {
        get => _scenarioContext.Get<RecordingMessagePublisher>(nameof(Publisher));
        set => _scenarioContext.Set(value, nameof(Publisher));
    }

    private Guid VideoId
    {
        get => _scenarioContext.Get<Guid>(nameof(VideoId));
        set => _scenarioContext.Set(value, nameof(VideoId));
    }

    private Guid OtherVideoId
    {
        get => _scenarioContext.Get<Guid>(nameof(OtherVideoId));
        set => _scenarioContext.Set(value, nameof(OtherVideoId));
    }

    private Guid FrameId
    {
        get => _scenarioContext.Get<Guid>(nameof(FrameId));
        set => _scenarioContext.Set(value, nameof(FrameId));
    }

    private Guid SecondFrameId
    {
        get => _scenarioContext.Get<Guid>(nameof(SecondFrameId));
        set => _scenarioContext.Set(value, nameof(SecondFrameId));
    }

    private Guid ForeignFrameId
    {
        get => _scenarioContext.Get<Guid>(nameof(ForeignFrameId));
        set => _scenarioContext.Set(value, nameof(ForeignFrameId));
    }

    private Guid ExistingObjectId
    {
        get => _scenarioContext.Get<Guid>(nameof(ExistingObjectId));
        set => _scenarioContext.Set(value, nameof(ExistingObjectId));
    }

    private Guid AddedObjectId
    {
        get => _scenarioContext.Get<Guid>(nameof(AddedObjectId));
        set => _scenarioContext.Set(value, nameof(AddedObjectId));
    }

    private Guid SecondObjectId
    {
        get => _scenarioContext.Get<Guid>(nameof(SecondObjectId));
        set => _scenarioContext.Set(value, nameof(SecondObjectId));
    }

    private Guid ForeignObjectId
    {
        get => _scenarioContext.Get<Guid>(nameof(ForeignObjectId));
        set => _scenarioContext.Set(value, nameof(ForeignObjectId));
    }

    private Guid FirstOccurrenceId
    {
        get => _scenarioContext.Get<Guid>(nameof(FirstOccurrenceId));
        set => _scenarioContext.Set(value, nameof(FirstOccurrenceId));
    }

    private Guid SecondOccurrenceId
    {
        get => _scenarioContext.Get<Guid>(nameof(SecondOccurrenceId));
        set => _scenarioContext.Set(value, nameof(SecondOccurrenceId));
    }

    private Guid FourthOccurrenceId
    {
        get => _scenarioContext.Get<Guid>(nameof(FourthOccurrenceId));
        set => _scenarioContext.Set(value, nameof(FourthOccurrenceId));
    }

    private Guid UntrackedOccurrenceId
    {
        get => _scenarioContext.Get<Guid>(nameof(UntrackedOccurrenceId));
        set => _scenarioContext.Set(value, nameof(UntrackedOccurrenceId));
    }

    private Dictionary<Guid, ConsecutiveSegment> ResolvedSegments
    {
        get => _scenarioContext.Get<Dictionary<Guid, ConsecutiveSegment>>(nameof(ResolvedSegments));
        set => _scenarioContext.Set(value, nameof(ResolvedSegments));
    }

    private List<DetectedObject> NormalizationOccurrences
    {
        get => _scenarioContext.Get<List<DetectedObject>>(nameof(NormalizationOccurrences));
        set => _scenarioContext.Set(value, nameof(NormalizationOccurrences));
    }

    private DetectedObject OverridingFace
    {
        get => _scenarioContext.Get<DetectedObject>(nameof(OverridingFace));
        set => _scenarioContext.Set(value, nameof(OverridingFace));
    }

    private DetectedObject PlainFace
    {
        get => _scenarioContext.Get<DetectedObject>(nameof(PlainFace));
        set => _scenarioContext.Set(value, nameof(PlainFace));
    }

    private int GlobalBlurSize
    {
        get => _scenarioContext.Get<int>(nameof(GlobalBlurSize));
        set => _scenarioContext.Set(value, nameof(GlobalBlurSize));
    }

    private (int Overriding, int Plain) ResolvedBlurSizes
    {
        get => _scenarioContext.Get<(int, int)>(nameof(ResolvedBlurSizes));
        set => _scenarioContext.Set(value, nameof(ResolvedBlurSizes));
    }

    private string UploadedFileName
    {
        get => _scenarioContext.Get<string>(nameof(UploadedFileName));
        set => _scenarioContext.Set(value, nameof(UploadedFileName));
    }

    private int DetectionIntervalMs
    {
        get => _scenarioContext.Get<int>(nameof(DetectionIntervalMs));
        set => _scenarioContext.Set(value, nameof(DetectionIntervalMs));
    }

    private string OriginalPath
    {
        get => _scenarioContext.Get<string>(nameof(OriginalPath));
        set => _scenarioContext.Set(value, nameof(OriginalPath));
    }

    private string AnonymizedPath
    {
        get => _scenarioContext.Get<string>(nameof(AnonymizedPath));
        set => _scenarioContext.Set(value, nameof(AnonymizedPath));
    }

    private IActionResult LastResult
    {
        get => _scenarioContext.Get<IActionResult>(nameof(LastResult));
        set => _scenarioContext.Set(value, nameof(LastResult));
    }

    private IActionResult LastOriginalFileResult
    {
        get => _scenarioContext.Get<IActionResult>(nameof(LastOriginalFileResult));
        set => _scenarioContext.Set(value, nameof(LastOriginalFileResult));
    }

    private IActionResult LastAnonymizedFileResult
    {
        get => _scenarioContext.Get<IActionResult>(nameof(LastAnonymizedFileResult));
        set => _scenarioContext.Set(value, nameof(LastAnonymizedFileResult));
    }

    private List<VideoDto> ListedVideos
    {
        get => _scenarioContext.Get<List<VideoDto>>(nameof(ListedVideos));
        set => _scenarioContext.Set(value, nameof(ListedVideos));
    }

    private List<AnalyzedFrameDto> ListedFrames
    {
        get => _scenarioContext.Get<List<AnalyzedFrameDto>>(nameof(ListedFrames));
        set => _scenarioContext.Set(value, nameof(ListedFrames));
    }

    private DeleteVideoResultDto DeleteResult
    {
        get => _scenarioContext.Get<DeleteVideoResultDto>(nameof(DeleteResult));
        set => _scenarioContext.Set(value, nameof(DeleteResult));
    }

    private string HostedStorageRoot
    {
        get => _scenarioContext.Get<string>(nameof(HostedStorageRoot));
        set => _scenarioContext.Set(value, nameof(HostedStorageRoot));
    }

    public LocalVideoPersistenceStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario("api_persistence")]
    public async Task SetUp()
    {
        ContentRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "persistence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ContentRoot);

        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<VideoAnonymizerDbContext>(options => options.UseSqlite(Connection));
        Services = services.BuildServiceProvider();
        DbFactory = Services.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();

        await using var db = await DbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    [AfterScenario("api_persistence")]
    public async Task TearDown()
    {
        if (_scenarioContext.TryGetValue<ServiceProvider>(nameof(RestartedServices), out var restartedServices))
        {
            await restartedServices.DisposeAsync();
        }

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

    [Given("a reviewer uploads {string} for object detection every {int} ms")]
    public async Task GivenAReviewerUploadsForObjectDetectionEveryMs(string fileName, int detectionIntervalMs)
    {
        Publisher = new RecordingMessagePublisher();
        UploadedFileName = fileName;
        DetectionIntervalMs = detectionIntervalMs;
        var upload = CreateVideoUpload(fileName);

        LastResult = await CreateVideosController(Publisher)
            .Analyze(upload, CancellationToken.None, detectionIntervalMs);

        VideoId = GetOkPayload<Guid>(LastResult);
    }

    [When("the reviewer opens the saved videos list")]
    public async Task WhenTheReviewerOpensTheSavedVideosList()
    {
        ListedVideos = GetOkPayload<List<VideoDto>>(await CreateVideosController().GetVideos());
    }

    [Then("the uploaded video is listed with the default anonymization settings")]
    public void ThenTheUploadedVideoIsListedWithDefaultAnonymizationSettings()
    {
        ListedVideos.Should().ContainSingle(video =>
            video.Id == VideoId
            && video.OriginalFileName == UploadedFileName
            && video.BlurSizePercent == 120
            && video.TimeBufferMs == 300);
    }

    [Then("an analysis job is published for the stored video")]
    public void ThenAnAnalysisJobIsPublishedForTheStoredVideo()
    {
        var published = Publisher.Messages.Should()
            .ContainSingle(message => message.RoutingKey == RabbitMQConstants.RoutingKeys.Analyze)
            .Subject;

        var analyze = published.Payload.Should().BeOfType<AnalyzeVideo>().Subject;
        analyze.VideoId.Should().Be(VideoId);
        analyze.CaptureIntervalMs.Should().Be(DetectionIntervalMs);
        File.Exists(analyze.Path).Should().BeTrue();
    }

    [Given("an imported video named {string}")]
    public async Task GivenAnImportedVideoNamed(string originalFileName)
    {
        VideoId = await SeedVideoAsync(originalFileName: originalFileName);
    }

    [When("the reviewer changes the blur size to {int} percent and the time buffer to {int} ms")]
    public async Task WhenTheReviewerChangesTheBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        LastResult = await CreateVideosController().UpdateVideoSettings(VideoId, new AnonymizationSettingsDto
        {
            BlurSizePercent = blurSizePercent,
            TimeBufferMs = timeBufferMs
        });
    }

    [Then("the saved videos list shows blur size {int} percent and time buffer {int} ms")]
    public async Task ThenTheSavedVideosListShowsBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        LastResult.Should().BeOfType<OkObjectResult>();
        var videos = GetOkPayload<List<VideoDto>>(await CreateVideosController().GetVideos());
        videos.Should().ContainSingle(video =>
            video.Id == VideoId
            && video.BlurSizePercent == blurSizePercent
            && video.TimeBufferMs == timeBufferMs);
    }

    [Then("the database stores blur size {int} percent and time buffer {int} ms")]
    public async Task ThenTheDatabaseStoresBlurSizeAndTimeBuffer(int blurSizePercent, int timeBufferMs)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var persisted = await db.Videos.SingleAsync(v => v.Id == VideoId);
        persisted.BlurSizePercent.Should().Be(blurSizePercent);
        persisted.TimeBufferMs.Should().Be(timeBufferMs);
    }

    [Given("a reviewed video has one detected face")]
    public async Task GivenAReviewedVideoHasOneDetectedFace()
    {
        VideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();

        await SeedVideoAsync(VideoId, [CreateFrame(FrameId, VideoId, [CreateObject(ExistingObjectId, FrameId, trackId: 1)])]);
    }

    [When("the reviewer adds another face, moves it, deselects it, and deletes the original face")]
    public async Task WhenTheReviewerAddsMovesDeselectsAndDeletesFaces()
    {
        AddedObjectId = Guid.NewGuid();
        var controller = CreateDetectedObjectsController();
        var added = CreateObjectDto(AddedObjectId, FrameId, trackId: 2, x: 50, y: 60);

        var addResult = await controller.AddDetectedObject(VideoId, FrameId, added);
        addResult.Should().BeOfType<CreatedAtActionResult>();

        var updated = CreateObjectDto(AddedObjectId, FrameId, trackId: 2, selected: false, x: 70, y: 80, width: 42, height: 43);
        var updateResult = await controller.UpdateDetectedObject(VideoId, FrameId, AddedObjectId, updated);
        updateResult.Should().BeOfType<OkObjectResult>();

        var deleteResult = await controller.DeleteDetectedObject(VideoId, FrameId, ExistingObjectId);
        deleteResult.Should().BeOfType<OkObjectResult>();
    }

    [Then("reopening the analyzed video shows only the edited face")]
    public async Task ThenReopeningTheAnalyzedVideoShowsOnlyTheEditedFace()
    {
        ListedFrames = GetOkPayload<List<AnalyzedFrameDto>>(await CreateVideosController().GetAnalyzedVideo(VideoId));
        ListedFrames.Should().ContainSingle();

        var objects = ListedFrames.Single().DetectedObjects;
        objects.Should().ContainSingle();
        objects.Single().Should().BeEquivalentTo(CreateObjectDto(
            AddedObjectId,
            FrameId,
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
        VideoId = Guid.NewGuid();
        OtherVideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        SecondFrameId = Guid.NewGuid();
        ForeignFrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();
        SecondObjectId = Guid.NewGuid();
        ForeignObjectId = Guid.NewGuid();

        await SeedVideoAsync(VideoId,
        [
            CreateFrame(FrameId, VideoId, [CreateObject(ExistingObjectId, FrameId, trackId: 1)]),
            CreateFrame(SecondFrameId, VideoId, [CreateObject(SecondObjectId, SecondFrameId, trackId: 2)])
        ]);
        await SeedVideoAsync(OtherVideoId, [CreateFrame(ForeignFrameId, OtherVideoId, [CreateObject(ForeignObjectId, ForeignFrameId, trackId: 9)])]);
    }

    [Given("a reviewed video has two detected faces")]
    public async Task GivenAReviewedVideoHasTwoDetectedFaces()
    {
        VideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();
        SecondObjectId = Guid.NewGuid();

        await SeedVideoAsync(VideoId,
        [
            CreateFrame(FrameId, VideoId,
            [
                CreateObject(ExistingObjectId, FrameId, trackId: 1),
                CreateObject(SecondObjectId, FrameId, trackId: 2)
            ])
        ]);
    }

    [When("the reviewer saves the first face with custom track settings")]
    public async Task WhenTheReviewerSavesTheFirstFaceWithCustomTrackSettings()
    {
        var dto = CreateObjectDto(ExistingObjectId, FrameId, trackId: 1);
        dto.BlurShape = "rectangle";
        dto.BlurSizePercentOverride = 140;
        dto.PreBufferMsOverride = 220;
        dto.PostBufferMsOverride = 480;

        LastResult = await CreateDetectedObjectsController()
            .UpdateDetectedObject(VideoId, FrameId, ExistingObjectId, dto);
    }

    [Then("the first face keeps its custom track settings when reopening the video")]
    public async Task ThenTheFirstFaceKeepsItsCustomTrackSettingsWhenReopeningTheVideo()
    {
        LastResult.Should().BeOfType<OkObjectResult>();
        ListedFrames = GetOkPayload<List<AnalyzedFrameDto>>(await CreateVideosController().GetAnalyzedVideo(VideoId));
        var first = ListedFrames.Single().DetectedObjects.Single(obj => obj.Id == ExistingObjectId);
        first.BlurShape.Should().Be("rectangle");
        first.BlurSizePercentOverride.Should().Be(140);
        first.PreBufferMsOverride.Should().Be(220);
        first.PostBufferMsOverride.Should().Be(480);
    }

    [Then("the second face still has no track overrides")]
    public async Task ThenTheSecondFaceStillHasNoTrackOverrides()
    {
        var second = ListedFrames.Single().DetectedObjects.Single(obj => obj.Id == SecondObjectId);
        second.BlurShape.Should().BeNull();
        second.BlurSizePercentOverride.Should().BeNull();
        second.PreBufferMsOverride.Should().BeNull();
        second.PostBufferMsOverride.Should().BeNull();
    }

    [Given("a reviewed video has a track with occurrences in the first, second, and fourth frames and an untracked occurrence in the fifth frame")]
    public async Task GivenATrackWithOccurrencesInFirstSecondAndFourthFrames()
    {
        VideoId = Guid.NewGuid();
        FirstOccurrenceId = Guid.NewGuid();
        SecondOccurrenceId = Guid.NewGuid();
        FourthOccurrenceId = Guid.NewGuid();
        UntrackedOccurrenceId = Guid.NewGuid();
        var frame0 = Guid.NewGuid();
        var frame1 = Guid.NewGuid();
        var frame2 = Guid.NewGuid();
        var frame3 = Guid.NewGuid();
        var frame4 = Guid.NewGuid();

        await SeedVideoAsync(VideoId,
        [
            CreateFrame(frame0, VideoId, [CreateObject(FirstOccurrenceId, frame0, trackId: 7)], frameIndex: 0),
            CreateFrame(frame1, VideoId, [CreateObject(SecondOccurrenceId, frame1, trackId: 7)], frameIndex: 1),
            CreateFrame(frame2, VideoId, [], frameIndex: 2),
            CreateFrame(frame3, VideoId, [CreateObject(FourthOccurrenceId, frame3, trackId: 7)], frameIndex: 3),
            CreateFrame(frame4, VideoId, [CreateObject(UntrackedOccurrenceId, frame4, trackId: null)], frameIndex: 4)
        ]);
    }

    [When("the reviewer resolves the segment of each occurrence")]
    public async Task WhenTheReviewerResolvesTheSegmentOfEachOccurrence()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        var frames = await db.AnalyzedFrames
            .Include(frame => frame.DetectedObjects)
            .Where(frame => frame.VideoId == VideoId)
            .ToListAsync();

        ResolvedSegments = new Dictionary<Guid, ConsecutiveSegment>
        {
            [FirstOccurrenceId] = ConsecutiveSegmentResolver.Find(frames, db.DetectedObjects.Single(o => o.Id == FirstOccurrenceId)),
            [SecondOccurrenceId] = ConsecutiveSegmentResolver.Find(frames, db.DetectedObjects.Single(o => o.Id == SecondOccurrenceId)),
            [FourthOccurrenceId] = ConsecutiveSegmentResolver.Find(frames, db.DetectedObjects.Single(o => o.Id == FourthOccurrenceId)),
            [UntrackedOccurrenceId] = ConsecutiveSegmentResolver.Find(frames, db.DetectedObjects.Single(o => o.Id == UntrackedOccurrenceId))
        };
    }

    [Then("the segment of the first occurrence spans the second occurrence")]
    public void ThenTheSegmentOfTheFirstOccurrenceSpansTheSecondOccurrence()
    {
        var segment = ResolvedSegments[FirstOccurrenceId];
        segment.First.Id.Should().Be(FirstOccurrenceId);
        segment.Last.Id.Should().Be(SecondOccurrenceId);
        segment.Occurrences.Select(o => o.Id).Should().Equal(FirstOccurrenceId, SecondOccurrenceId);
    }

    [Then("the segment of the second occurrence is unchanged by the missing third frame")]
    public void ThenTheSegmentOfTheSecondOccurrenceIsUnchangedByTheMissingThirdFrame()
    {
        var segment = ResolvedSegments[SecondOccurrenceId];
        segment.First.Id.Should().Be(FirstOccurrenceId);
        segment.Last.Id.Should().Be(SecondOccurrenceId);
        segment.Occurrences.Select(o => o.Id).Should().Equal(FirstOccurrenceId, SecondOccurrenceId);
    }

    [Then("the fourth occurrence forms a single-occurrence segment after the gap")]
    public void ThenTheFourthOccurrenceFormsASingleOccurrenceSegmentAfterTheGap()
    {
        var segment = ResolvedSegments[FourthOccurrenceId];
        segment.First.Id.Should().Be(FourthOccurrenceId);
        segment.Last.Id.Should().Be(FourthOccurrenceId);
        segment.Occurrences.Select(o => o.Id).Should().Equal(FourthOccurrenceId);
    }

    [Then("the untracked occurrence forms a single-occurrence segment")]
    public void ThenTheUntrackedOccurrenceFormsASingleOccurrenceSegment()
    {
        var segment = ResolvedSegments[UntrackedOccurrenceId];
        segment.First.Id.Should().Be(UntrackedOccurrenceId);
        segment.Last.Id.Should().Be(UntrackedOccurrenceId);
        segment.Occurrences.Select(o => o.Id).Should().Equal(UntrackedOccurrenceId);
    }

    [Given("a segment now spans previously separate runs")]
    public void GivenASegmentNowSpansPreviouslySeparateRuns()
    {
        NormalizationOccurrences =
        [
            new DetectedObject { Id = Guid.NewGuid(), TrackId = 7, PreBufferMsOverride = 100, PostBufferMsOverride = 200 },
            new DetectedObject { Id = Guid.NewGuid(), TrackId = 7, PreBufferMsOverride = 300, PostBufferMsOverride = 400 },
            new DetectedObject { Id = Guid.NewGuid(), TrackId = 7, PreBufferMsOverride = 500, PostBufferMsOverride = 600 }
        ];
    }

    [When("the reviewer normalizes the segment boundaries")]
    public void WhenTheReviewerNormalizesTheSegmentBoundaries()
    {
        SegmentBoundaryNormalizer.Normalize(NormalizationOccurrences);
    }

    [Then("only the first occurrence stores a pre-buffer override")]
    public void ThenOnlyTheFirstOccurrenceStoresAPreBufferOverride()
    {
        NormalizationOccurrences[0].PreBufferMsOverride.Should().Be(100);
        NormalizationOccurrences[0].PostBufferMsOverride.Should().BeNull();
        NormalizationOccurrences.Skip(1).Should().OnlyContain(obj => obj.PreBufferMsOverride == null);
    }

    [Then("only the last occurrence stores a post-buffer override")]
    public void ThenOnlyTheLastOccurrenceStoresAPostBufferOverride()
    {
        NormalizationOccurrences[^1].PostBufferMsOverride.Should().Be(600);
        NormalizationOccurrences[^1].PreBufferMsOverride.Should().BeNull();
        NormalizationOccurrences.Take(NormalizationOccurrences.Count - 1)
            .Should().OnlyContain(obj => obj.PostBufferMsOverride == null);
    }

    [Given("a reviewer's video has a global blur size of {int} percent")]
    public void GivenAGlobalBlurSize(int blurSizePercent)
    {
        GlobalBlurSize = blurSizePercent;
    }

    [Given("one face overrides its blur size while another face has no override")]
    public void GivenOneFaceOverridesItsBlurSizeWhileAnotherFaceHasNoOverride()
    {
        OverridingFace = new DetectedObject { Id = Guid.NewGuid(), BlurSizePercentOverride = 150 };
        PlainFace = new DetectedObject { Id = Guid.NewGuid(), BlurSizePercentOverride = null };
    }

    [When("the effective blur sizes are resolved")]
    public void WhenTheEffectiveBlurSizesAreResolved()
    {
        ResolvedBlurSizes = (
            AnonymizationSettingsResolver.ResolveBlurSize(OverridingFace, GlobalBlurSize),
            AnonymizationSettingsResolver.ResolveBlurSize(PlainFace, GlobalBlurSize));
    }

    [Then("the overriding face resolves to {int} percent and the other face resolves to {int} percent")]
    public void ThenTheResolvedBlurSizes(int overriding, int plain)
    {
        ResolvedBlurSizes.Should().Be((overriding, plain));
    }

    [When("a bulk edit includes a face from the other video")]
    public async Task WhenABulkEditIncludesAFaceFromTheOtherVideo()
    {
        LastResult = await CreateDetectedObjectsController().BulkUpdateDetectedObjects(VideoId,
        [
            CreateObjectDto(ExistingObjectId, FrameId, trackId: 20),
            CreateObjectDto(ForeignObjectId, ForeignFrameId, trackId: 20)
        ]);
    }

    [Then("no face in the reviewed video is partially changed")]
    public async Task ThenNoFaceInTheReviewedVideoIsPartiallyChanged()
    {
        LastResult.Should().BeOfType<NotFoundResult>();
        await AssertDetectedObjectAsync(ExistingObjectId, obj => obj.TrackId.Should().Be(1));
    }

    [When("the reviewer bulk edits only faces from the reviewed video")]
    public async Task WhenTheReviewerBulkEditsOnlyFacesFromTheReviewedVideo()
    {
        LastResult = await CreateDetectedObjectsController().BulkUpdateDetectedObjects(VideoId,
        [
            CreateObjectDto(ExistingObjectId, FrameId, trackId: 20, selected: false),
            CreateObjectDto(SecondObjectId, SecondFrameId, trackId: 20, selected: false)
        ]);
    }

    [Then("all reviewed video faces are saved together")]
    public async Task ThenAllReviewedVideoFacesAreSavedTogether()
    {
        LastResult.Should().BeOfType<OkObjectResult>();
        await AssertDetectedObjectAsync(ExistingObjectId, obj =>
        {
            obj.TrackId.Should().Be(20);
            obj.Selected.Should().BeFalse();
        });
        await AssertDetectedObjectAsync(SecondObjectId, obj =>
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
            VideoId,
            FrameId,
            CreateObjectDto(Guid.NewGuid(), Guid.NewGuid(), trackId: 1));

        var mismatchedUpdate = await controller.UpdateDetectedObject(
            VideoId,
            FrameId,
            Guid.NewGuid(),
            CreateObjectDto(ExistingObjectId, FrameId, trackId: 3));

        var wrongVideo = await controller.UpdateDetectedObject(
            Guid.NewGuid(),
            FrameId,
            ExistingObjectId,
            CreateObjectDto(ExistingObjectId, FrameId, trackId: 3));

        mismatchedAdd.Should().BeOfType<BadRequestObjectResult>();
        mismatchedUpdate.Should().BeOfType<BadRequestObjectResult>();
        wrongVideo.Should().BeOfType<NotFoundResult>();
    }

    [Then("the object changes are rejected")]
    public async Task ThenTheObjectChangesAreRejected()
    {
        await AssertDetectedObjectAsync(ExistingObjectId, obj => obj.TrackId.Should().Be(1));
    }

    [Given("a saved video has original and anonymized file paths")]
    public async Task GivenASavedVideoHasOriginalAndAnonymizedFilePaths()
    {
        VideoId = Guid.NewGuid();
        OriginalPath = WriteVideoFile("source.mp4");
        AnonymizedPath = WriteVideoFile("source_anonymized.mp4");
        await SeedVideoAsync(VideoId, sourcePath: OriginalPath, anonymizedPath: AnonymizedPath);
    }

    [When("the viewer requests the original and anonymized files")]
    public async Task WhenTheViewerRequestsTheOriginalAndAnonymizedFiles()
    {
        var controller = CreateVideosController();
        LastOriginalFileResult = await controller.GetOriginalVideo(VideoId);
        LastAnonymizedFileResult = await controller.GetAnonymizedVideo(VideoId);
    }

    [Then("the API streams both persisted video files")]
    public void ThenTheApiStreamsBothPersistedVideoFiles()
    {
        var originalFile = LastOriginalFileResult.Should().BeOfType<PhysicalFileResult>().Subject;
        originalFile.FileName.Should().Be(OriginalPath);
        originalFile.FileDownloadName.Should().Be("source.mp4");
        originalFile.ContentType.Should().StartWith("video/");

        var anonymizedFile = LastAnonymizedFileResult.Should().BeOfType<PhysicalFileResult>().Subject;
        anonymizedFile.FileName.Should().Be(AnonymizedPath);
        anonymizedFile.FileDownloadName.Should().Be("source_anonymized.mp4");
        anonymizedFile.ContentType.Should().StartWith("video/");
    }

    [Given("a file-backed local database contains a reviewed video")]
    public async Task GivenAFileBackedLocalDatabaseContainsAReviewedVideo()
    {
        var dbPath = Path.Combine(ContentRoot, "restart-proof.db");
        VideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();

        await using var firstProvider = CreateFileBackedSqliteProvider(dbPath);
        var dbFactory = firstProvider.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        db.Videos.Add(CreateVideo(VideoId,
            originalFileName: "after-restart.mp4",
            frames: [CreateFrame(FrameId, VideoId, [CreateObject(ExistingObjectId, FrameId, trackId: 12)])],
            blurSizePercent: 190,
            timeBufferMs: 700));
        await db.SaveChangesAsync();
    }

    [When("the API services are recreated")]
    public void WhenTheApiServicesAreRecreated()
    {
        var dbPath = Path.Combine(ContentRoot, "restart-proof.db");
        RestartedServices = CreateFileBackedSqliteProvider(dbPath);
    }

    [Then("the saved video, settings, frame and face are still available")]
    public async Task ThenTheSavedVideoSettingsFrameAndFaceAreStillAvailable()
    {
        var restartedFactory = RestartedServices.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
        var videoDataService = new VideoDataService(restartedFactory);

        var videos = await videoDataService.GetVideos();
        videos.Should().ContainSingle(video =>
            video.Id == VideoId
            && video.OriginalFileName == "after-restart.mp4"
            && video.BlurSizePercent == 190
            && video.TimeBufferMs == 700);

        var frames = await videoDataService.GetAnalyzedVideo(VideoId);
        frames.Should().ContainSingle(frame =>
            frame.Id == FrameId
            && frame.DetectedObjects.Single().Id == ExistingObjectId
            && frame.DetectedObjects.Single().TrackId == 12);
    }

    [Given("videos exist in imported analyzed and exported states")]
    public async Task GivenVideosExistInImportedAnalyzedAndExportedStates()
    {
        var importedId = Guid.NewGuid();
        var analyzedId = Guid.NewGuid();
        var exportedId = Guid.NewGuid();
        var frameId = Guid.NewGuid();
        var objectId = Guid.NewGuid();

        await SeedVideoAsync(importedId, originalFileName: "status-imported.mp4");
        await SeedVideoAsync(
            analyzedId,
            frames: [CreateFrame(frameId, analyzedId, [CreateObject(objectId, frameId, trackId: 1)])],
            originalFileName: "status-analyzed.mp4");

        var exportedFrameId = Guid.NewGuid();
        var exportedObjectId = Guid.NewGuid();
        var sourcePath = WriteManagedVideoFile(ContentRoot, exportedId, anonymized: false);
        var anonymizedPath = WriteManagedVideoFile(ContentRoot, exportedId, anonymized: true);
        await SeedVideoAsync(
            exportedId,
            frames: [CreateFrame(exportedFrameId, exportedId, [CreateObject(exportedObjectId, exportedFrameId, trackId: 2)])],
            sourcePath: sourcePath,
            anonymizedPath: anonymizedPath,
            originalFileName: "status-exported.mp4");

        VideoId = analyzedId;
    }

    [Then("the listed videos show statuses {string}, {string}, and {string}")]
    public void ThenTheListedVideosShowStatuses(string imported, string ready, string exported)
    {
        ListedVideos.Should().Contain(v =>
            v.OriginalFileName == "status-imported.mp4"
            && v.Status == imported
            && !v.HasAnalysis
            && !v.HasAnonymizedOutput);

        ListedVideos.Should().Contain(v =>
            v.OriginalFileName == "status-analyzed.mp4"
            && v.Status == ready
            && v.HasAnalysis
            && !v.HasAnonymizedOutput);

        ListedVideos.Should().Contain(v =>
            v.OriginalFileName == "status-exported.mp4"
            && v.Status == exported
            && v.HasAnalysis
            && v.HasAnonymizedOutput);
    }

    [Then("the listed video carries a server UTC upload time close to now")]
    public void ThenTheListedVideoCarriesAServerUtcUploadTimeCloseToNow()
    {
        var video = ListedVideos.Single(v => v.Id == VideoId);
        video.UploadedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Given("imported videos exist with known upload times")]
    public async Task GivenImportedVideosExistWithKnownUploadTimes()
    {
        var now = DateTime.UtcNow;
        await SeedVideoAsync(Guid.NewGuid(), originalFileName: "b-newest.mp4", uploadedAtUtc: now);
        await SeedVideoAsync(Guid.NewGuid(), originalFileName: "a-tie.mp4", uploadedAtUtc: now.AddMinutes(-10));
        await SeedVideoAsync(Guid.NewGuid(), originalFileName: "c-tie.mp4", uploadedAtUtc: now.AddMinutes(-10));
        await SeedVideoAsync(Guid.NewGuid(), originalFileName: "z-oldest.mp4", uploadedAtUtc: now.AddMinutes(-30));
    }

    [Then("the listed videos are ordered newest upload first")]
    public void ThenTheListedVideosAreOrderedNewestUploadFirst()
    {
        ListedVideos.Select(v => v.OriginalFileName)
            .Should()
            .Equal("b-newest.mp4", "a-tie.mp4", "c-tie.mp4", "z-oldest.mp4");
    }

    [Then("upload time ties are broken by file name deterministically")]
    public void ThenUploadTimeTiesAreBrokenByFileNameDeterministically()
    {
        // a-tie.mp4 and c-tie.mp4 share one upload time; the stable order keeps a before c.
        var aIndex = ListedVideos.FindIndex(v => v.OriginalFileName == "a-tie.mp4");
        var cIndex = ListedVideos.FindIndex(v => v.OriginalFileName == "c-tie.mp4");
        aIndex.Should().BeGreaterThanOrEqualTo(0);
        cIndex.Should().BeGreaterThan(aIndex);
    }

    [Given("a saved video has original and anonymized file paths under standalone storage")]
    public async Task GivenASavedVideoHasOriginalAndAnonymizedFilePathsUnderStandaloneStorage()
    {
        VideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();
        OriginalPath = WriteManagedVideoFile(ContentRoot, VideoId, anonymized: false);
        AnonymizedPath = WriteManagedVideoFile(ContentRoot, VideoId, anonymized: true);
        await SeedVideoAsync(
            VideoId,
            frames: [CreateFrame(FrameId, VideoId, [CreateObject(ExistingObjectId, FrameId, trackId: 1)])],
            sourcePath: OriginalPath,
            anonymizedPath: AnonymizedPath,
            originalFileName: "standalone-copy.mp4");
    }

    [Given("a saved video has original and anonymized file paths under hosted storage")]
    public async Task GivenASavedVideoHasOriginalAndAnonymizedFilePathsUnderHostedStorage()
    {
        VideoId = Guid.NewGuid();
        FrameId = Guid.NewGuid();
        ExistingObjectId = Guid.NewGuid();
        HostedStorageRoot = Path.Combine(ContentRoot, "hosted-volume");
        Directory.CreateDirectory(HostedStorageRoot);
        OriginalPath = WriteManagedVideoFile(HostedStorageRoot, VideoId, anonymized: false);
        AnonymizedPath = WriteManagedVideoFile(HostedStorageRoot, VideoId, anonymized: true);
        await SeedVideoAsync(
            VideoId,
            frames: [CreateFrame(FrameId, VideoId, [CreateObject(ExistingObjectId, FrameId, trackId: 3)])],
            sourcePath: OriginalPath,
            anonymizedPath: AnonymizedPath,
            originalFileName: "hosted-copy.mp4");
    }

    [Given("the video has editor action history")]
    public async Task GivenTheVideoHasEditorActionHistory()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.EditorActions.Add(new EditorAction
        {
            Id = Guid.NewGuid(),
            VideoId = VideoId,
            ActionType = "object-updated",
            SequenceNumber = 1,
            CreatedAt = DateTime.UtcNow,
            Undone = false,
            Data = "{}"
        });
        await db.SaveChangesAsync();
    }

    [Given("a saved video points its source path outside managed storage")]
    public async Task GivenASavedVideoPointsItsSourcePathOutsideManagedStorage()
    {
        VideoId = Guid.NewGuid();
        var unsafeDir = Path.Combine(ContentRoot, "outside-storage");
        Directory.CreateDirectory(unsafeDir);
        OriginalPath = Path.Combine(unsafeDir, $"{VideoId}.mp4");
        File.WriteAllBytes(OriginalPath, [1, 2, 3, 4]);
        await SeedVideoAsync(
            VideoId,
            sourcePath: OriginalPath,
            anonymizedPath: null,
            originalFileName: "unsafe-path.mp4");
    }

    [When("the reviewer deletes the working copy")]
    public async Task WhenTheReviewerDeletesTheWorkingCopy()
    {
        LastResult = await CreateVideosController().DeleteWorkingCopy(VideoId);
        DeleteResult = GetOkPayload<DeleteVideoResultDto>(LastResult);
    }

    [Then("the video is removed from the database")]
    public async Task ThenTheVideoIsRemovedFromTheDatabase()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        (await db.Videos.AnyAsync(v => v.Id == VideoId)).Should().BeFalse();
        (await db.AnalyzedFrames.AnyAsync(f => f.VideoId == VideoId)).Should().BeFalse();
        (await db.EditorActions.AnyAsync(a => a.VideoId == VideoId)).Should().BeFalse();
    }

    [Then("the standalone source and anonymized files are deleted")]
    public void ThenTheStandaloneSourceAndAnonymizedFilesAreDeleted()
    {
        File.Exists(OriginalPath).Should().BeFalse();
        File.Exists(AnonymizedPath).Should().BeFalse();
        DeleteResult.SourceFileDeleted.Should().BeTrue();
        DeleteResult.AnonymizedFileDeleted.Should().BeTrue();
        DeleteResult.DatabaseDeleted.Should().BeTrue();
    }

    [Then("the hosted source and anonymized files are deleted")]
    public void ThenTheHostedSourceAndAnonymizedFilesAreDeleted()
    {
        File.Exists(OriginalPath).Should().BeFalse();
        File.Exists(AnonymizedPath).Should().BeFalse();
        DeleteResult.SourceFileDeleted.Should().BeTrue();
        DeleteResult.AnonymizedFileDeleted.Should().BeTrue();
        DeleteResult.DatabaseDeleted.Should().BeTrue();
    }

    [Then("the delete result reports no file warnings")]
    public void ThenTheDeleteResultReportsNoFileWarnings()
    {
        DeleteResult.Warnings.Should().BeEmpty();
    }

    [Then("the delete result reports a skipped source file warning")]
    public void ThenTheDeleteResultReportsASkippedSourceFileWarning()
    {
        DeleteResult.DatabaseDeleted.Should().BeTrue();
        DeleteResult.SourceFileDeleted.Should().BeFalse();
        DeleteResult.Warnings.Should().Contain(w => w.Contains("source", StringComparison.OrdinalIgnoreCase));
        File.Exists(OriginalPath).Should().BeTrue();
    }

    private VideosController CreateVideosController(RecordingMessagePublisher? publisher = null) =>
        new(
            publisher ?? new RecordingMessagePublisher(),
            new TestWebHostEnvironment(ContentRoot),
            new VideoDataService(DbFactory),
            CreateConfiguration());

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anonymization:InterpolateTrackedObjects"] = "true"
            })
            .Build();

    private DetectedObjectsController CreateDetectedObjectsController() =>
        new(new DetectedObjectDataService(DbFactory));

    private async Task<Guid> SeedVideoAsync(
        string originalFileName = "sample.mp4",
        string? sourcePath = null,
        string? anonymizedPath = null,
        DateTime? uploadedAtUtc = null)
    {
        var videoId = Guid.NewGuid();
        await SeedVideoAsync(videoId, sourcePath: sourcePath, anonymizedPath: anonymizedPath, originalFileName: originalFileName, uploadedAtUtc: uploadedAtUtc);
        return videoId;
    }

    private async Task SeedVideoAsync(
        Guid videoId,
        IReadOnlyList<AnalyzedFrame>? frames = null,
        string? sourcePath = null,
        string? anonymizedPath = null,
        string originalFileName = "sample.mp4",
        DateTime? uploadedAtUtc = null)
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        db.Videos.Add(CreateVideo(videoId, originalFileName, frames, sourcePath, anonymizedPath, uploadedAtUtc: uploadedAtUtc));
        await db.SaveChangesAsync();
    }

    private Video CreateVideo(
        Guid videoId,
        string originalFileName = "sample.mp4",
        IReadOnlyList<AnalyzedFrame>? frames = null,
        string? sourcePath = null,
        string? anonymizedPath = null,
        int blurSizePercent = 120,
        int timeBufferMs = 300,
        DateTime? uploadedAtUtc = null)
    {
        return new Video
        {
            Id = videoId,
            SourcePath = sourcePath ?? WriteVideoFile($"{videoId}.mp4"),
            AnonomizedPath = anonymizedPath,
            OriginalFileName = originalFileName,
            UploadedAtUtc = uploadedAtUtc ?? DateTime.UtcNow,
            BlurSizePercent = blurSizePercent,
            TimeBufferMs = timeBufferMs,
            AnalyzedFrames = frames?.ToList() ?? []
        };
    }

    private static AnalyzedFrame CreateFrame(
        Guid frameId,
        Guid videoId,
        IReadOnlyList<DetectedObject> objects,
        int frameIndex = 0,
        double timeSeconds = 1.25) =>
        new()
        {
            Id = frameId,
            VideoId = videoId,
            FrameIndex = frameIndex,
            TimeSeconds = timeSeconds,
            DetectedObjects = objects.ToList()
        };

    private static DetectedObject CreateObject(Guid objectId, Guid frameId, int? trackId = 1, bool selected = true) =>
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
        await using var db = await DbFactory.CreateDbContextAsync();
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
        var path = Path.Combine(ContentRoot, fileName);
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5]);
        return path;
    }

    private static string WriteManagedVideoFile(string storageRoot, Guid videoId, bool anonymized)
    {
        var uploadsRoot = Path.Combine(storageRoot, "App_Data", "Uploads");
        Directory.CreateDirectory(uploadsRoot);
        var fileName = anonymized ? $"{videoId}_anonymized.mp4" : $"{videoId}.mp4";
        var path = Path.Combine(uploadsRoot, fileName);
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
