using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed record ObjectTrackingPipelineOptions(
    int SaveBatchSize,
    AppearanceObjectTrackingOptions AppearanceTracking)
{
    public static ObjectTrackingPipelineOptions FromConfiguration(IConfiguration configuration)
    {
        return new ObjectTrackingPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)),
            AppearanceObjectTrackingOptions.FromConfiguration(configuration));
    }
}
