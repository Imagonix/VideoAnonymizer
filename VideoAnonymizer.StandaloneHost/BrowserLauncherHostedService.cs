using System.Diagnostics;

namespace VideoAnonymizer.StandaloneHost;

public sealed class BrowserLauncherHostedService(
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<BrowserLauncherHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Standalone:OpenBrowser", true))
        {
            return Task.CompletedTask;
        }

        lifetime.ApplicationStarted.Register(OpenBrowser);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OpenBrowser()
    {
        var url = configuration["Standalone:Url"] ?? "http://127.0.0.1:5117";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not open browser for {Url}", url);
        }
    }
}
