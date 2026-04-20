using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using OpenCvSharp;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.ObjectDetectionClient;

namespace VideoAnonymizer.VideoProcessor.VideoAnalyzer;

public class VideoAnalysisWorker(ILogger<VideoAnalysisWorker> logger, IMessagePublisher messagePublisher, IServiceProvider serviceProvider, AnalyzerPipeline pipeline) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    protected override async Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        var objectDetectionClient = serviceProvider.CreateScope().ServiceProvider.GetService<ObjectDetectionClient.ObjectDetectionClient>()!;
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
        var video = await db.Videos.FindAsync(job.VideoId);

        var sessionId = $"{Guid.NewGuid()}";

        var frameIndex = 0;

        var frameStep = Math.Max(1, (int)Math.Round(fps * job.CaptureIntervalMs * 0.001));
        
        var totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
        var lastFrameIndex = Math.Max(0, totalFrames - 1);

        var analyzedFrames = new List<AnalyzedFrame>();

        var processedFrameCount = await pipeline.Run(
            capture,
            fps,
            frameStep,
            lastFrameIndex,
            video,
            dbFactory,
            objectDetectionClient,
            sessionId,
            stoppingToken);

        
        await objectDetectionClient.Cleanup_tracker_endpoint_cleanupTracker_postAsync(sessionId);

        stoppingToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Finished processing video {VideoPath}. Analyzed frames: {ProcessedFrameCount}",
            job.Path,
            processedFrameCount);

        video.AnalyzedFrames = analyzedFrames;
        await db.SaveChangesAsync();

        await messagePublisher.PublishAsync(RabbitMQConstants.RoutingKeys.Analyzed, new AnalyzedVideo(job.VideoId, DateTime.Now), stoppingToken);
    }
}
