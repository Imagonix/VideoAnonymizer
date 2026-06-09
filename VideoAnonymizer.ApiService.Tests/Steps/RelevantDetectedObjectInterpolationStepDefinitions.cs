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

    private Dictionary<double, List<DetectedObject>> AnalyzedFrames
    {
        get => _scenarioContext.Get<Dictionary<double, List<DetectedObject>>>(nameof(AnalyzedFrames));
        set => _scenarioContext.Set(value, nameof(AnalyzedFrames));
    }

    private List<DetectedObject> PredictedObjects
    {
        get => _scenarioContext.Get<List<DetectedObject>>(nameof(PredictedObjects));
        set => _scenarioContext.Set(value, nameof(PredictedObjects));
    }

    public RelevantDetectedObjectInterpolationStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("analyzed detections for prediction")]
    public void GivenAnalyzedDetectionsForPrediction(Table table)
    {
        AnalyzedFrames = [];

        foreach (var row in table.Rows)
        {
            var timeSeconds = ParseRequiredDouble(row, "timeSeconds");

            if (!AnalyzedFrames.TryGetValue(timeSeconds, out var objects))
            {
                objects = [];
                AnalyzedFrames[timeSeconds] = objects;
            }

            objects.Add(CreateObject(
                ParseOptionalInt(row, "trackId"),
                ParseRequiredInt(row, "x"),
                ParseOptionalInt(row, "y") ?? 20,
                ParseOptionalInt(row, "width") ?? 30,
                ParseOptionalInt(row, "height") ?? 40,
                GetOptional(row, "blurShape")));
        }
    }

    [When("the processor predicts objects at {double} seconds with a {double} second buffer")]
    public void WhenTheProcessorPredictsObjectsAtSecondsWithBuffer(
        double currentTimeSeconds,
        double timeBufferSeconds)
    {
        const double fps = 100;
        var frameIndex = (int)Math.Round(currentTimeSeconds * fps);

        PredictedObjects = RelevantDetectedObjectSelector.GetPredictedObjectsFromRelevantAnalyzedFrames(
            AnalyzedFrames,
            frameIndex,
            fps,
            timeBufferSeconds);
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

    private static DetectedObject CreateObject(
        int? trackId,
        int x,
        int y,
        int width,
        int height,
        string? blurShape) =>
        new()
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
    }

    private static void AssertOptionalInt(DataTableRow row, string column, int actual)
    {
        if (HasColumn(row, column))
        {
            actual.Should().Be(ParseRequiredInt(row, column));
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
