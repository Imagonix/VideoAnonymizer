using System.Globalization;
using FluentAssertions;
using Reqnroll;
using VideoAnonymizer.Database;
using VideoAnonymizer.VideoProcessor.Anonymization;

namespace VideoAnonymizer.ApiService.Tests.Steps;

[Binding]
public sealed class RelevantDetectedObjectInterpolationStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    private List<AnalyzedFrame> AnalyzedFrames
    {
        get => _scenarioContext.Get<List<AnalyzedFrame>>(nameof(AnalyzedFrames));
        set => _scenarioContext.Set(value, nameof(AnalyzedFrames));
    }

    private List<DetectedObject> PredictedObjects
    {
        get => _scenarioContext.Get<List<DetectedObject>>(nameof(PredictedObjects));
        set => _scenarioContext.Set(value, nameof(PredictedObjects));
    }

    private bool InterpolateTrackedObjects
    {
        get => _scenarioContext.Get<bool>(nameof(InterpolateTrackedObjects));
        set => _scenarioContext.Set(value, nameof(InterpolateTrackedObjects));
    }

    private int VideoWidth
    {
        get => _scenarioContext.ContainsKey(nameof(VideoWidth))
            ? _scenarioContext.Get<int>(nameof(VideoWidth))
            : 0;
        set => _scenarioContext.Set(value, nameof(VideoWidth));
    }

    private int VideoHeight
    {
        get => _scenarioContext.ContainsKey(nameof(VideoHeight))
            ? _scenarioContext.Get<int>(nameof(VideoHeight))
            : 0;
        set => _scenarioContext.Set(value, nameof(VideoHeight));
    }

    public RelevantDetectedObjectInterpolationStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("analyzed detections for prediction")]
    public void GivenAnalyzedDetectionsForPrediction(Table table)
    {
        AnalyzedFrames = BuildFrames(table);
        InterpolateTrackedObjects = true;
        VideoWidth = 0;
        VideoHeight = 0;
    }

    [Given("object interpolation is disabled")]
    public void GivenObjectInterpolationIsDisabled()
    {
        InterpolateTrackedObjects = false;
    }

    [Given("the video frame size is {int} by {int}")]
    public void GivenTheVideoFrameSizeIs(int width, int height)
    {
        VideoWidth = width;
        VideoHeight = height;
    }

    [Given("an empty analyzed frame at {double} seconds with frame index {int}")]
    public void GivenAnEmptyAnalyzedFrameAtSecondsWithFrameIndex(double timeSeconds, int frameIndex)
    {
        var frames = AnalyzedFrames;
        if (frames.Any(frame => frame.FrameIndex == frameIndex))
            return;

        frames.Add(new AnalyzedFrame
        {
            Id = Guid.NewGuid(),
            VideoId = frames.FirstOrDefault()?.VideoId ?? Guid.NewGuid(),
            FrameIndex = frameIndex,
            TimeSeconds = timeSeconds,
            DetectedObjects = []
        });
        AnalyzedFrames = frames.OrderBy(frame => frame.FrameIndex).ToList();
    }

    [When("the processor predicts objects at {double} seconds with a {double} second buffer")]
    public void WhenTheProcessorPredictsObjectsAtSecondsWithBuffer(
        double currentTimeSeconds,
        double timeBufferSeconds)
    {
        const double fps = 100;
        var frameIndex = (int)Math.Round(currentTimeSeconds * fps);

        PredictedObjects = RelevantDetectedObjectSelector.GetObjectsForFrame(
            AnalyzedFrames,
            frameIndex,
            fps,
            (int)Math.Round(timeBufferSeconds * 1000),
            InterpolateTrackedObjects,
            VideoWidth,
            VideoHeight);
    }

    [Then("the predicted objects are")]
    public void ThenThePredictedObjectsAre(Table table)
    {
        PredictedObjects.Should().HaveCount(table.Rows.Count);

        for (var index = 0; index < table.Rows.Count; index++)
        {
            AssertObjectMatchesRow(PredictedObjects[index], table.Rows[index]);
        }
    }

    [Then(@"^the predicted tracks are (.*)$")]
    public void ThenThePredictedTracksAre(string trackIds)
    {
        var expected = trackIds
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.Parse(id, CultureInfo.InvariantCulture))
            .ToList();

        PredictedObjects.Select(obj => obj.TrackId).Should().BeEquivalentTo(expected);
    }

    [Then("no predicted objects are returned")]
    public void ThenNoPredictedObjectsAreReturned()
    {
        PredictedObjects.Should().BeEmpty();
    }

    [Then("exactly one predicted region is returned for track {int}")]
    public void ThenExactlyOnePredictedRegionIsReturnedForTrack(int trackId)
    {
        PredictedObjects.Where(obj => obj.TrackId == trackId).Should().HaveCount(1);
    }

    [Then("exactly two predicted regions are returned for track {int}")]
    public void ThenExactlyTwoPredictedRegionsAreReturnedForTrack(int trackId)
    {
        PredictedObjects.Where(obj => obj.TrackId == trackId).Should().HaveCount(2);
    }

    private static List<AnalyzedFrame> BuildFrames(Table table)
    {
        var framesByIndex = new Dictionary<int, AnalyzedFrame>();
        var sequentialByTime = new Dictionary<double, int>();
        var nextSequential = 0;

        foreach (var row in table.Rows)
        {
            var timeSeconds = ParseRequiredDouble(row, "timeSeconds");

            if (!sequentialByTime.TryGetValue(timeSeconds, out _))
            {
                sequentialByTime[timeSeconds] = nextSequential++;
            }

            var frameIndex = GetOptional(row, "frameIndex") is { } rawIndex
                ? int.Parse(rawIndex, CultureInfo.InvariantCulture)
                : sequentialByTime[timeSeconds];

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

            var obj = CreateObject(
                ParseOptionalInt(row, "trackId"),
                ParseRequiredInt(row, "x"),
                ParseOptionalInt(row, "y") ?? 20,
                ParseOptionalInt(row, "width") ?? 30,
                ParseOptionalInt(row, "height") ?? 40,
                GetOptional(row, "blurShape"),
                ParseOptionalInt(row, "blurSizePercentOverride"),
                ParseOptionalInt(row, "occurrenceBlurSizePercentOverride"),
                ParseOptionalInt(row, "preOverrideMs"),
                ParseOptionalInt(row, "postOverrideMs"),
                ParseOptionalGapHandlingMode(row, "nextGapHandlingMode"),
                ParseOptionalBool(row, "selected") ?? true);
            obj.AnalyzedFrame = frame;
            frame.DetectedObjects.Add(obj);
        }

        return framesByIndex.Values.OrderBy(frame => frame.FrameIndex).ToList();
    }

    private static DetectedObject CreateObject(
        int? trackId,
        int x,
        int y,
        int width,
        int height,
        string? blurShape,
        int? blurSizePercentOverride,
        int? occurrenceBlurSizePercentOverride,
        int? preOverrideMs,
        int? postOverrideMs,
        GapHandlingMode? nextGapHandlingMode = null,
        bool selected = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            Confidence = 0.9,
            ClassName = trackId is null ? "other" : "license_plate",
            BlurShape = blurShape,
            BlurSizePercentOverride = blurSizePercentOverride,
            OccurrenceBlurSizePercentOverride = occurrenceBlurSizePercentOverride,
            PreBufferMsOverride = preOverrideMs,
            PostBufferMsOverride = postOverrideMs,
            NextGapHandlingMode = nextGapHandlingMode,
            Selected = selected,
            TrackId = trackId,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };

    private static void AssertObjectMatchesRow(DetectedObject actual, DataTableRow row)
    {
        if (HasColumn(row, "trackId"))
        {
            actual.TrackId.Should().Be(ParseOptionalInt(row, "trackId"));
        }

        AssertOptionalInt(row, "x", actual.X);
        AssertOptionalInt(row, "y", actual.Y);
        AssertOptionalInt(row, "width", actual.Width);
        AssertOptionalInt(row, "height", actual.Height);

        if (HasColumn(row, "blurShape"))
        {
            actual.BlurShape.Should().Be(GetOptional(row, "blurShape"));
        }

        AssertOptionalNullableInt(row, "blurSizePercentOverride", actual.BlurSizePercentOverride);
        AssertOptionalNullableInt(row, "occurrenceBlurSizePercentOverride", actual.OccurrenceBlurSizePercentOverride);
        AssertOptionalNullableInt(row, "preOverrideMs", actual.PreBufferMsOverride);
        AssertOptionalNullableInt(row, "postOverrideMs", actual.PostBufferMsOverride);

        if (HasColumn(row, "nextGapHandlingMode"))
        {
            actual.NextGapHandlingMode.Should().Be(ParseOptionalGapHandlingMode(row, "nextGapHandlingMode"));
        }
    }

    private static GapHandlingMode? ParseOptionalGapHandlingMode(DataTableRow row, string column)
    {
        var value = GetOptional(row, column);
        if (value is null)
            return null;

        if (value == nameof(GapHandlingMode.Interpolate))
            return GapHandlingMode.Interpolate;

        if (value == nameof(GapHandlingMode.UseBuffers))
            return GapHandlingMode.UseBuffers;

        throw new ArgumentException(
            $"Unsupported gap handling mode in test table: '{value}'.",
            column);
    }

    private static bool? ParseOptionalBool(DataTableRow row, string column)
    {
        var value = GetOptional(row, column);
        if (value is null) return null;
        return bool.Parse(value);
    }

    private static void AssertOptionalInt(DataTableRow row, string column, int actual)
    {
        if (HasColumn(row, column))
        {
            actual.Should().Be(ParseRequiredInt(row, column));
        }
    }

    private static void AssertOptionalNullableInt(DataTableRow row, string column, int? actual)
    {
        if (HasColumn(row, column))
        {
            actual.Should().Be(ParseOptionalInt(row, column));
        }
    }

    private static int ParseRequiredInt(DataTableRow row, string column) =>
        int.Parse(row[column], CultureInfo.InvariantCulture);

    private static double ParseRequiredDouble(DataTableRow row, string column) =>
        double.Parse(row[column], CultureInfo.InvariantCulture);

    private static int? ParseOptionalInt(DataTableRow row, string column)
    {
        var value = GetOptional(row, column);
        return value is null ? null : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string? GetOptional(DataTableRow row, string column)
    {
        return row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static bool HasColumn(DataTableRow row, string column) => row.ContainsKey(column);
}
