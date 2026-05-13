using FluentAssertions;
using NUnit.Framework;
using VideoAnonymizer.VideoProcessor.Analysis;

namespace VideoAnonymizer.ApiService.Tests.Analysis;

public sealed class AppearanceObjectTrackerTests
{
    [Test]
    public void AssignTracks_reuses_track_for_similar_appearance()
    {
        var tracker = new AppearanceObjectTracker(CreateOptions());
        var first = CreateDetection(FeatureA, frameOffset: 0);
        var second = CreateDetection(FeatureA, frameOffset: 1, x: 12);

        var firstAssignments = tracker.AssignTracks(0, 0.0, [first]);
        var secondAssignments = tracker.AssignTracks(10, 0.5, [second]);

        secondAssignments[second.DetectedObjectId].Should().Be(firstAssignments[first.DetectedObjectId]);
    }

    [Test]
    public void AssignTracks_creates_new_track_for_different_appearance()
    {
        var tracker = new AppearanceObjectTracker(CreateOptions());
        var first = CreateDetection(FeatureA, frameOffset: 0);
        var second = CreateDetection(FeatureB, frameOffset: 1, x: 12);

        var firstAssignments = tracker.AssignTracks(0, 0.0, [first]);
        var secondAssignments = tracker.AssignTracks(10, 0.5, [second]);

        secondAssignments[second.DetectedObjectId].Should().NotBe(firstAssignments[first.DetectedObjectId]);
    }

    [Test]
    public void AssignTracks_assigns_each_track_to_at_most_one_detection_per_frame()
    {
        var tracker = new AppearanceObjectTracker(CreateOptions());
        var first = CreateDetection(FeatureA, frameOffset: 0);
        var firstAssignments = tracker.AssignTracks(0, 0.0, [first]);
        var secondFrameDetections = new[]
        {
            CreateDetection(FeatureA, frameOffset: 1, x: 12),
            CreateDetection(FeatureA, frameOffset: 2, x: 18)
        };

        var secondAssignments = tracker.AssignTracks(10, 0.5, secondFrameDetections);

        secondAssignments.Values.Should().OnlyHaveUniqueItems();
        secondAssignments.Values.Should().Contain(firstAssignments[first.DetectedObjectId]);
    }

    private static AppearanceDetection CreateDetection(
        AppearanceFeature feature,
        int frameOffset,
        int x = 10)
    {
        return new AppearanceDetection(
            Guid.NewGuid(),
            "face",
            0.90,
            new TrackBox(x, 20 + frameOffset, 40, 50),
            feature);
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

    private static AppearanceFeature FeatureA { get; } =
        new([1.0, 0.0], 0UL, [1.0, 0.0]);

    private static AppearanceFeature FeatureB { get; } =
        new([0.0, 1.0], 0x7FFFFFFFFFFFFFFFUL, [-1.0, 0.0]);
}
