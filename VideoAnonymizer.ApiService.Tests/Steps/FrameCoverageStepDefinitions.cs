using System.Globalization;
using System.Reflection;
using FluentAssertions;
using Reqnroll;
using VideoAnonymizer.Database;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class FrameCoverageStepDefinitions
{
    private Dictionary<double, List<DetectedObject>> _analyzedFrames = [];
    private List<DetectedObject> _relevantObjects = [];

    [Given("analyzed detections")]
    public void GivenAnalyzedDetections(Table table)
    {
        _analyzedFrames = [];

        foreach (var row in table.Rows)
        {
            var timeSeconds = double.Parse(row["timeSeconds"], CultureInfo.InvariantCulture);
            var trackId = int.Parse(row["trackId"], CultureInfo.InvariantCulture);
            var x = int.Parse(row["x"], CultureInfo.InvariantCulture);

            if (!_analyzedFrames.TryGetValue(timeSeconds, out var objects))
            {
                objects = [];
                _analyzedFrames[timeSeconds] = objects;
            }

            objects.Add(CreateObject(trackId, x));
        }
    }

    [When("the processor asks for objects at {double} seconds with a {double} second buffer")]
    public void WhenTheProcessorAsksForObjectsAtSecondsWithBuffer(double currentTimeSeconds, double timeBufferSeconds)
    {
        _relevantObjects = GetObjects(_analyzedFrames, currentTimeSeconds, timeBufferSeconds);
    }

    [Then("the relevant objects are")]
    public void ThenTheRelevantObjectsAre(Table table)
    {
        var expected = table.Rows
            .Select(row => new ObjectSnapshot(
                int.Parse(row["trackId"], CultureInfo.InvariantCulture),
                int.Parse(row["x"], CultureInfo.InvariantCulture)))
            .ToList();

        _relevantObjects
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

        _relevantObjects.Select(obj => obj.TrackId).Should().BeEquivalentTo(expected);
    }

    [Then("no relevant objects are returned")]
    public void ThenNoRelevantObjectsAreReturned()
    {
        _relevantObjects.Should().BeEmpty();
    }

    private static List<DetectedObject> GetObjects(
        Dictionary<double, List<DetectedObject>> analyzedFrames,
        double currentTimeSeconds,
        double timeBufferSeconds)
    {
        const double fps = 100;
        var frameIndex = (int)Math.Round(currentTimeSeconds * fps);

        // TODO Make method public and call directly
        var method = typeof(VideoAnonymizer.VideoProcessor.VideoAnonymizer)
            .GetMethod("GetObjectsFromRelevantAnalyzedFrames", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        return ((List<DetectedObject>)method!.Invoke(null, [analyzedFrames, frameIndex, fps, timeBufferSeconds])!)!;
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
