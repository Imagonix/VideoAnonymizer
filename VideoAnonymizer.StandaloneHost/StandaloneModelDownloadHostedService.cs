using VideoAnonymizer.ModelDownloader;

namespace VideoAnonymizer.StandaloneHost;

public sealed class StandaloneModelDownloadHostedService(
    ModelDownloadService modelDownloadService,
    ILogger<StandaloneModelDownloadHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await modelDownloadService.EnsureModelExistsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Standalone model download failed.");
        }
    }
}
