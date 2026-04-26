namespace VideoAnonymizer.Web.Shared.DTO;

public class CudaRuntimeStateDto
{
    public string Severity { get; set; } = "warning";
    public string Summary { get; set; } = string.Empty;
    public bool NvidiaGpuDetected { get; set; }
    public string[] GpuNames { get; set; } = [];
    public string? NvidiaDriverVersion { get; set; }
    public string? OnnxRuntimeVersion { get; set; }
    public string[] AvailableProviders { get; set; } = [];
    public string[] ActiveProviders { get; set; } = [];
    public bool CudaProviderAvailable { get; set; }
    public bool CudaExecutionProviderActive { get; set; }
    public bool RunsOnCpu { get; set; }
    public string? InitializationError { get; set; }
    public string[] MissingDependencies { get; set; } = [];
    public string Recommendation { get; set; } = string.Empty;
    public string[] InstallationHint { get; set; } = [];
}
