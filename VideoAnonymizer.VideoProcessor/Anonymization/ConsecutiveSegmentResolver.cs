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
/// Resolves the consecutive segment containing a given occurrence from the complete
/// analyzed-frame sequence ordered by FrameIndex. A missing occurrence in any
/// analyzed frame ends the segment; no time thresholds are applied.
/// </summary>
public static class ConsecutiveSegmentResolver
{
    public static ConsecutiveSegment Find(IReadOnlyList<AnalyzedFrame> analyzedFrames, DetectedObject occurrence)
    {
        ArgumentNullException.ThrowIfNull(analyzedFrames);
        ArgumentNullException.ThrowIfNull(occurrence);

        if (occurrence.TrackId is null)
            return new ConsecutiveSegment([occurrence]);

        var trackId = occurrence.TrackId.Value;
        var orderedFrames = analyzedFrames.OrderBy(frame => frame.FrameIndex).ToList();

        var trackOccurrences = new List<(int FrameIndex, DetectedObject Object)>();
        foreach (var frame in orderedFrames)
        {
            var match = frame.DetectedObjects.FirstOrDefault(obj => obj.TrackId == trackId);
            if (match is not null)
                trackOccurrences.Add((frame.FrameIndex, match));
        }

        var anchorIndex = trackOccurrences.FindIndex(item => item.Object.Id == occurrence.Id);
        if (anchorIndex < 0)
            throw new ArgumentException("The occurrence was not found in the provided analyzed frames.", nameof(occurrence));

        var start = anchorIndex;
        while (start > 0 && trackOccurrences[start - 1].FrameIndex == trackOccurrences[start].FrameIndex - 1)
            start--;

        var end = anchorIndex;
        while (end < trackOccurrences.Count - 1 && trackOccurrences[end + 1].FrameIndex == trackOccurrences[end].FrameIndex + 1)
            end++;

        return new ConsecutiveSegment(trackOccurrences[start..(end + 1)].Select(item => item.Object).ToList());
    }
}
