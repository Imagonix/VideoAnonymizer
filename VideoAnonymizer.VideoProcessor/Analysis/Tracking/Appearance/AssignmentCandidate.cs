namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

internal sealed record AssignmentCandidate(
    int DetectionIndex,
    AppearanceTrack Track,
    double Score);
