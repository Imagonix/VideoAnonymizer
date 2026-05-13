using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database;
using VideoAnonymizer.VideoProcessor;

namespace VideoAnonymizer.VideoProcessor.Analysis;

internal sealed class VideoAnalyzer(
    ILogger<VideoAnalyzer> logger,
    IMessagePublisher messagePublisher,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    VideoAnalysisProgressReporter progressReporter,
    VideoAnalysisPipeline videoAnalysisPipeline,
    ObjectTrackingPipeline objectTrackingPipeline) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    protected override async Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        var lastReportedProgress = -1;
        lastReportedProgress = await progressReporter.ReportAsync(
            job.VideoId,
            job.VideoId,
            0,
            lastReportedProgress,
            "Loading video...",
            stoppingToken);

        if (string.IsNullOrWhiteSpace(job.Path))
            throw new ArgumentException("Video path is empty.", nameof(job.Path));
        if (!File.Exists(job.Path))
            throw new FileNotFoundException("Video file not found.", job.Path);

        await EnsureVideoExistsAsync(job.VideoId, stoppingToken);

        var videoMetadata = VideoAnalysisMetadata.Read(job.Path, job.CaptureIntervalMs);

        lastReportedProgress = await progressReporter.ReportAsync(
            job.VideoId,
            job.VideoId,
            VideoAnalysisProgressRanges.DetectionStart,
            lastReportedProgress,
            "Preparing frame analysis...",
            stoppingToken);

        var detectionResult = await videoAnalysisPipeline.RunAsync(
            job,
            videoMetadata,
            lastReportedProgress,
            stoppingToken);
        lastReportedProgress = detectionResult.LastReportedProgress;

        stoppingToken.ThrowIfCancellationRequested();

        lastReportedProgress = await progressReporter.ReportAsync(
            job.VideoId,
            job.VideoId,
            VideoAnalysisProgressRanges.TrackingStart,
            lastReportedProgress,
            "Assigning object tracks...",
            stoppingToken);

        var trackingResult = await objectTrackingPipeline.RunAsync(
            job.VideoId,
            job.Path,
            videoMetadata.Fps,
            videoMetadata.TotalFramesToAnalyze,
            lastReportedProgress,
            stoppingToken);
        lastReportedProgress = trackingResult.LastReportedProgress;

        stoppingToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Finished processing video {VideoPath}. Analyzed frames: {ProcessedFrameCount}, tracked frames: {TrackedFrameCount}",
            job.Path,
            detectionResult.SavedFrameCount,
            trackingResult.TrackedFrameCount);

        lastReportedProgress = await progressReporter.ReportAsync(
            job.VideoId,
            job.VideoId,
            VideoAnalysisProgressRanges.TrackingEnd,
            lastReportedProgress,
            "Finalizing analysis...",
            stoppingToken);

        await progressReporter.ReportAsync(
            job.VideoId,
            job.VideoId,
            100,
            lastReportedProgress,
            "Analysis saved.",
            stoppingToken);
        await messagePublisher.PublishAsync(RabbitMQConstants.RoutingKeys.Analyzed, new AnalyzedVideo(job.VideoId, DateTime.Now), stoppingToken);
    }

    private async Task EnsureVideoExistsAsync(Guid videoId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var exists = await db.Videos.AnyAsync(video => video.Id == videoId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException($"Video {videoId} was not found.");
    }
}
