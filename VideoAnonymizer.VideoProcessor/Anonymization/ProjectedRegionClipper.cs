using VideoAnonymizer.Database;

namespace VideoAnonymizer.VideoProcessor.Anonymization;

/// <summary>
/// Pure frame-intersection helpers for projected anonymization regions. Projection keeps
/// raw unbounded geometry; export rasterization and optional presence checks use these
/// helpers on a copy. Clipped geometry is never fed back into motion.
/// </summary>
public static class ProjectedRegionClipper
{
    /// <summary>
    /// Returns a new object holding only the in-frame intersection, or null when the
    /// raw region is fully outside. Does not mutate <paramref name="obj"/>.
    /// </summary>
    public static DetectedObject? ClipToFrame(DetectedObject obj, int videoWidth, int videoHeight)
    {
        ArgumentNullException.ThrowIfNull(obj);

        // Unknown dimensions: leave the projected box unclipped rather than collapsing it.
        if (videoWidth <= 0 || videoHeight <= 0)
            return CopyGeometry(obj);

        var left = Math.Max(0, obj.X);
        var top = Math.Max(0, obj.Y);
        var right = Math.Min(videoWidth, obj.X + obj.Width);
        var bottom = Math.Min(videoHeight, obj.Y + obj.Height);

        if (right <= left || bottom <= top)
            return null;

        var clipped = CopyGeometry(obj);
        clipped.X = left;
        clipped.Y = top;
        clipped.Width = right - left;
        clipped.Height = bottom - top;
        return clipped;
    }

    /// <summary>
    /// True when the raw region has no intersection with the frame. Unknown dimensions
    /// never count as fully outside.
    /// </summary>
    public static bool IsFullyOutside(DetectedObject obj, int videoWidth, int videoHeight)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (videoWidth <= 0 || videoHeight <= 0)
            return false;

        var left = Math.Max(0, obj.X);
        var top = Math.Max(0, obj.Y);
        var right = Math.Min(videoWidth, obj.X + obj.Width);
        var bottom = Math.Min(videoHeight, obj.Y + obj.Height);
        return right <= left || bottom <= top;
    }

    private static DetectedObject CopyGeometry(DetectedObject source) => new()
    {
        Id = source.Id,
        Confidence = source.Confidence,
        ClassName = source.ClassName,
        BlurShape = source.BlurShape,
        BlurSizePercentOverride = source.BlurSizePercentOverride,
        OccurrenceBlurSizePercentOverride = source.OccurrenceBlurSizePercentOverride,
        PreBufferMsOverride = source.PreBufferMsOverride,
        PostBufferMsOverride = source.PostBufferMsOverride,
        NextGapHandlingMode = source.NextGapHandlingMode,
        Selected = source.Selected,
        TrackId = source.TrackId,
        X = source.X,
        Y = source.Y,
        Width = source.Width,
        Height = source.Height,
        AnalyzedFrameId = source.AnalyzedFrameId
    };
}
