using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using VideoAnonymizer.Database;
using VideoAnonymizer.VideoProcessor.Analysis.Progress;
using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Appearance;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking;

internal sealed class ObjectTrackingPipeline(
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    IConfiguration configuration,
    VideoAnalysisProgressReporter progressReporter)
{
    private static readonly TimeSpan FramePollingDelay = TimeSpan.FromMilliseconds(200);

    public async Task<ObjectTrackingPipelineResult> RunAsync(
        Guid videoId,
        string videoPath,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        ConsecutiveFrameTracker consecutiveFrames,
        Task detectionTask,
        CancellationToken cancellationToken)
    {
        var options = ObjectTrackingPipelineOptions.FromConfiguration(configuration);
        return await TrackPersistedFramesByAppearanceAsync(
            videoId,
            videoPath,
            totalFramesToAnalyze,
            options,
            lastReportedProgress,
            consecutiveFrames,
            detectionTask,
            cancellationToken);
    }

    private async Task<ObjectTrackingPipelineResult> TrackPersistedFramesByAppearanceAsync(
        Guid videoId,
        string videoPath,
        int totalFramesToAnalyze,
        ObjectTrackingPipelineOptions options,
        int lastReportedProgress,
        ConsecutiveFrameTracker consecutiveFrames,
        Task detectionTask,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException("Video file not found for appearance tracking.", videoPath);

        var frameQueue = Channel.CreateBounded<PersistedFrameToTrack>(
            new BoundedChannelOptions(options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

        var featureJobs = Channel.CreateBounded<FeatureExtractionFrameJob>(
            new BoundedChannelOptions(options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });

        var trackingResults = Channel.CreateBounded<TrackingFrameResult>(
            new BoundedChannelOptions(options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        using var pipelineCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = pipelineCts.Token;
        var tracker = new AppearanceObjectTracker(options.AppearanceTracking);

        var feederTask = Task.Run(
            () => FeedPersistedFramesAsync(
                videoId,
                options,
                consecutiveFrames,
                detectionTask,
                frameQueue.Writer,
                pipelineToken),
            pipelineToken);

        var readerTask = Task.Run(
            () => ReadVideoFramesForwardAsync(
                videoPath,
                frameQueue.Reader,
                featureJobs.Writer,
                options,
                pipelineToken),
            pipelineToken);

        var workerTasks = Enumerable.Range(0, options.FeatureWorkerCount)
            .Select(_ => Task.Run(
                () => ExtractFeaturesFromFramesAsync(
                    featureJobs.Reader,
                    trackingResults.Writer,
                    pipelineToken),
                pipelineToken))
            .ToArray();

        var workersCompletionTask = CompleteResultWriterWhenWorkersCompleteAsync(workerTasks, trackingResults.Writer);

        var collectorTask = CollectTrackAndSaveAsync(
            videoId,
            trackingResults.Reader,
            tracker,
            options,
            totalFramesToAnalyze,
            lastReportedProgress,
            detectionTask,
            pipelineToken);

        CancelPipelineOnFault(feederTask, pipelineCts);
        CancelPipelineOnFault(readerTask, pipelineCts);
        CancelPipelineOnFault(workersCompletionTask, pipelineCts);
        CancelPipelineOnFault(collectorTask, pipelineCts);

        try
        {
            await Task.WhenAll(feederTask, readerTask, workersCompletionTask, collectorTask);
            return collectorTask.Result;
        }
        catch
        {
            pipelineCts.Cancel();
            frameQueue.Writer.TryComplete();
            featureJobs.Writer.TryComplete();
            trackingResults.Writer.TryComplete();
            throw;
        }
    }

    private async Task FeedPersistedFramesAsync(
        Guid videoId,
        ObjectTrackingPipelineOptions options,
        ConsecutiveFrameTracker consecutiveFrames,
        Task detectionTask,
        ChannelWriter<PersistedFrameToTrack> writer,
        CancellationToken cancellationToken)
    {
        var lastQueuedFrameIndex = -1;
        var nextSequence = 0L;

        try
        {
            while (true)
            {
                var maxFrameIndex = detectionTask.IsCompleted
                    ? int.MaxValue
                    : consecutiveFrames.MaxConsecutive;

                if (maxFrameIndex <= lastQueuedFrameIndex)
                {
                    await Task.Delay(FramePollingDelay, cancellationToken);
                    continue;
                }

                var frames = await LoadFramesToTrackAsync(
                    videoId,
                    lastQueuedFrameIndex,
                    maxFrameIndex,
                    options.SaveBatchSize,
                    cancellationToken);

                if (frames.Count == 0)
                {
                    if (detectionTask.IsCompleted)
                        break;

                    await Task.Delay(FramePollingDelay, cancellationToken);
                    continue;
                }

                foreach (var frame in frames)
                {
                    var frameToTrack = CreatePersistedFrameToTrack(frame, nextSequence++);
                    await writer.WriteAsync(frameToTrack, cancellationToken);
                    lastQueuedFrameIndex = frame.FrameIndex;
                }
            }

            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
    }

    private async Task<List<AnalyzedFrame>> LoadFramesToTrackAsync(
        Guid videoId,
        int lastQueuedFrameIndex,
        int maxFrameIndex,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.AnalyzedFrames
            .AsNoTracking()
            .Include(frame => frame.DetectedObjects)
            .Where(frame =>
                frame.VideoId == videoId &&
                frame.FrameIndex > lastQueuedFrameIndex &&
                frame.FrameIndex <= maxFrameIndex)
            .OrderBy(frame => frame.FrameIndex)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    private static PersistedFrameToTrack CreatePersistedFrameToTrack(
        AnalyzedFrame frame,
        long sequence)
    {
        var detectedObjects = frame.DetectedObjects
            .OrderBy(obj => obj.X)
            .ThenBy(obj => obj.Y)
            .Select(CreatePersistedDetectedObject)
            .ToList();

        return new PersistedFrameToTrack(
            sequence,
            frame.FrameIndex,
            frame.TimeSeconds,
            detectedObjects);
    }

    private static PersistedDetectedObject CreatePersistedDetectedObject(DetectedObject detectedObject)
    {
        var className = string.IsNullOrWhiteSpace(detectedObject.ClassName)
            ? "face"
            : detectedObject.ClassName.Trim().ToLowerInvariant();

        return new PersistedDetectedObject(
            detectedObject.Id,
            className,
            detectedObject.Confidence,
            new TrackBox(
                detectedObject.X,
                detectedObject.Y,
                detectedObject.Width,
                detectedObject.Height));
    }

    private static async Task ReadVideoFramesForwardAsync(
        string videoPath,
        ChannelReader<PersistedFrameToTrack> reader,
        ChannelWriter<FeatureExtractionFrameJob> writer,
        ObjectTrackingPipelineOptions options,
        CancellationToken cancellationToken)
    {
        Mat? currentFrame = null;
        var currentFrameIndex = -1;

        try
        {
            using var capture = new VideoCapture(videoPath);
            if (!capture.IsOpened())
                throw new InvalidOperationException($"Could not open video for appearance tracking: {videoPath}");

            await foreach (var frame in reader.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (frame.FrameIndex < currentFrameIndex)
                {
                    throw new InvalidOperationException(
                        $"Tracking frame order moved backwards from {currentFrameIndex} to {frame.FrameIndex}.");
                }

                var frameAvailable = ReadForwardToFrame(
                    capture,
                    frame.FrameIndex,
                    ref currentFrameIndex,
                    ref currentFrame);
                var job = CreateFeatureExtractionFrameJob(
                    frame,
                    frameAvailable ? currentFrame : null,
                    options.AppearanceTracking.CropPaddingPercent);

                try
                {
                    await writer.WriteAsync(job, cancellationToken);
                }
                catch
                {
                    job.Dispose();
                    throw;
                }
            }

            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
        finally
        {
            currentFrame?.Dispose();
        }
    }

    private static bool ReadForwardToFrame(
        VideoCapture capture,
        int targetFrameIndex,
        ref int currentFrameIndex,
        ref Mat? currentFrame)
    {
        while (currentFrameIndex < targetFrameIndex)
        {
            currentFrame?.Dispose();
            currentFrame = new Mat();

            if (!capture.Read(currentFrame) || currentFrame.Empty())
            {
                currentFrame.Dispose();
                currentFrame = null;
                return false;
            }

            currentFrameIndex++;
        }

        return currentFrame is not null && !currentFrame.Empty();
    }

    private static FeatureExtractionFrameJob CreateFeatureExtractionFrameJob(
        PersistedFrameToTrack frame,
        Mat? sourceFrame,
        double cropPaddingPercent)
    {
        var detections = frame.DetectedObjects
            .Select(detectedObject => new FeatureExtractionDetectedObject(
                detectedObject.Id,
                detectedObject.ClassName,
                detectedObject.Confidence,
                detectedObject.Box,
                sourceFrame is null
                    ? null
                    : AppearanceFeatureExtractor.CreatePaddedCrop(
                        sourceFrame,
                        detectedObject.Box,
                        cropPaddingPercent)))
            .ToList();

        return new FeatureExtractionFrameJob(
            frame.Sequence,
            frame.FrameIndex,
            frame.TimeSeconds,
            detections);
    }

    private static async Task ExtractFeaturesFromFramesAsync(
        ChannelReader<FeatureExtractionFrameJob> reader,
        ChannelWriter<TrackingFrameResult> writer,
        CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken))
        {
            while (reader.TryRead(out var job))
            {
                try
                {
                    var detections = job.DetectedObjects
                        .Select(CreateAppearanceDetection)
                        .ToList();

                    await writer.WriteAsync(
                        new TrackingFrameResult(
                            job.Sequence,
                            job.FrameIndex,
                            job.TimeSeconds,
                            detections),
                        cancellationToken);
                }
                finally
                {
                    job.Dispose();
                }
            }
        }
    }

    private static AppearanceDetection CreateAppearanceDetection(
        FeatureExtractionDetectedObject detectedObject)
    {
        return new AppearanceDetection(
            detectedObject.Id,
            detectedObject.ClassName,
            detectedObject.Confidence,
            detectedObject.Box,
            detectedObject.Crop is null
                ? null
                : AppearanceFeatureExtractor.ExtractFromCrop(detectedObject.Crop));
    }

    private async Task<ObjectTrackingPipelineResult> CollectTrackAndSaveAsync(
        Guid videoId,
        ChannelReader<TrackingFrameResult> reader,
        AppearanceObjectTracker tracker,
        ObjectTrackingPipelineOptions options,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        Task detectionTask,
        CancellationToken cancellationToken)
    {
        var buffer = new Dictionary<long, TrackingFrameResult>();
        var pendingAssignments = new Dictionary<Guid, int>();
        var nextSequence = 0L;
        var trackedFrameCount = 0;
        var framesSinceSave = 0;

        await foreach (var result in reader.ReadAllAsync(cancellationToken))
        {
            buffer[result.Sequence] = result;

            while (buffer.TryGetValue(nextSequence, out var next))
            {
                TrackFrame(next, tracker, pendingAssignments);

                buffer.Remove(nextSequence);
                nextSequence++;
                trackedFrameCount++;
                framesSinceSave++;

                if (framesSinceSave >= options.SaveBatchSize)
                    await SaveAndReportAsync();
            }
        }

        if (buffer.Count > 0)
            throw new InvalidOperationException("Tracking results completed before all earlier frames were processed.");

        if (framesSinceSave > 0)
            await SaveAndReportAsync();

        return new ObjectTrackingPipelineResult(trackedFrameCount, lastReportedProgress);

        async Task SaveAndReportAsync()
        {
            await SaveAssignmentsAsync(videoId, pendingAssignments, cancellationToken);
            pendingAssignments.Clear();
            framesSinceSave = 0;

            if (detectionTask.IsCompleted)
            {
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
        }
    }

    private static void TrackFrame(
        TrackingFrameResult frame,
        AppearanceObjectTracker tracker,
        Dictionary<Guid, int> pendingAssignments)
    {
        var assignments = tracker.AssignTracks(
            frame.FrameIndex,
            frame.TimeSeconds,
            frame.Detections);

        foreach (var detection in frame.Detections)
        {
            if (assignments.TryGetValue(detection.DetectedObjectId, out var trackId))
                pendingAssignments[detection.DetectedObjectId] = trackId;
        }
    }

    private async Task SaveAssignmentsAsync(
        Guid videoId,
        Dictionary<Guid, int> assignments,
        CancellationToken cancellationToken)
    {
        if (assignments.Count == 0)
            return;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var objectIds = assignments.Keys.ToList();
        var objects = await db.DetectedObjects
            .Where(obj => objectIds.Contains(obj.Id) && obj.AnalyzedFrame.VideoId == videoId)
            .ToListAsync(cancellationToken);

        foreach (var obj in objects)
        {
            if (assignments.TryGetValue(obj.Id, out var trackId))
                obj.TrackId = trackId;
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static async Task CompleteResultWriterWhenWorkersCompleteAsync(
        Task[] workerTasks,
        ChannelWriter<TrackingFrameResult> writer)
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

    private static void CancelPipelineOnFault(Task task, CancellationTokenSource pipelineCts)
    {
        _ = task.ContinueWith(
            _ => pipelineCts.Cancel(),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private sealed record PersistedFrameToTrack(
        long Sequence,
        int FrameIndex,
        double TimeSeconds,
        IReadOnlyList<PersistedDetectedObject> DetectedObjects);

    private sealed record PersistedDetectedObject(
        Guid Id,
        string ClassName,
        double Confidence,
        TrackBox Box);

    private sealed class FeatureExtractionFrameJob(
        long sequence,
        int frameIndex,
        double timeSeconds,
        IReadOnlyList<FeatureExtractionDetectedObject> detectedObjects) : IDisposable
    {
        public long Sequence { get; } = sequence;
        public int FrameIndex { get; } = frameIndex;
        public double TimeSeconds { get; } = timeSeconds;
        public IReadOnlyList<FeatureExtractionDetectedObject> DetectedObjects { get; } = detectedObjects;

        public void Dispose()
        {
            foreach (var detectedObject in DetectedObjects)
                detectedObject.Crop?.Dispose();
        }
    }

    private sealed record FeatureExtractionDetectedObject(
        Guid Id,
        string ClassName,
        double Confidence,
        TrackBox Box,
        Mat? Crop);

    private sealed record TrackingFrameResult(
        long Sequence,
        int FrameIndex,
        double TimeSeconds,
        IReadOnlyList<AppearanceDetection> Detections);
}
