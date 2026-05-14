namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed record ObjectTrackingPipelineResult(
    int TrackedFrameCount,
    int LastReportedProgress);
