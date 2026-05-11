using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnalyzer(
    ILogger<VideoAnalyzer> logger,
    IMessagePublisher messagePublisher,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    private const string Operation = "analyze";
    private const int DetectionProgressStart = 5;
    private const int DetectionProgressEnd = 80;
    private const int TrackingProgressStart = 80;
    private const int TrackingProgressEnd = 95;

    protected override async Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        var lastReportedProgress = -1;
        lastReportedProgress = await ReportProgressAsync(
            job.VideoId,
            job.VideoId,
            Operation,
            0,
            lastReportedProgress,
            "Loading video...",
            stoppingToken);

        if (string.IsNullOrWhiteSpace(job.Path))
            throw new ArgumentException("Video path is empty.", nameof(job.Path));
        if (!File.Exists(job.Path))
            throw new FileNotFoundException("Video file not found.", job.Path);

        using var scope = serviceProvider.CreateScope();
        var objectDetectionClient = scope.ServiceProvider.GetRequiredService<ObjectDetectionClient.ObjectDetectionClient>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();

        await EnsureVideoExistsAsync(dbFactory, job.VideoId, stoppingToken);

        var videoMetadata = ReadVideoMetadata(job.Path, job.CaptureIntervalMs);
        var options = DetectionPipelineOptions.FromConfiguration(configuration);
        var sessionId = Guid.NewGuid().ToString();

        logger.LogInformation(
            "Processing video {VideoPath} with FPS {Fps}. Detection workers: {WorkerCount}, queue capacity: {QueueCapacity}",
            job.Path,
            videoMetadata.Fps,
            options.WorkerCount,
            options.QueueCapacity);

        lastReportedProgress = await ReportProgressAsync(
            job.VideoId,
            job.VideoId,
            Operation,
            DetectionProgressStart,
            lastReportedProgress,
            "Preparing frame analysis...",
            stoppingToken);

        var detectionResult = await RunDetectionPipelineAsync(
            job,
            videoMetadata,
            options,
            objectDetectionClient,
            dbFactory,
            lastReportedProgress,
            stoppingToken);
        lastReportedProgress = detectionResult.LastReportedProgress;

        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            lastReportedProgress = await ReportProgressAsync(
                job.VideoId,
                job.VideoId,
                Operation,
                TrackingProgressStart,
                lastReportedProgress,
                "Assigning object tracks...",
                stoppingToken);

            var trackingResult = await TrackPersistedFramesAsync(
                job.VideoId,
                sessionId,
                videoMetadata.Fps,
                videoMetadata.TotalFramesToAnalyze,
                options.SaveBatchSize,
                objectDetectionClient,
                dbFactory,
                lastReportedProgress,
                stoppingToken);
            lastReportedProgress = trackingResult.LastReportedProgress;
        }
        finally
        {
            await CleanupTrackerAsync(objectDetectionClient, sessionId);
        }

        stoppingToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Finished processing video {VideoPath}. Analyzed frames: {ProcessedFrameCount}",
            job.Path,
            detectionResult.SavedFrameCount);

        lastReportedProgress = await ReportProgressAsync(
            job.VideoId,
            job.VideoId,
            Operation,
            TrackingProgressEnd,
            lastReportedProgress,
            "Finalizing analysis...",
            stoppingToken);

        await ReportProgressAsync(
            job.VideoId,
            job.VideoId,
            Operation,
            100,
            lastReportedProgress,
            "Analysis saved.",
            stoppingToken);
        await messagePublisher.PublishAsync(RabbitMQConstants.RoutingKeys.Analyzed, new AnalyzedVideo(job.VideoId, DateTime.Now), stoppingToken);
    }

    private async Task<(int SavedFrameCount, int LastReportedProgress)> RunDetectionPipelineAsync(
        AnalyzeVideo job,
        VideoAnalysisMetadata videoMetadata,
        DetectionPipelineOptions options,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        using var pipelineCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = pipelineCts.Token;

        var frameJobs = Channel.CreateBounded<FrameDetectionJob>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });
        var detectionResults = Channel.CreateBounded<FrameDetectionResult>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        var schedulerTask = Task.Run(
            () => ScheduleFrameDetectionJobsAsync(
                job.Path,
                videoMetadata.Fps,
                videoMetadata.FrameStep,
                videoMetadata.LastFrameIndex,
                frameJobs.Writer,
                pipelineToken));

        var workerTasks = Enumerable.Range(1, options.WorkerCount)
            .Select(workerId => DetectFramesAsync(
                workerId,
                frameJobs.Reader,
                detectionResults.Writer,
                objectDetectionClient,
                pipelineToken))
            .ToArray();

        var collectorTask = CollectDetectionResultsAsync(
            job.VideoId,
            detectionResults.Reader,
            dbFactory,
            options.SaveBatchSize,
            videoMetadata.TotalFramesToAnalyze,
            lastReportedProgress,
            pipelineToken);

        var workersCompletionTask = CompleteResultWriterWhenWorkersCompleteAsync(workerTasks, detectionResults.Writer);

        CancelPipelineOnFault(schedulerTask, pipelineCts);
        CancelPipelineOnFault(workersCompletionTask, pipelineCts);
        CancelPipelineOnFault(collectorTask, pipelineCts);

        try
        {
            await Task.WhenAll(schedulerTask, workersCompletionTask, collectorTask);
            return collectorTask.Result;
        }
        catch
        {
            pipelineCts.Cancel();
            frameJobs.Writer.TryComplete();
            detectionResults.Writer.TryComplete();
            throw;
        }
    }

    private static void CancelPipelineOnFault(Task task, CancellationTokenSource pipelineCts)
    {
        _ = task.ContinueWith(
            _ => pipelineCts.Cancel(),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private async Task<int> ScheduleFrameDetectionJobsAsync(
        string videoPath,
        double fps,
        int frameStep,
        int lastFrameIndex,
        ChannelWriter<FrameDetectionJob> writer,
        CancellationToken cancellationToken)
    {
        var scheduledFrameCount = 0;

        try
        {
            using var capture = new VideoCapture(videoPath);
            if (!capture.IsOpened())
                throw new InvalidOperationException($"Could not open video: {videoPath}");

            using var frame = new Mat();
            var frameIndex = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var success = capture.Read(frame);
                if (!success || frame.Empty())
                    break;

                var shouldProcess =
                    frameIndex == 0 ||
                    frameIndex == lastFrameIndex ||
                    frameIndex % frameStep == 0;

                if (shouldProcess)
                {
                    var job = new FrameDetectionJob(
                        frameIndex,
                        frameIndex / fps,
                        ConvertMatToBase64Jpeg(frame));
                    await writer.WriteAsync(job, cancellationToken);
                    scheduledFrameCount++;
                }

                frameIndex++;
            }

            writer.TryComplete();
            logger.LogInformation("Scheduled {FrameCount} frames for detection.", scheduledFrameCount);
            return scheduledFrameCount;
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
    }

    private async Task DetectFramesAsync(
        int workerId,
        ChannelReader<FrameDetectionJob> reader,
        ChannelWriter<FrameDetectionResult> writer,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        CancellationToken cancellationToken)
    {
        await foreach (var job in reader.ReadAllAsync(cancellationToken))
        {
            var detections = await objectDetectionClient.DetectObjects_detectObjects_postAsync(
                new DetectRequest
                {
                    ImageBase64 = job.ImageBase64,
                    SessionId = string.Empty,
                    Fps = 0
                },
                cancellationToken);

            await writer.WriteAsync(
                new FrameDetectionResult(job.FrameIndex, job.TimeSeconds, detections.ToList()),
                cancellationToken);

            logger.LogDebug(
                "Detection worker {WorkerId} processed frame {FrameIndex}. Detections: {DetectionCount}",
                workerId,
                job.FrameIndex,
                detections.Count);
        }
    }

    private static async Task CompleteResultWriterWhenWorkersCompleteAsync(
        Task[] workerTasks,
        ChannelWriter<FrameDetectionResult> writer)
    {
        try
        {
            await Task.WhenAll(workerTasks);
            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
    }

    private async Task<(int SavedFrameCount, int LastReportedProgress)> CollectDetectionResultsAsync(
        Guid videoId,
        ChannelReader<FrameDetectionResult> reader,
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
        int batchSize,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        var batch = new List<FrameDetectionResult>(batchSize);
        var savedFrameCount = 0;

        await foreach (var result in reader.ReadAllAsync(cancellationToken))
        {
            batch.Add(result);
            if (batch.Count < batchSize)
                continue;

            await SaveBatchAsync();
        }

        if (batch.Count > 0)
            await SaveBatchAsync();

        return (savedFrameCount, lastReportedProgress);

        async Task SaveBatchAsync()
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var frames = batch
                .Select(result => CreateAnalyzedFrame(videoId, result))
                .ToList();

            await db.AnalyzedFrames.AddRangeAsync(frames, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();

            savedFrameCount += batch.Count;
            lastReportedProgress = await ReportProgressAsync(
                videoId,
                videoId,
                Operation,
                savedFrameCount,
                totalFramesToAnalyze,
                DetectionProgressStart,
                DetectionProgressEnd,
                lastReportedProgress,
                $"Saved {savedFrameCount} of {totalFramesToAnalyze} analyzed frames...",
                cancellationToken);

            batch.Clear();
        }
    }

    private async Task<(int TrackedFrameCount, int LastReportedProgress)> TrackPersistedFramesAsync(
        Guid videoId,
        string sessionId,
        double fps,
        int totalFramesToAnalyze,
        int batchSize,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
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
                    .Select(ToDetectionResult)
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
                    trackedDetections.Select(detection => CreateDetectedObject(frame.Id, detection)),
                    cancellationToken);

                trackedFrameCount++;
                lastFrameIndex = frame.FrameIndex;
            }

            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();

            lastReportedProgress = await ReportProgressAsync(
                videoId,
                videoId,
                Operation,
                trackedFrameCount,
                totalFramesToAnalyze,
                TrackingProgressStart,
                TrackingProgressEnd,
                lastReportedProgress,
                $"Assigned tracks for {trackedFrameCount} of {totalFramesToAnalyze} analyzed frames...",
                cancellationToken);
        }

        return (trackedFrameCount, lastReportedProgress);
    }

    private static async Task EnsureVideoExistsAsync(
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
        Guid videoId,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var exists = await db.Videos.AnyAsync(video => video.Id == videoId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException($"Video {videoId} was not found.");
    }

    private static VideoAnalysisMetadata ReadVideoMetadata(string videoPath, int captureIntervalMs)
    {
        using var capture = new VideoCapture(videoPath);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open video: {videoPath}");

        var fps = capture.Fps;
        if (fps <= 0 || double.IsNaN(fps))
            fps = 25;

        var frameStep = Math.Max(1, (int)Math.Round(fps * captureIntervalMs * 0.001));
        var totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        var lastFrameIndex = Math.Max(0, totalFrames - 1);
        var totalFramesToAnalyze = CountFramesToAnalyze(totalFrames, frameStep);

        return new VideoAnalysisMetadata(fps, frameStep, lastFrameIndex, totalFramesToAnalyze);
    }

    private static AnalyzedFrame CreateAnalyzedFrame(Guid videoId, FrameDetectionResult result)
    {
        var analyzedFrame = new AnalyzedFrame
        {
            FrameIndex = result.FrameIndex,
            TimeSeconds = result.TimeSeconds,
            VideoId = videoId,
            DetectedObjects = []
        };

        analyzedFrame.DetectedObjects = result.Detections
            .Select(detection => CreateDetectedObject(analyzedFrame.Id, detection))
            .ToList();

        return analyzedFrame;
    }

    private static DetectedObject CreateDetectedObject(Guid analyzedFrameId, DetectionResult detection)
    {
        return new DetectedObject
        {
            Selected = true,
            AnalyzedFrameId = analyzedFrameId,
            Height = detection.Height,
            Width = detection.Width,
            X = detection.X,
            Y = detection.Y,
            ClassName = detection.ClassName,
            Confidence = detection.Confidence,
            TrackId = detection.TrackId
        };
    }

    private static DetectionResult ToDetectionResult(DetectedObject detectedObject)
    {
        return new DetectionResult
        {
            ClassName = detectedObject.ClassName ?? "face",
            Confidence = detectedObject.Confidence,
            X = detectedObject.X,
            Y = detectedObject.Y,
            Width = detectedObject.Width,
            Height = detectedObject.Height,
            TrackId = detectedObject.TrackId
        };
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

    private async Task<int> ReportProgressAsync(
        Guid jobId,
        Guid? videoId,
        string operation,
        int progressPercent,
        int lastReportedProgress,
        string status,
        CancellationToken cancellationToken)
    {
        progressPercent = Math.Clamp(progressPercent, 0, 100);

        if (progressPercent == lastReportedProgress)
            return lastReportedProgress;

        await PublishProgressAsync(jobId, videoId, operation, progressPercent, status, cancellationToken);
        return progressPercent;
    }

    private async Task<int> ReportProgressAsync(
        Guid jobId,
        Guid? videoId,
        string operation,
        int completed,
        int total,
        int rangeStart,
        int rangeEnd,
        int lastReportedProgress,
        string status,
        CancellationToken cancellationToken)
    {
        if (total <= 0)
            return lastReportedProgress;

        var progressPercent = rangeStart + (int)Math.Round(
            completed * (rangeEnd - rangeStart) / (double)total,
            MidpointRounding.AwayFromZero);
        progressPercent = Math.Clamp(progressPercent, rangeStart, rangeEnd);

        if (progressPercent == lastReportedProgress)
            return lastReportedProgress;

        await PublishProgressAsync(jobId, videoId, operation, progressPercent, status, cancellationToken);
        return progressPercent;
    }

    private Task PublishProgressAsync(
        Guid jobId,
        Guid? videoId,
        string operation,
        int? progressPercent,
        string status,
        CancellationToken cancellationToken)
    {
        return messagePublisher.PublishAsync(
            RabbitMQConstants.RoutingKeys.Progress,
            new VideoProcessingProgress(
                jobId,
                videoId,
                operation,
                progressPercent,
                status,
                DateTimeOffset.UtcNow),
            cancellationToken);
    }

    private static int CountFramesToAnalyze(int totalFrames, int frameStep)
    {
        if (totalFrames <= 0)
            return 0;

        var lastFrameIndex = totalFrames - 1;
        var safeFrameStep = Math.Max(1, frameStep);
        var count = lastFrameIndex / safeFrameStep + 1;

        if (lastFrameIndex > 0 && lastFrameIndex % safeFrameStep != 0)
            count++;

        return count;
    }

    private static string ConvertMatToBase64Jpeg(Mat frame)
    {
        Cv2.ImEncode(".jpg", frame, out var imageBytes);
        return Convert.ToBase64String(imageBytes);
    }

    private sealed record VideoAnalysisMetadata(
        double Fps,
        int FrameStep,
        int LastFrameIndex,
        int TotalFramesToAnalyze);

    private sealed record FrameDetectionJob(
        int FrameIndex,
        double TimeSeconds,
        string ImageBase64);

    private sealed record FrameDetectionResult(
        int FrameIndex,
        double TimeSeconds,
        IReadOnlyList<DetectionResult> Detections);

    private sealed record DetectionPipelineOptions(
        int WorkerCount,
        int QueueCapacity,
        int SaveBatchSize)
    {
        public static DetectionPipelineOptions FromConfiguration(IConfiguration configuration)
        {
            return new DetectionPipelineOptions(
                Math.Max(1, configuration.GetValue("ObjectDetection:DetectionWorkerCount", 4)),
                Math.Max(1, configuration.GetValue("ObjectDetection:DetectionQueueCapacity", 50)),
                Math.Max(1, configuration.GetValue("ObjectDetection:DetectionSaveBatchSize", 25)));
        }
    }
}
