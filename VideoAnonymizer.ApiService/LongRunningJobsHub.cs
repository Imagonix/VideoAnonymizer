using Microsoft.AspNetCore.SignalR;

namespace VideoAnonymizer.ApiService
{
    public class LongRunningJobsHub(ILogger<LongRunningJobsHub> logger) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            logger.LogDebug($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogDebug($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}