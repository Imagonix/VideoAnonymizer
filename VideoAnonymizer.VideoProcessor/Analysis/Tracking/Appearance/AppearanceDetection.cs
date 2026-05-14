namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

internal sealed record AppearanceDetection(
    Guid DetectedObjectId,
    string ClassName,
    double Confidence,
    TrackBox Box,
    AppearanceFeature? AppearanceFeature);
