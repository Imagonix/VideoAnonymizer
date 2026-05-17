using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed record ObjectTrackingPipelineOptions(
    int SaveBatchSize,
    int QueueCapacity,
    AppearanceObjectTrackingOptions AppearanceTracking)
{
    public int FeatureWorkerCount => Math.Max(1, Math.Min(4, Environment.ProcessorCount / 2));

    public static ObjectTrackingPipelineOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("ObjectDetection:AppearanceTracking");

        return new ObjectTrackingPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)),
            Math.Max(1, section.GetValue("QueueCapacity", 16)),
            AppearanceObjectTrackingOptions.FromConfiguration(configuration));
    }
}
