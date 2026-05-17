using FluentAssertions;
using Reqnroll;
using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class AppearanceObjectTrackingStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    private AppearanceObjectTracker Tracker
    {
        get => _scenarioContext.Get<AppearanceObjectTracker>(nameof(Tracker));
        set => _scenarioContext.Set(value, nameof(Tracker));
    }

    private Dictionary<string, int> TrackIdsByFaceName
    {
        get => _scenarioContext.Get<Dictionary<string, int>>(nameof(TrackIdsByFaceName));
        set => _scenarioContext.Set(value, nameof(TrackIdsByFaceName));
    }

    public AppearanceObjectTrackingStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("an appearance tracker with default matching settings")]
    public void GivenAnAppearanceTrackerWithDefaultMatchingSettings()
    {
        Tracker = new AppearanceObjectTracker(CreateOptions());
        TrackIdsByFaceName = [];
    }

    [Given("the tracker has already seen {string} as {string}")]
    public void GivenTheTrackerHasAlreadySeenFace(string faceName, string appearanceName)
    {
        var detection = CreateDetection(faceName, appearanceName, x: 10, y: 20);
        AssignFrame(frameIndex: 0, timeSeconds: 0.0, [detection]);
    }

    [When("the next frame contains {string} as {string}")]
    public void WhenTheNextFrameContainsFace(string faceName, string appearanceName)
    {
        var detection = CreateDetection(faceName, appearanceName, x: 12, y: 21);
        AssignFrame(frameIndex: 10, timeSeconds: 0.5, [detection]);
    }

    [When("the next frame contains these faces")]
    public void WhenTheNextFrameContainsTheseFaces(Table table)
    {
        var detections = table.Rows
            .Select((row, index) => CreateDetection(
                row["name"],
                row["appearance"],
                x: 12 + index * 6,
                y: 21 + index))
            .ToList();

        AssignFrame(frameIndex: 10, timeSeconds: 0.5, detections);
    }

    [Then("{string} has the same track id as {string}")]
    public void ThenFaceHasTheSameTrackIdAs(string actualFaceName, string expectedFaceName)
    {
        TrackIdsByFaceName[actualFaceName].Should().Be(TrackIdsByFaceName[expectedFaceName]);
    }

    [Then("{string} has a different track id than {string}")]
    public void ThenFaceHasADifferentTrackIdThan(string actualFaceName, string expectedFaceName)
    {
        TrackIdsByFaceName[actualFaceName].Should().NotBe(TrackIdsByFaceName[expectedFaceName]);
    }

    [Then("exactly one of these faces has the same track id as {string}")]
    public void ThenExactlyOneOfTheseFacesHasTheSameTrackIdAs(string expectedFaceName, Table table)
    {
        var expectedTrackId = TrackIdsByFaceName[expectedFaceName];
        table.Rows
            .Select(row => TrackIdsByFaceName[row["name"]])
            .Count(trackId => trackId == expectedTrackId)
            .Should().Be(1);
    }

    [Then("these faces have different track ids")]
    public void ThenTheseFacesHaveDifferentTrackIds(Table table)
    {
        table.Rows
            .Select(row => TrackIdsByFaceName[row["name"]])
            .Should().OnlyHaveUniqueItems();
    }

    private void AssignFrame(
        int frameIndex,
        double timeSeconds,
        IReadOnlyList<NamedAppearanceDetection> detections)
    {
        var assignments = Tracker.AssignTracks(
            frameIndex,
            timeSeconds,
            detections.Select(detection => detection.Detection).ToList());

        foreach (var detection in detections)
            TrackIdsByFaceName[detection.FaceName] = assignments[detection.Detection.DetectedObjectId];
    }

    private static NamedAppearanceDetection CreateDetection(
        string faceName,
        string appearanceName,
        int x,
        int y)
    {
        return new NamedAppearanceDetection(
            faceName,
            new AppearanceDetection(
                Guid.NewGuid(),
                "face",
                0.90,
                new TrackBox(x, y, 40, 50),
                CreateFeature(appearanceName)));
    }

    private static AppearanceFeature CreateFeature(string appearanceName)
    {
        return appearanceName.Trim().ToLowerInvariant() switch
        {
            "person a" => new AppearanceFeature([1.0, 0.0], 0UL, [1.0, 0.0]),
            "person b" => new AppearanceFeature([0.0, 1.0], 0x7FFFFFFFFFFFFFFFUL, [-1.0, 0.0]),
            _ => throw new ArgumentOutOfRangeException(nameof(appearanceName), appearanceName, "Unknown appearance.")
        };
    }

    private static AppearanceObjectTrackingOptions CreateOptions() =>
        new(
            MaxSamplesPerTrack: 5,
            MaxTrackGapSeconds: 5,
            MinAppearanceSimilarity: 0.62,
            MinAssignmentScore: 0.68,
            MinSpatialSimilarity: 0.05,
            MinSizeSimilarity: 0.35,
            AppearanceScoreWeight: 0.65,
            SpatialScoreWeight: 0.20,
            SizeScoreWeight: 0.10,
            RecencyScoreWeight: 0.05,
            MaxCenterDistanceBoxDiagonals: 6,
            CropPaddingPercent: 0.15);

    private sealed record NamedAppearanceDetection(
        string FaceName,
        AppearanceDetection Detection);
}
