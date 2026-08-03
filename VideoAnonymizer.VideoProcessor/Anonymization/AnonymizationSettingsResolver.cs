using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// Cohesive resolution of effective anonymization settings. Fallback rules live in one
/// place so the processor, selector and boundary transfers never duplicate them.
/// </summary>
public static class AnonymizationSettingsResolver
{
    public static (int PreBufferMs, int PostBufferMs) ResolveBuffers(
        ConsecutiveSegment segment,
        int globalTimeBufferMs)
    {
        var trackTimeBufferMs = segment.First.TrackTimeBufferMsOverride ?? globalTimeBufferMs;
        return (segment.First.PreBufferMsOverride ?? trackTimeBufferMs,
            segment.Last.PostBufferMsOverride ?? trackTimeBufferMs);
    }

    /// <summary>
    /// Effective blur size for one occurrence: occurrence override -> materialized
    /// track-wide override -> global default.
    /// </summary>
    public static int ResolveBlurSize(DetectedObject occurrence, int globalBlurSizePercent) =>
        occurrence.OccurrenceBlurSizePercentOverride
        ?? occurrence.BlurSizePercentOverride
        ?? globalBlurSizePercent;
}
