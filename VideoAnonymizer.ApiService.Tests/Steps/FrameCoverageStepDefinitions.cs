using System.Globalization;
using FluentAssertions;
using Reqnroll;
using VideoAnonymizer.Database;
using VideoAnonymizer.VideoProcessor.Anonymization;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class FrameCoverageStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    private List<AnalyzedFrame> AnalyzedFrames
    {
        get => _scenarioContext.Get<List<AnalyzedFrame>>(nameof(AnalyzedFrames));
        set => _scenarioContext.Set(value, nameof(AnalyzedFrames));
    }

    private List<DetectedObject> RelevantObjects
    {
        get => _scenarioContext.Get<List<DetectedObject>>(nameof(RelevantObjects));
        set => _scenarioContext.Set(value, nameof(RelevantObjects));
    }

    public FrameCoverageStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("analyzed detections")]
    public void GivenAnalyzedDetections(Table table)
    {
        var framesByIndex = new Dictionary<int, AnalyzedFrame>();
        var sequentialByTime = new Dictionary<double, int>();
        var nextSequential = 0;

        foreach (var row in table.Rows)
        {
            var timeSeconds = double.Parse(row["timeSeconds"], CultureInfo.InvariantCulture);
            var trackId = int.Parse(row["trackId"], CultureInfo.InvariantCulture);
            var x = int.Parse(row["x"], CultureInfo.InvariantCulture);

            if (!sequentialByTime.TryGetValue(timeSeconds, out _))
            {
                sequentialByTime[timeSeconds] = nextSequential++;
            }

            var frameIndex = sequentialByTime[timeSeconds];

            if (!framesByIndex.TryGetValue(frameIndex, out var frame))
            {
                frame = new AnalyzedFrame
                {
                    Id = Guid.NewGuid(),
                    VideoId = Guid.NewGuid(),
                    FrameIndex = frameIndex,
                    TimeSeconds = timeSeconds,
                    DetectedObjects = []
                };
                framesByIndex[frameIndex] = frame;
            }

            var obj = CreateObject(trackId, x);
            obj.AnalyzedFrame = frame;
            frame.DetectedObjects.Add(obj);
        }

        AnalyzedFrames = framesByIndex.Values.OrderBy(frame => frame.FrameIndex).ToList();
    }

    [When("the processor asks for objects at {double} seconds with a {double} second buffer")]
    public void WhenTheProcessorAsksForObjectsAtSecondsWithBuffer(double currentTimeSeconds, double timeBufferSeconds)
    {
        const double fps = 100;
        var frameIndex = (int)Math.Round(currentTimeSeconds * fps);

        RelevantObjects = RelevantDetectedObjectSelector.GetObjectsForFrame(
            AnalyzedFrames,
            frameIndex,
            fps,
            (int)Math.Round(timeBufferSeconds * 1000),
            interpolateTrackedObjects: false);
    }

    [Then("the relevant objects are")]
    public void ThenTheRelevantObjectsAre(Table table)
    {
        var expected = table.Rows
            .Select(row => new ObjectSnapshot(
                int.Parse(row["trackId"], CultureInfo.InvariantCulture),
                int.Parse(row["x"], CultureInfo.InvariantCulture)))
            .ToList();

        RelevantObjects
            .Select(obj => new ObjectSnapshot(obj.TrackId!.Value, obj.X))
            .Should().BeEquivalentTo(expected);
    }

    [Then(@"^the relevant tracks are (.*)$")]
    public void ThenTheRelevantTracksAre(string trackIds)
    {
        var expected = trackIds
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.Parse(id, CultureInfo.InvariantCulture))
            .ToList();

        RelevantObjects.Select(obj => obj.TrackId).Should().BeEquivalentTo(expected);
    }

    [Then("no relevant objects are returned")]
    public void ThenNoRelevantObjectsAreReturned()
    {
        RelevantObjects.Should().BeEmpty();
    }

    private static DetectedObject CreateObject(int trackId, int x) =>
        new()
        {
            Id = Guid.NewGuid(),
            TrackId = trackId,
            X = x,
            Y = 20,
            Width = 30,
            Height = 40,
            Selected = true
        };

    private sealed record ObjectSnapshot(int TrackId, int X);
}
