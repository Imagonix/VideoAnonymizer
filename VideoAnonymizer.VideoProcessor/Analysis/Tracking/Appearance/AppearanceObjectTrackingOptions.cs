namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

internal sealed record AppearanceObjectTrackingOptions(
    int MaxSamplesPerTrack,
    double MaxTrackGapSeconds,
    double MinAppearanceSimilarity,
    double MinAssignmentScore,
    double MinSpatialSimilarity,
    double MinSizeSimilarity,
    double AppearanceScoreWeight,
    double SpatialScoreWeight,
    double SizeScoreWeight,
    double RecencyScoreWeight,
    double MaxCenterDistanceBoxDiagonals,
    double CropPaddingPercent)
{
    public static AppearanceObjectTrackingOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("ObjectDetection:AppearanceTracking");

        return new AppearanceObjectTrackingOptions(
            Math.Max(1, section.GetValue("MaxSamplesPerTrack", 5)),
            Math.Max(0, section.GetValue("MaxTrackGapSeconds", 5.0)),
            Math.Clamp(section.GetValue("MinAppearanceSimilarity", 0.62), 0, 1),
            Math.Clamp(section.GetValue("MinAssignmentScore", 0.68), 0, 1),
            Math.Clamp(section.GetValue("MinSpatialSimilarity", 0.05), 0, 1),
            Math.Clamp(section.GetValue("MinSizeSimilarity", 0.35), 0, 1),
            Math.Max(0, section.GetValue("AppearanceScoreWeight", 0.65)),
            Math.Max(0, section.GetValue("SpatialScoreWeight", 0.20)),
            Math.Max(0, section.GetValue("SizeScoreWeight", 0.10)),
            Math.Max(0, section.GetValue("RecencyScoreWeight", 0.05)),
            Math.Max(0.1, section.GetValue("MaxCenterDistanceBoxDiagonals", 6.0)),
            Math.Clamp(section.GetValue("CropPaddingPercent", 0.15), 0, 1));
    }
}
