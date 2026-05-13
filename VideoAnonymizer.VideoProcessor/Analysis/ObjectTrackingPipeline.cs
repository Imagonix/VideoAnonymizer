using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class ObjectTrackingPipeline(
    ILogger<ObjectTrackingPipeline> logger,
    IServiceProvider serviceProvider,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    IConfiguration configuration,
    VideoAnalysisProgressReporter progressReporter)
{
    public async Task<ObjectTrackingPipelineResult> RunAsync(
        Guid videoId,
        double fps,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var objectDetectionClient = scope.ServiceProvider.GetRequiredService<ObjectDetectionClient.ObjectDetectionClient>();
        var sessionId = Guid.NewGuid().ToString();
        var options = ObjectTrackingPipelineOptions.FromConfiguration(configuration);

        try
        {
            return await TrackPersistedFramesAsync(
                videoId,
                sessionId,
                fps,
                totalFramesToAnalyze,
                options.SaveBatchSize,
                objectDetectionClient,
                lastReportedProgress,
                cancellationToken);
        }
        finally
        {
            await CleanupTrackerAsync(objectDetectionClient, sessionId);
        }
    }

    private async Task<ObjectTrackingPipelineResult> TrackPersistedFramesAsync(
        Guid videoId,
        string sessionId,
        double fps,
        int totalFramesToAnalyze,
        int batchSize,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        var trackedFrameCount = 0;
        var lastFrameIndex = -1;

        while (true)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var frames = await db.AnalyzedFrames
                .Include(frame => frame.DetectedObjects)
                .Where(frame => frame.VideoId == videoId && frame.FrameIndex > lastFrameIndex)
                .OrderBy(frame => frame.FrameIndex)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (frames.Count == 0)
                break;

            foreach (var frame in frames)
            {
                var rawDetections = frame.DetectedObjects
                    .Select(DetectedObjectFactory.ToDetectionResult)
                    .ToList();
                var trackedDetections = await objectDetectionClient.TrackObjectsAsync(
                    new TrackObjectsRequest
                    {
                        Detections = rawDetections,
                        SessionId = sessionId,
                        Fps = fps
                    },
                    cancellationToken);

                db.DetectedObjects.RemoveRange(frame.DetectedObjects);
                await db.DetectedObjects.AddRangeAsync(
                    trackedDetections.Select(detection => DetectedObjectFactory.CreateDetectedObject(frame.Id, detection)),
                    cancellationToken);

                trackedFrameCount++;
                lastFrameIndex = frame.FrameIndex;
            }

            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();

            lastReportedProgress = await progressReporter.ReportRangeAsync(
                videoId,
                videoId,
                trackedFrameCount,
                totalFramesToAnalyze,
                VideoAnalysisProgressRanges.TrackingStart,
                VideoAnalysisProgressRanges.TrackingEnd,
                lastReportedProgress,
                $"Assigned tracks for {trackedFrameCount} of {totalFramesToAnalyze} analyzed frames...",
                cancellationToken);
        }

        return new ObjectTrackingPipelineResult(trackedFrameCount, lastReportedProgress);
    }

    private async Task CleanupTrackerAsync(ObjectDetectionClient.ObjectDetectionClient objectDetectionClient, string sessionId)
    {
        try
        {
            await objectDetectionClient.Cleanup_tracker_endpoint_cleanupTracker_postAsync(sessionId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up tracker session {SessionId}.", sessionId);
        }
    }
}

internal sealed record ObjectTrackingPipelineResult(
    int TrackedFrameCount,
    int LastReportedProgress);
