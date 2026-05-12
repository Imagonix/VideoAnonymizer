namespace VideoAnonymizer.VideoProcessor;

internal sealed record VideoAnalysisPipelineOptions(
    int WorkerCount,
    int BatchSize,
    int QueueCapacity,
    int SaveBatchSize)
{
    public static VideoAnalysisPipelineOptions FromConfiguration(IConfiguration configuration)
    {
        return new VideoAnalysisPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionWorkerCount", 2)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionBatchSize", 8)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionQueueCapacity", 50)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)));
    }
}
