namespace VideoAnonymizer.VideoProcessor.Analysis.Detection;

internal sealed record GpuRuntimeInfo(
    bool CudaExecutionProviderActive,
    int? GpuMemoryTotalMb);
