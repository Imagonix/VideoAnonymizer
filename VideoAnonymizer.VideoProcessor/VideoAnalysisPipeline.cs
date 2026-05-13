using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class VideoAnalysisPipeline(
    ILogger<VideoAnalysisPipeline> logger,
    IServiceProvider serviceProvider,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    IConfiguration configuration,
    VideoAnalysisProgressReporter progressReporter)
{
    private static readonly TimeSpan BatchFillDelay = TimeSpan.FromMilliseconds(20);

    public async Task<VideoAnalysisPipelineResult> RunAsync(
        AnalyzeVideo job,
        VideoAnalysisMetadata videoMetadata,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var objectDetectionClient = scope.ServiceProvider.GetRequiredService<ObjectDetectionClient.ObjectDetectionClient>();
        var options = await VideoAnalysisPipelineOptions.FromConfigurationAsync(
            configuration,
            objectDetectionClient,
            logger,
            cancellationToken);

        logger.LogInformation(
            "Processing video {VideoPath} with FPS {Fps}. Detection workers: {WorkerCount}, batch size: {BatchSize}, queue capacity: {QueueCapacity}",
            job.Path,
            videoMetadata.Fps,
            options.WorkerCount,
            options.BatchSize,
            options.QueueCapacity);

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
                options.BatchSize,
                pipelineToken))
            .ToArray();

        var collectorTask = CollectDetectionResultsAsync(
            job.VideoId,
            detectionResults.Reader,
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
        int batchSize,
        CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken))
        {
            var batch = await ReadDetectionBatchAsync(reader, batchSize, cancellationToken);
            if (batch.Count == 0)
                continue;

            var results = await DetectFrameBatchWithFallbackAsync(
                batch,
                objectDetectionClient,
                cancellationToken);

            foreach (var result in results)
                await writer.WriteAsync(result, cancellationToken);

            logger.LogDebug(
                "Detection worker {WorkerId} processed {BatchSize} frames.",
                workerId,
                batch.Count);
        }
    }

    private static async Task<List<FrameDetectionJob>> ReadDetectionBatchAsync(
        ChannelReader<FrameDetectionJob> reader,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var batch = new List<FrameDetectionJob>(batchSize);

        while (batch.Count < batchSize)
        {
            if (reader.TryRead(out var job))
            {
                batch.Add(job);
                continue;
            }

            if (batch.Count == 0)
                return batch;

            await Task.Delay(BatchFillDelay, cancellationToken);

            if (reader.TryRead(out var delayedJob))
            {
                batch.Add(delayedJob);
                continue;
            }

            break;
        }

        return batch;
    }

    private async Task<IReadOnlyList<FrameDetectionResult>> DetectFrameBatchWithFallbackAsync(
        IReadOnlyList<FrameDetectionJob> batch,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 1)
            return [await DetectSingleFrameAsync(batch[0], objectDetectionClient, cancellationToken)];

        try
        {
            var response = await objectDetectionClient.DetectObjectsBatchAsync(
                new DetectObjectsBatchRequest
                {
                    Frames = batch
                        .Select(job => new DetectObjectsBatchFrame
                        {
                            FrameIndex = job.FrameIndex,
                            ImageBase64 = job.ImageBase64
                        })
                        .ToList()
                },
                cancellationToken);

            var resultsByFrameIndex = response.ToDictionary(
                result => result.FrameIndex,
                result => result.Detections?.ToList() ?? []);

            if (batch.Any(job => !resultsByFrameIndex.ContainsKey(job.FrameIndex)))
                throw new InvalidOperationException("Batch detection response did not include all requested frames.");

            return batch
                .Select(job => new FrameDetectionResult(
                    job.FrameIndex,
                    job.TimeSeconds,
                    resultsByFrameIndex.GetValueOrDefault(job.FrameIndex) ?? []))
                .ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                ex,
                "Batch detection failed for {BatchSize} frames. Splitting the batch and retrying.",
                batch.Count);

            var midpoint = batch.Count / 2;
            var firstHalf = await DetectFrameBatchWithFallbackAsync(
                batch.Take(midpoint).ToList(),
                objectDetectionClient,
                cancellationToken);
            var secondHalf = await DetectFrameBatchWithFallbackAsync(
                batch.Skip(midpoint).ToList(),
                objectDetectionClient,
                cancellationToken);

            return firstHalf.Concat(secondHalf).ToList();
        }
    }

    private static async Task<FrameDetectionResult> DetectSingleFrameAsync(
        FrameDetectionJob job,
        ObjectDetectionClient.ObjectDetectionClient objectDetectionClient,
        CancellationToken cancellationToken)
    {
        var detections = await objectDetectionClient.DetectObjects_detectObjects_postAsync(
            new DetectRequest
            {
                ImageBase64 = job.ImageBase64,
                SessionId = string.Empty,
                Fps = 0
            },
            cancellationToken);

        return new FrameDetectionResult(job.FrameIndex, job.TimeSeconds, detections.ToList());
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

    private async Task<VideoAnalysisPipelineResult> CollectDetectionResultsAsync(
        Guid videoId,
        ChannelReader<FrameDetectionResult> reader,
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

        return new VideoAnalysisPipelineResult(savedFrameCount, lastReportedProgress);

        async Task SaveBatchAsync()
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var frames = batch
                .Select(result => DetectedObjectFactory.CreateAnalyzedFrame(videoId, result))
                .ToList();

            await db.AnalyzedFrames.AddRangeAsync(frames, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();

            savedFrameCount += batch.Count;
            lastReportedProgress = await progressReporter.ReportRangeAsync(
                videoId,
                videoId,
                savedFrameCount,
                totalFramesToAnalyze,
                VideoAnalysisProgressRanges.DetectionStart,
                VideoAnalysisProgressRanges.DetectionEnd,
                lastReportedProgress,
                $"Saved {savedFrameCount} of {totalFramesToAnalyze} analyzed frames...",
                cancellationToken);

            batch.Clear();
        }
    }

    private static string ConvertMatToBase64Jpeg(Mat frame)
    {
        Cv2.ImEncode(".jpg", frame, out var imageBytes);
        return Convert.ToBase64String(imageBytes);
    }

    private sealed record FrameDetectionJob(
        int FrameIndex,
        double TimeSeconds,
        string ImageBase64);
}

internal sealed record VideoAnalysisPipelineResult(
    int SavedFrameCount,
    int LastReportedProgress);
