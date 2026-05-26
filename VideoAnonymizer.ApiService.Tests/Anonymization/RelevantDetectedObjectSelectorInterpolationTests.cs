using FluentAssertions;
using NUnit.Framework;
using VideoAnonymizer.Database;
using VideoAnonymizer.VideoProcessor.Anonymization;

namespace VideoAnonymizer.ApiService.Tests.Anonymization;

public sealed class RelevantDetectedObjectSelectorInterpolationTests
{
    [Test]
    public void InterpolatesTrackedBoxCenterAndSizeBetweenAnalyzedFrames()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [0.0] = [CreateObject(trackId: 7, x: 10, y: 20, width: 30, height: 40, blurShape: "rectangle")],
            [1.0] = [CreateObject(trackId: 7, x: 100, y: 60, width: 50, height: 20, blurShape: "rectangle")]
        };

        var result = GetPredictedObjectsAt(timeSeconds: 0.5, analyzedFrames);

        result.Should().ContainSingle();
        result[0].Should().BeEquivalentTo(
            new
            {
                TrackId = 7,
                X = 55,
                Y = 40,
                Width = 40,
                Height = 30,
                BlurShape = "rectangle"
            },
            options => options.ExcludingMissingMembers());
    }

    [Test]
    public void UsesExactAnalyzedBoxAtAnalyzedFrameTime()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [0.0] = [CreateObject(trackId: 7, x: 10)],
            [1.0] = [CreateObject(trackId: 7, x: 100)]
        };

        var result = GetPredictedObjectsAt(timeSeconds: 1.0, analyzedFrames);

        result.Should().ContainSingle();
        result[0].X.Should().Be(100);
    }

    [Test]
    public void HoldsPreviousTrackThroughBufferWhenNoMatchingNextSampleExists()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [0.0] = [CreateObject(trackId: 1, x: 10)],
            [1.0] = [CreateObject(trackId: 2, x: 100)]
        };

        var result = GetPredictedObjectsAt(
            timeSeconds: 1.15,
            analyzedFrames,
            timeBufferSeconds: 0.25);

        result.Select(obj => obj.TrackId).Should().BeEquivalentTo([1, 2]);
    }

    [Test]
    public void UsesUpcomingTrackWithinBufferBeforeFirstSample()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [1.0] = [CreateObject(trackId: 1, x: 100, blurShape: "rectangle")]
        };

        var result = GetPredictedObjectsAt(
            timeSeconds: 0.85,
            analyzedFrames,
            timeBufferSeconds: 0.25);

        result.Should().ContainSingle();
        result[0].Should().BeEquivalentTo(
            new
            {
                TrackId = 1,
                X = 100,
                BlurShape = "rectangle"
            },
            options => options.ExcludingMissingMembers());
    }

    [Test]
    public void DoesNotUseUpcomingTrackOutsideBufferBeforeFirstSample()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [1.0] = [CreateObject(trackId: 1, x: 100)]
        };

        var result = GetPredictedObjectsAt(
            timeSeconds: 0.70,
            analyzedFrames,
            timeBufferSeconds: 0.25);

        result.Should().BeEmpty();
    }

    [Test]
    public void DoesNotUseUpcomingTrackWhenBufferIsDisabled()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [1.0] = [CreateObject(trackId: 1, x: 100)]
        };

        var result = GetPredictedObjectsAt(
            timeSeconds: 0.85,
            analyzedFrames,
            timeBufferSeconds: 0);

        result.Should().BeEmpty();
    }

    [Test]
    public void KeepsUntrackedObjectsOnTheirAnalyzedBoxInsteadOfInterpolating()
    {
        var analyzedFrames = new Dictionary<double, List<DetectedObject>>
        {
            [0.0] = [CreateObject(trackId: null, x: 10)],
            [1.0] = [CreateObject(trackId: null, x: 100)]
        };

        var result = GetPredictedObjectsAt(timeSeconds: 0.5, analyzedFrames);

        result.Should().ContainSingle();
        result[0].X.Should().Be(10);
    }

    private static List<DetectedObject> GetPredictedObjectsAt(
        double timeSeconds,
        Dictionary<double, List<DetectedObject>> analyzedFrames,
        double timeBufferSeconds = 0.0)
    {
        const double fps = 100;
        var frameIndex = (int)Math.Round(timeSeconds * fps);
        return RelevantDetectedObjectSelector.GetPredictedObjectsFromRelevantAnalyzedFrames(
            analyzedFrames,
            frameIndex,
            fps,
            timeBufferSeconds);
    }

    private static DetectedObject CreateObject(
        int? trackId,
        int x,
        int y = 20,
        int width = 30,
        int height = 40,
        string? blurShape = null)
    {
        return new DetectedObject
        {
            Id = Guid.NewGuid(),
            Confidence = 0.9,
            ClassName = trackId is null ? "other" : "license_plate",
            BlurShape = blurShape,
            Selected = true,
            TrackId = trackId,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }
}
