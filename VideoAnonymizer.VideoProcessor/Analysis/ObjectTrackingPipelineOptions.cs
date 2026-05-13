namespace VideoAnonymizer.VideoProcessor;

internal sealed record ObjectTrackingPipelineOptions(int SaveBatchSize)
{
    public static ObjectTrackingPipelineOptions FromConfiguration(IConfiguration configuration)
    {
        return new ObjectTrackingPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)));
    }
}
