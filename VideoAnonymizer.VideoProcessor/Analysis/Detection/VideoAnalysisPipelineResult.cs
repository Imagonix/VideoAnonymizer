namespace VideoAnonymizer.VideoProcessor.Analysis.Detection;

internal sealed record VideoAnalysisPipelineResult(
    int SavedFrameCount,
    int LastReportedProgress);
