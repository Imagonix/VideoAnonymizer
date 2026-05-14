using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed record ObjectTrackingPipelineOptions(
    int SaveBatchSize,
    ObjectTrackingMode TrackingMode,
    AppearanceObjectTrackingOptions AppearanceTracking)
{
    public static ObjectTrackingPipelineOptions FromConfiguration(IConfiguration configuration)
    {
        return new ObjectTrackingPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)),
            ParseTrackingMode(configuration.GetValue("ObjectDetection:TrackingMode", "Appearance")),
            AppearanceObjectTrackingOptions.FromConfiguration(configuration));
    }

    private static ObjectTrackingMode ParseTrackingMode(string? value)
    {
        return Enum.TryParse<ObjectTrackingMode>(value, ignoreCase: true, out var mode)
            ? mode
            : ObjectTrackingMode.Appearance;
    }
}
