using Microsoft.AspNetCore.SignalR;
using VideoAnonymizer.ApiService.Notifications;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.ApiService.DataServices;

public sealed class TrackForwardJobWorker(
    TrackForwardJobQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<LongRunningJobsHub> hub,
    ILogger<TrackForwardJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(TrackForwardJob job, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var forwardTrackingService = scope.ServiceProvider.GetRequiredService<ForwardTrackingService>();

            var result = await forwardTrackingService.TrackForwardAsync(
                job.VideoId,
                job.Request,
                cancellationToken);

            await hub.Clients.All.SendAsync(
                SharedConstants.SignalR.Messages.TrackForwardCompleted,
                new TrackForwardCompletedMessage
                {
                    JobId = job.JobId,
                    VideoId = job.VideoId,
                    Status = SharedConstants.SignalR.Status.Completed,
                    Result = result
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Track forward job {JobId} failed for video {VideoId}.", job.JobId, job.VideoId);

            await hub.Clients.All.SendAsync(
                SharedConstants.SignalR.Messages.TrackForwardCompleted,
                new TrackForwardCompletedMessage
                {
                    JobId = job.JobId,
                    VideoId = job.VideoId,
                    Status = SharedConstants.SignalR.Status.Failed,
                    Error = ex.Message
                },
                CancellationToken.None);
        }
    }
}
