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
    public async Task<ObjectTrackingPipelineResult> RunAsync(
        Guid videoId,
        string videoPath,
        int totalFramesToAnalyze,
        int lastReportedProgress,
        CancellationToken cancellationToken)
    {
        var options = ObjectTrackingPipelineOptions.FromConfiguration(configuration);
        return await TrackPersistedFramesByAppearanceAsync(
            videoId,
            videoPath,
            totalFramesToAnalyze,
            options,
            lastReportedProgress,
            cancellationToken);
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

            var objectLookup = frames
                .SelectMany(f => f.DetectedObjects)
                .ToDictionary(o => o.Id);

            var extractionJobs = Channel.CreateBounded<(AnalyzedFrame Frame, Mat Mat)>(
                new BoundedChannelOptions(50)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = true
                });

            var extractionResults = Channel.CreateBounded<(int FrameIndex, double TimeSeconds, IReadOnlyList<AppearanceDetection> Detections)>(
                new BoundedChannelOptions(50)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });

            using var pipelineCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var pipelineToken = pipelineCts.Token;

            var schedulerTask = Task.Run(
                () => ScheduleFrameDecodingAsync(capture, frames, extractionJobs.Writer, pipelineToken),
                pipelineToken);

            var workerTasks = Enumerable.Range(0, 4)
                .Select(_ => Task.Run(
                    () => ExtractFeaturesFromFramesAsync(extractionJobs.Reader, extractionResults.Writer, options, pipelineToken),
                    pipelineToken))
                .ToArray();

            var collectorTask = CollectAndTrackAsync(
                extractionResults.Reader, tracker, frames, objectLookup, pipelineToken);

            var workersCompletionTask = CompleteResultWriterWhenWorkersCompleteAsync(workerTasks, extractionResults.Writer);

            CancelPipelineOnFault(schedulerTask, pipelineCts);
            CancelPipelineOnFault(workersCompletionTask, pipelineCts);
            CancelPipelineOnFault(collectorTask, pipelineCts);

            try
            {
                await Task.WhenAll(schedulerTask, workersCompletionTask, collectorTask);
                trackedFrameCount += collectorTask.Result;
                lastFrameIndex = frames[^1].FrameIndex;

                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
            catch
            {
                pipelineCts.Cancel();
                extractionJobs.Writer.TryComplete();
                extractionResults.Writer.TryComplete();
                throw;
            }

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

    private static async Task ScheduleFrameDecodingAsync(
        VideoCapture capture,
        List<AnalyzedFrame> frames,
        ChannelWriter<(AnalyzedFrame Frame, Mat Mat)> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var frame in frames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mat = ReadFrame(capture, frame.FrameIndex);
                await writer.WriteAsync((frame, mat), cancellationToken);
            }

            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
    }

    private static async Task ExtractFeaturesFromFramesAsync(
        ChannelReader<(AnalyzedFrame Frame, Mat Mat)> reader,
        ChannelWriter<(int FrameIndex, double TimeSeconds, IReadOnlyList<AppearanceDetection> Detections)> writer,
        ObjectTrackingPipelineOptions options,
        CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken))
        {
            while (reader.TryRead(out var job))
            {
                var (frame, mat) = job;
                try
                {
                    var detections = frame.DetectedObjects
                        .OrderBy(obj => obj.X)
                        .ThenBy(obj => obj.Y)
                        .Select(obj => CreateAppearanceDetection(
                            obj,
                            mat,
                            options.AppearanceTracking.CropPaddingPercent))
                        .ToList();
                    await writer.WriteAsync((frame.FrameIndex, frame.TimeSeconds, detections), cancellationToken);
                }
                finally
                {
                    mat.Dispose();
                }
            }
        }
    }

    private static async Task<int> CollectAndTrackAsync(
        ChannelReader<(int FrameIndex, double TimeSeconds, IReadOnlyList<AppearanceDetection> Detections)> reader,
        AppearanceObjectTracker tracker,
        List<AnalyzedFrame> frames,
        Dictionary<Guid, DetectedObject> objectLookup,
        CancellationToken cancellationToken)
    {
        var buffer = new Dictionary<int, (double TimeSeconds, IReadOnlyList<AppearanceDetection> Detections)>();
        var processedCount = 0;

        await foreach (var result in reader.ReadAllAsync(cancellationToken))
        {
            buffer[result.FrameIndex] = (result.TimeSeconds, result.Detections);

            while (processedCount < frames.Count && buffer.TryGetValue(frames[processedCount].FrameIndex, out var next))
            {
                var assignments = tracker.AssignTracks(frames[processedCount].FrameIndex, next.TimeSeconds, next.Detections);

                foreach (var detection in next.Detections)
                {
                    if (objectLookup.TryGetValue(detection.DetectedObjectId, out var obj)
                        && assignments.TryGetValue(detection.DetectedObjectId, out var trackId))
                    {
                        obj.TrackId = trackId;
                    }
                }

                buffer.Remove(frames[processedCount].FrameIndex);
                processedCount++;
            }
        }

        return processedCount;
    }

    private static async Task CompleteResultWriterWhenWorkersCompleteAsync(
        Task[] workerTasks,
        ChannelWriter<(int FrameIndex, double TimeSeconds, IReadOnlyList<AppearanceDetection> Detections)> writer)
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
}
