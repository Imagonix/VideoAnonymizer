namespace VideoAnonymizer.VideoProcessor.Analysis.Detection;

internal sealed record FrameDetectionJob(
    int FrameIndex,
    double TimeSeconds,
    string ImageBase64);
