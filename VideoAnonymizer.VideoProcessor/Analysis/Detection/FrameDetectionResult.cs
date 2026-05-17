using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.Analysis.Detection;

internal sealed record FrameDetectionResult(
    int FrameIndex,
    double TimeSeconds,
    IReadOnlyList<DetectionResult> Detections);
