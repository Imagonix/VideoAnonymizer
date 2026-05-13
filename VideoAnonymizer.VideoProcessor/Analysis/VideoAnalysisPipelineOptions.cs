using System.Text.Json;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed record VideoAnalysisPipelineOptions(
    int WorkerCount,
    int BatchSize,
    int QueueCapacity,
    int SaveBatchSize)
{
    private const int DefaultWorkerCount = 4;
    private const int DefaultBatchSize = 8;
    private const int DefaultQueueCapacity = 50;
    private const int DefaultSaveBatchSize = 25;

    public static async Task<VideoAnalysisPipelineOptions> FromConfigurationAsync(
        IConfiguration configuration,
        global::VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var configuredOptions = FromExplicitConfiguration(configuration);
        var autoDetectGpuSettings = configuration.GetValue("ObjectDetection:AutoDetectGpuSettings", false);
        if (!autoDetectGpuSettings)
            return configuredOptions;

        var autoOptions = await TryGetAutoOptionsAsync(
            configuredOptions,
            objectDetectionClient,
            logger,
            cancellationToken);

        if (autoOptions is not null)
        {
            logger.LogInformation(
                "Auto-selected object detection settings from runtime VRAM data. Workers: {WorkerCount}, batch size: {BatchSize}, queue capacity: {QueueCapacity}",
                autoOptions.WorkerCount,
                autoOptions.BatchSize,
                autoOptions.QueueCapacity);
            return autoOptions;
        }

        logger.LogWarning(
            "Object detection GPU auto-detect is enabled, but runtime VRAM data was not available. Falling back to configured settings. Workers: {WorkerCount}, batch size: {BatchSize}, queue capacity: {QueueCapacity}",
            configuredOptions.WorkerCount,
            configuredOptions.BatchSize,
            configuredOptions.QueueCapacity);
        return configuredOptions;
    }

    private static VideoAnalysisPipelineOptions FromExplicitConfiguration(IConfiguration configuration)
    {
        return new VideoAnalysisPipelineOptions(
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionWorkerCount", DefaultWorkerCount)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionBatchSize", DefaultBatchSize)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionQueueCapacity", DefaultQueueCapacity)),
            Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", DefaultSaveBatchSize)));
    }

    private static async Task<VideoAnalysisPipelineOptions?> TryGetAutoOptionsAsync(
        VideoAnalysisPipelineOptions configuredOptions,
        global::VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var gpuRuntime = await TryGetGpuRuntimeAsync(
            objectDetectionClient,
            logger,
            cancellationToken);

        if (gpuRuntime is null)
            return null;

        return new VideoAnalysisPipelineOptions(
            CalculateAutoWorkerCount(gpuRuntime.CudaExecutionProviderActive),
            CalculateAutoBatchSize(gpuRuntime.CudaExecutionProviderActive, gpuRuntime.GpuMemoryTotalMb),
            CalculateAutoQueueCapacity(gpuRuntime.CudaExecutionProviderActive),
            configuredOptions.SaveBatchSize);
    }

    private static async Task<GpuRuntimeInfo?> TryGetGpuRuntimeAsync(
        global::VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            var health = await objectDetectionClient.Health_health_getAsync(timeoutCts.Token);
            if (health is not JsonElement healthJson)
                return null;

            if (!healthJson.TryGetProperty("cuda", out var cuda))
                return null;

            var cudaExecutionProviderActive =
                TryGetBoolean(cuda, "cuda_execution_provider_active", out var isActive)
                    && isActive;
            var gpuMemoryTotalMb =
                TryGetInt32(cuda, "gpu_memory_total_mb", out var memoryTotalMb)
                    ? (int?)memoryTotalMb
                    : null;

            return new GpuRuntimeInfo(cudaExecutionProviderActive, gpuMemoryTotalMb);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Failed to read object detection runtime VRAM data.");
            return null;
        }
    }

    private static int CalculateAutoWorkerCount(bool cudaExecutionProviderActive)
    {
        return cudaExecutionProviderActive ? 4 : 2;
    }

    private static int CalculateAutoBatchSize(bool cudaExecutionProviderActive, int? gpuMemoryTotalMb)
    {
        if (!cudaExecutionProviderActive)
            return 4;

        if (gpuMemoryTotalMb is null)
            return DefaultBatchSize;

        return gpuMemoryTotalMb.Value switch
        {
            < 5 * 1024 => 4,
            < 8 * 1024 => 8,
            < 12 * 1024 => 16,
            < 16 * 1024 => 24,
            _ => 32
        };
    }

    private static int CalculateAutoQueueCapacity(bool cudaExecutionProviderActive)
    {
        return cudaExecutionProviderActive ? 100 : 50;
    }

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number)
            return property.TryGetInt32(out value);

        return property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out value);
    }

    private static bool TryGetBoolean(JsonElement element, string propertyName, out bool value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = property.GetBoolean();
            return true;
        }

        return property.ValueKind == JsonValueKind.String
            && bool.TryParse(property.GetString(), out value);
    }

    private sealed record GpuRuntimeInfo(
        bool CudaExecutionProviderActive,
        int? GpuMemoryTotalMb);
}
