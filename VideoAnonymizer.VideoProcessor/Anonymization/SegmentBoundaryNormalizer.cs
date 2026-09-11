using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// Maintains the boundary-storage invariant for consecutive segments:
/// - Only the first occurrence may store PreBufferMsOverride
/// - Only the last occurrence may store PostBufferMsOverride
/// - Only the last occurrence before a real following same-track gap may store
///   NextGapHandlingMode; closing a gap clears it and a newly created gap starts null
///   (default Interpolate).
/// </summary>
public static class SegmentBoundaryNormalizer
{
    public static void Normalize(IReadOnlyList<DetectedObject> occurrences)
    {
        if (occurrences.Count == 0)
            return;

        var first = occurrences[0];
        var last = occurrences[^1];

        foreach (var obj in occurrences)
        {
            if (!ReferenceEquals(obj, first))
                obj.PreBufferMsOverride = null;

            if (!ReferenceEquals(obj, last))
            {
                obj.PostBufferMsOverride = null;
                obj.NextGapHandlingMode = null;
            }
        }
    }

    /// <summary>
    /// Clears NextGapHandlingMode from every occurrence that is not the last of a
    /// consecutive segment that has a real following same-track gap. Call after
    /// rebuildable segment structure changes (merge, split, add, delete).
    /// </summary>
    public static void NormalizeGapModes(IReadOnlyList<AnalyzedFrame> analyzedFrames)
    {
        ArgumentNullException.ThrowIfNull(analyzedFrames);

        var segments = ConsecutiveSegmentResolver.BuildAll(analyzedFrames);
        var lastBeforeGap = new HashSet<Guid>();

        var byTrack = segments
            .Where(segment => segment.First.TrackId is not null)
            .GroupBy(segment => segment.First.TrackId!.Value);

        foreach (var group in byTrack)
        {
            var ordered = group
                .OrderBy(segment => segment.First.AnalyzedFrame.FrameIndex)
                .ToList();

            for (var index = 0; index < ordered.Count - 1; index++)
            {
                lastBeforeGap.Add(ordered[index].Last.Id);
            }
        }

        foreach (var obj in analyzedFrames.SelectMany(frame => frame.DetectedObjects))
        {
            if (!lastBeforeGap.Contains(obj.Id))
                obj.NextGapHandlingMode = null;
        }
    }
}
