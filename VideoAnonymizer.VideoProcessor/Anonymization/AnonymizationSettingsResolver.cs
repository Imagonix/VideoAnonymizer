using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

public static class AnonymizationSettingsResolver
{
    public static (int PreBufferMs, int PostBufferMs) ResolveBuffers(
        ConsecutiveSegment segment,
        int globalTimeBufferMs) =>
        (segment.First.PreBufferMsOverride ?? globalTimeBufferMs,
            segment.Last.PostBufferMsOverride ?? globalTimeBufferMs);

    public static int ResolveBlurSize(DetectedObject occurrence, int globalBlurSizePercent) =>
        occurrence.OccurrenceBlurSizePercentOverride
        ?? occurrence.BlurSizePercentOverride
        ?? globalBlurSizePercent;

    public static GapHandlingMode ResolveGapHandlingMode(GapHandlingMode? stored) =>
        stored ?? GapHandlingMode.Interpolate;
}
