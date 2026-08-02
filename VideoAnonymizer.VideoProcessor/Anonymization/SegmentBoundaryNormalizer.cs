using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// Maintains the boundary-storage invariant for a consecutive segment: only its first
/// occurrence may store PreBufferMsOverride and only its last may store PostBufferMsOverride.
/// When runs are joined into one segment, now-interior boundary overrides are cleared.
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
                obj.PostBufferMsOverride = null;
        }
    }
}
