using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed class SingleObjectTracker(
    ILogger<SingleObjectTracker> logger,
    IMessagePublisher messagePublisher,
    IServiceScopeFactory scopeFactory) : SingleJobQueingWorker<TrackForwardJob>(logger)
{
    protected override async Task HandleJob(TrackForwardJob job, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var forwardTrackingService = scope.ServiceProvider.GetRequiredService<ForwardTrackingService>();

        try
        {
            var result = await forwardTrackingService.TrackForwardIncrementalAsync(
                job.VideoId, job,
                async gapResult =>
                {
                    await messagePublisher.PublishAsync(
                        RabbitMQConstants.RoutingKeys.TrackForwardProgress,
                        new TrackForwardProgress(
                            job.JobId, job.VideoId, DateTimeOffset.UtcNow,
                            "completed", string.Empty, gapResult.TrackId,
                            gapResult.GapStartTimeMs, gapResult.GapEndTimeMs,
                            gapResult.CreatedObjectIds.ToList(), gapResult.IsFinal),
                        stoppingToken);
                },
                stoppingToken);

            await messagePublisher.PublishAsync(
                RabbitMQConstants.RoutingKeys.TrackForwardCompleted,
                new TrackForwardCompleted
                {
                    JobId = job.JobId,
                    VideoId = job.VideoId,
                    AddedAt = DateTimeOffset.UtcNow,
                    Status = "completed",
                    TrackId = result.TrackId,
                    CreatedObjectIds = result.CreatedObjectIds.ToList(),
                    CreatedDetections = result.CreatedDetections,
                    SkippedConflicts = result.SkippedConflicts,
                    ReacquiredCount = result.ReacquiredCount,
                    StoppedReason = result.StoppedReason,
                    Gaps = result.Gaps
                        .Select(gap => new TrackForwardGapSummary(gap.StartTimeMs, gap.EndTimeMs))
                        .ToList()
                },
                stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Track forward job {JobId} failed for video {VideoId}.", job.JobId, job.VideoId);

            await messagePublisher.PublishAsync(
                RabbitMQConstants.RoutingKeys.TrackForwardProgress,
                new TrackForwardProgress(
                    job.JobId, job.VideoId, DateTimeOffset.UtcNow,
                    "failed", ex.Message, null, 0, 0, null, true),
                CancellationToken.None);

            await messagePublisher.PublishAsync(
                RabbitMQConstants.RoutingKeys.TrackForwardCompleted,
                new TrackForwardCompleted(
                    job.JobId, job.VideoId, DateTimeOffset.UtcNow,
                    "failed", ex.Message, null),
                CancellationToken.None);
        }
    }
}
