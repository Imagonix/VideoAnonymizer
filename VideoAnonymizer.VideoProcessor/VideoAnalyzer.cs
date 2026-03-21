using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnalyzer(ILogger<VideoAnalyzer> logger, IServiceProvider serviceProvider) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    protected override async Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        var objectDetectionClient = serviceProvider.CreateScope().ServiceProvider.GetService<ObjectDetectionClient.ObjectDetectionClient>();
        if (string.IsNullOrWhiteSpace(job.Path))
            throw new ArgumentException("Video path is empty.", nameof(job.Path));

        if (!File.Exists(job.Path))
            throw new FileNotFoundException("Video file not found.", job.Path);

        using var capture = new VideoCapture(job.Path);

        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open video: {job.Path}");

        var fps = capture.Fps;
        if (fps <= 0 || double.IsNaN(fps))
            fps = 25;

        logger.LogInformation("Processing video {VideoPath} with FPS {Fps}", job.Path, fps);

        var dbFactory = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
        using var db = await dbFactory.CreateDbContextAsync();
        var video = await db.Videos.FindAsync(job.videoId);

        using var frame = new Mat();

        var frameIndex = 0;
        var processedFrameCount = 0;

        // Example: only analyze 1 frame per second
        var frameStep = Math.Max(1, (int)Math.Round(fps));

        var analyzedFrames = new List<AnalyzedFrame>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var success = capture.Read(frame);
            if (!success || frame.Empty())
                break;

            if (frameIndex % frameStep != 0)
            {
                frameIndex++;
                continue;
            }

            var analyzedFrame = new AnalyzedFrame
            {
                TimeSeconds = frameIndex / fps,
                VideoId = video.Id
            };
            analyzedFrames.Add(analyzedFrame);
            await db.AddAsync(analyzedFrame);

            var imageBase64 = ConvertMatToBase64Jpeg(frame);

            var detections = await objectDetectionClient.DetectObjects_detectObjects_postAsync(
                new DetectRequest
                {
                    ImageBase64 = imageBase64
                },
                stoppingToken);

            var detectedObjects = detections.Select(detection => new DetectedObject()
            {
                Selected = true,
                AnalyzedFrameId = analyzedFrame.Id,
                Height = detection.Height,
                Width = detection.Width,
                X = detection.X,
                Y = detection.Y,
                ClassName = detection.ClassName,
                Confidence = detection.Confidence,
            }).ToList();
            await db.AddRangeAsync(detectedObjects);


            logger.LogInformation(
                "Frame {FrameIndex} at {Timestamp} processed. Detections: {Count}",
                frameIndex,
                TimeSpan.FromSeconds(frameIndex / fps),
                detections?.Count ?? 0);

            processedFrameCount++;
            frameIndex++;
        }

        stoppingToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Finished processing video {VideoPath}. Analyzed frames: {ProcessedFrameCount}",
            job.Path,
            processedFrameCount);
        video.AnalyzedFrames = analyzedFrames;
        await db.SaveChangesAsync();

        var publishEndpoint = serviceProvider.CreateScope().ServiceProvider.GetService<IPublishEndpoint>();
        await publishEndpoint.Publish(new AnalyzedVideo(job.videoId, DateTime.Now));
    }

    private static string ConvertMatToBase64Jpeg(Mat frame)
    {
        Cv2.ImEncode(".jpg", frame, out var imageBytes);
        return Convert.ToBase64String(imageBytes);
    }
}
