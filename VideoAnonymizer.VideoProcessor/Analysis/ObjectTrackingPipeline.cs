using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed class ObjectTrackingPipeline(
    ILogger<ObjectTrackingPipeline> logger,
    IServiceProvider serviceProvider,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    IConfiguration configuration,
    VideoAnalysisProgressReporter progressReporter)
{
    public async Task<ObjectTrackingPipelineResult> RunAsync(
        Guid videoId,
        string videoPath,
        double fps,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        var options = ObjectTrackingPipelineOptions.FromConfiguration(configuration);
        if (options.TrackingMode == ObjectTrackingMode.Appearance)
        {
            return await TrackPersistedFramesByAppearanceAsync(
                videoId,
                videoPath,
                totalFramesToAnalyze,
                options,
                lastReportedProgress,
                cancellationToken);
        }

        using var scope = serviceProvider.CreateScope();
        var objectDetectionClient = scope.ServiceProvider.GetRequiredService<ObjectDetectionClient.ObjectDetectionClient>();
        var sessionId = Guid.NewGuid().ToString();
        try
        {
            return await TrackPersistedFramesWithByteTrackAsync(
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

    private async Task<ObjectTrackingPipelineResult> TrackPersistedFramesByAppearanceAsync(
        Guid videoId,
        string videoPath,
        int totalFramesToAnalyze,
        ObjectTrackingPipelineOptions options,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException("Video file not found for appearance tracking.", videoPath);

        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open video for appearance tracking: {videoPath}");

        var tracker = new AppearanceObjectTracker(options.AppearanceTracking);
        var trackedFrameCount = 0;
        var lastFrameIndex = -1;

        while (true)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var frames = await db.AnalyzedFrames
                .Include(frame => frame.DetectedObjects)
                .Where(frame => frame.VideoId == videoId && frame.FrameIndex > lastFrameIndex)
                .OrderBy(frame => frame.FrameIndex)
                .Take(options.SaveBatchSize)
                .ToListAsync(cancellationToken);

            if (frames.Count == 0)
                break;

            foreach (var frame in frames)
            {
                using var frameMat = ReadFrame(capture, frame.FrameIndex);
                var detections = frame.DetectedObjects
                    .OrderBy(obj => obj.X)
                    .ThenBy(obj => obj.Y)
                    .Select(obj => CreateAppearanceDetection(
                        obj,
                        frameMat,
                        options.AppearanceTracking.CropPaddingPercent))
                    .ToList();
                var assignments = tracker.AssignTracks(
                    frame.FrameIndex,
                    frame.TimeSeconds,
                    detections);

                foreach (var detectedObject in frame.DetectedObjects)
                {
                    if (assignments.TryGetValue(detectedObject.Id, out var trackId))
                        detectedObject.TrackId = trackId;
                }

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

    private static Mat ReadFrame(VideoCapture capture, int frameIndex)
    {
        capture.Set(VideoCaptureProperties.PosFrames, Math.Max(0, frameIndex));

        var frame = new Mat();
        if (capture.Read(frame) && !frame.Empty())
            return frame;

        frame.Dispose();
        return new Mat();
    }

    private static AppearanceDetection CreateAppearanceDetection(
        DetectedObject detectedObject,
        Mat frame,
        double cropPaddingPercent)
    {
        var className = string.IsNullOrWhiteSpace(detectedObject.ClassName)
            ? "face"
            : detectedObject.ClassName.Trim().ToLowerInvariant();
        var box = new TrackBox(
            detectedObject.X,
            detectedObject.Y,
            detectedObject.Width,
            detectedObject.Height);

        return new AppearanceDetection(
            detectedObject.Id,
            className,
            detectedObject.Confidence,
            box,
            AppearanceFeatureExtractor.Extract(frame, box, cropPaddingPercent));
    }

    private async Task<ObjectTrackingPipelineResult> TrackPersistedFramesWithByteTrackAsync(
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
