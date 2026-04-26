using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using VideoAnonymizer.Database;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices
{
    public class StateDataService(
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
        IObjectDetectionApiReadyState pythonApiReadyState,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        public async Task<AppStateDto> LoadState(CancellationToken cancellationToken = default)
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
            var modelAvailable = dbContext.SystemSettings.FirstOrDefault(x => x.Key.Equals(SystemSettingConstants.ModelAvailable));
            var isStandalone = configuration.GetSection("Standalone").Exists();

            var appState = new AppStateDto()
            {
                IsStandalone = isStandalone,
                ModelAvailable = modelAvailable is null ? false : modelAvailable.ReadBooleanValue(),
                ObjectDetectionApiRunning = pythonApiReadyState.IsReady,
            };

            if (isStandalone && pythonApiReadyState.IsReady)
            {
                appState.CudaRuntime = await LoadCudaRuntimeStateAsync(cancellationToken);
            }

            return appState;
        }

        private async Task<CudaRuntimeStateDto?> LoadCudaRuntimeStateAsync(CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("objectDetection");
                var health = await httpClient.GetFromJsonAsync<ObjectDetectionHealthDto>("health", cancellationToken);
                return health?.Cuda?.ToAppState();
            }
            catch
            {
                return new CudaRuntimeStateDto
                {
                    Severity = "warning",
                    Summary = "CUDA status could not be read from the local AI service.",
                    Recommendation = "Continue only if CPU processing is acceptable, or restart the standalone app after installing CUDA Toolkit 12.x and cuDNN 9.x."
                };
            }
        }

        private sealed class ObjectDetectionHealthDto
        {
            [JsonPropertyName("cuda")]
            public ObjectDetectionCudaRuntimeStateDto? Cuda { get; set; }
        }

        private sealed class ObjectDetectionCudaRuntimeStateDto
        {
            [JsonPropertyName("severity")]
            public string? Severity { get; set; }

            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("nvidia_gpu_detected")]
            public bool NvidiaGpuDetected { get; set; }

            [JsonPropertyName("gpu_names")]
            public string[]? GpuNames { get; set; }

            [JsonPropertyName("nvidia_driver_version")]
            public string? NvidiaDriverVersion { get; set; }

            [JsonPropertyName("onnxruntime_version")]
            public string? OnnxRuntimeVersion { get; set; }

            [JsonPropertyName("available_providers")]
            public string[]? AvailableProviders { get; set; }

            [JsonPropertyName("active_providers")]
            public string[]? ActiveProviders { get; set; }

            [JsonPropertyName("cuda_provider_available")]
            public bool CudaProviderAvailable { get; set; }

            [JsonPropertyName("cuda_execution_provider_active")]
            public bool CudaExecutionProviderActive { get; set; }

            [JsonPropertyName("runs_on_cpu")]
            public bool RunsOnCpu { get; set; }

            [JsonPropertyName("initialization_error")]
            public string? InitializationError { get; set; }

            [JsonPropertyName("missing_dependencies")]
            public string[]? MissingDependencies { get; set; }

            [JsonPropertyName("recommendation")]
            public string? Recommendation { get; set; }

            [JsonPropertyName("installation_hint")]
            public string[]? InstallationHint { get; set; }

            public CudaRuntimeStateDto ToAppState()
            {
                return new CudaRuntimeStateDto
                {
                    Severity = string.IsNullOrWhiteSpace(Severity) ? "warning" : Severity,
                    Summary = Summary ?? string.Empty,
                    NvidiaGpuDetected = NvidiaGpuDetected,
                    GpuNames = GpuNames ?? [],
                    NvidiaDriverVersion = NvidiaDriverVersion,
                    OnnxRuntimeVersion = OnnxRuntimeVersion,
                    AvailableProviders = AvailableProviders ?? [],
                    ActiveProviders = ActiveProviders ?? [],
                    CudaProviderAvailable = CudaProviderAvailable,
                    CudaExecutionProviderActive = CudaExecutionProviderActive,
                    RunsOnCpu = RunsOnCpu,
                    InitializationError = InitializationError,
                    MissingDependencies = MissingDependencies ?? [],
                    Recommendation = Recommendation ?? string.Empty,
                    InstallationHint = InstallationHint ?? [],
                };
            }
        }
    }
}
