using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.ApiService.DataServices;

public class ObjectDetectionApiStartupWaiter : BackgroundService
{
    private readonly ObjectDetectionClient.ObjectDetectionClient _client;
    private readonly IObjectDetectionApiReadyState _state;
    private readonly ILogger<ObjectDetectionApiStartupWaiter> _logger;

    public ObjectDetectionApiStartupWaiter(
        ObjectDetectionClient.ObjectDetectionClient client,
        IObjectDetectionApiReadyState state,
        ILogger<ObjectDetectionApiStartupWaiter> logger)
    {
        _client = client;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500));

                var result = await _client.Health_health_getAsync(timeoutCts.Token);

                var json = (JsonElement)result;

                if (json.TryGetProperty("status", out var statusProp) &&
                    statusProp.GetString() == "running")
                {
                    _state.Set(true);
                    return;
                }
            }
            catch (Exception)
            {
                // ignore → retry
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}