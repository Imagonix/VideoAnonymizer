using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal static class TrackingConflictDetector
{
    private const double IntersectionOverUnionThreshold = 0.30;

    public static bool HasDifferentTrackConflict(
        AnalyzedFrame frame,
        TrackForwardPythonDetectionResult detection,
        int trackId) =>
        frame.DetectedObjects.Any(existing =>
            existing.TrackId != trackId
            && CalculateIntersectionOverUnion(existing, detection) >= IntersectionOverUnionThreshold);

    private static double CalculateIntersectionOverUnion(
        DetectedObject existing,
        TrackForwardPythonDetectionResult detection)
    {
        var x1 = Math.Max(existing.X, detection.X);
        var y1 = Math.Max(existing.Y, detection.Y);
        var x2 = Math.Min(existing.X + existing.Width, detection.X + detection.Width);
        var y2 = Math.Min(existing.Y + existing.Height, detection.Y + detection.Height);

        var intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var existingArea = Math.Max(0, existing.Width) * Math.Max(0, existing.Height);
        var detectionArea = Math.Max(0, detection.Width) * Math.Max(0, detection.Height);
        var union = existingArea + detectionArea - intersection;

        return union <= 0 ? 0 : (double)intersection / union;
    }
}
