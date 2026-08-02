using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// The maximal run of occurrences that share the same TrackId in adjacent analyzed
/// frames. An untracked occurrence is a one-object segment and is both its first
/// and last boundary. Occurrence inclusion/exclusion does not affect segment identity.
/// </summary>
public sealed record ConsecutiveSegment(IReadOnlyList<DetectedObject> Occurrences)
{
    public DetectedObject First => Occurrences[0];
    public DetectedObject Last => Occurrences[^1];
}

/// <summary>
/// Resolves consecutive segments from the complete analyzed-frame sequence ordered by
/// FrameIndex. A missing occurrence in any analyzed frame ends the segment; no time
/// thresholds are applied and the Selected flag never changes segment identity.
/// </summary>
public static class ConsecutiveSegmentResolver
{
    public static ConsecutiveSegment Find(IReadOnlyList<AnalyzedFrame> analyzedFrames, DetectedObject occurrence)
    {
        ArgumentNullException.ThrowIfNull(analyzedFrames);
        ArgumentNullException.ThrowIfNull(occurrence);

        var segment = BuildAll(analyzedFrames)
            .FirstOrDefault(candidate => candidate.Occurrences.Any(obj => obj.Id == occurrence.Id));
        if (segment is null)
            throw new ArgumentException("The occurrence was not found in the provided analyzed frames.", nameof(occurrence));

        return segment;
    }

    public static List<ConsecutiveSegment> BuildAll(IReadOnlyList<AnalyzedFrame> analyzedFrames)
    {
        ArgumentNullException.ThrowIfNull(analyzedFrames);

        var orderedFrames = analyzedFrames.OrderBy(frame => frame.FrameIndex).ToList();
        var segments = new List<ConsecutiveSegment>();

        var trackIds = orderedFrames
            .SelectMany(frame => frame.DetectedObjects)
            .Select(obj => obj.TrackId)
            .Distinct()
            .ToList();

        foreach (var trackId in trackIds)
        {
            if (trackId is null)
            {
                foreach (var obj in orderedFrames
                    .SelectMany(frame => frame.DetectedObjects)
                    .Where(obj => obj.TrackId is null))
                {
                    segments.Add(new ConsecutiveSegment([obj]));
                }

                continue;
            }

            var occurrences = new List<(int FrameIndex, DetectedObject Object)>();
            foreach (var frame in orderedFrames)
            {
                var match = frame.DetectedObjects.FirstOrDefault(obj => obj.TrackId == trackId);
                if (match is not null)
                    occurrences.Add((frame.FrameIndex, match));
            }

            var runStart = 0;
            for (var index = 1; index <= occurrences.Count; index++)
            {
                if (index < occurrences.Count
                    && occurrences[index].FrameIndex == occurrences[index - 1].FrameIndex + 1)
                {
                    continue;
                }

                segments.Add(new ConsecutiveSegment(
                    occurrences[runStart..index].Select(item => item.Object).ToList()));
                runStart = index;
            }
        }

        return segments;
    }
}
