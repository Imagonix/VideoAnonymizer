using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// The maximal run of occurrences that share the same TrackId in adjacent entries of
/// the complete analyzed-frame sequence. An untracked occurrence is a one-object
/// segment and is both its first and last boundary. Occurrence inclusion/exclusion
/// does not affect segment identity.
/// </summary>
public sealed record ConsecutiveSegment(IReadOnlyList<DetectedObject> Occurrences)
{
    public DetectedObject First => Occurrences[0];
    public DetectedObject Last => Occurrences[^1];
}

/// <summary>
/// Resolves consecutive segments from the complete analyzed-frame sequence ordered by
/// FrameIndex. FrameIndex is only the ordering key — adjacency means successive entries
/// in that ordered list, not a numeric FrameIndex difference of 1. A missing occurrence
/// of the track in any intervening analyzed frame ends the segment; the Selected flag
/// never changes segment identity.
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

        // Untracked occurrences are always one-object segments.
        foreach (var obj in orderedFrames
            .SelectMany(frame => frame.DetectedObjects)
            .Where(obj => obj.TrackId is null))
        {
            segments.Add(new ConsecutiveSegment([obj]));
        }

        var trackIds = orderedFrames
            .SelectMany(frame => frame.DetectedObjects)
            .Select(obj => obj.TrackId)
            .Where(trackId => trackId is not null)
            .Distinct()
            .ToList();

        foreach (var trackId in trackIds)
        {
            // Walk every analyzed frame in order. Continue the run when this frame has
            // the track; an intervening analyzed frame without the track breaks it.
            var currentRun = new List<DetectedObject>();
            foreach (var frame in orderedFrames)
            {
                var match = frame.DetectedObjects.FirstOrDefault(obj => obj.TrackId == trackId);
                if (match is not null)
                {
                    currentRun.Add(match);
                    continue;
                }

                if (currentRun.Count > 0)
                {
                    segments.Add(new ConsecutiveSegment(currentRun));
                    currentRun = [];
                }
            }

            if (currentRun.Count > 0)
                segments.Add(new ConsecutiveSegment(currentRun));
        }

        return segments;
    }
}
