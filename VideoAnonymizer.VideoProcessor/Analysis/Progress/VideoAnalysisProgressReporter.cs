using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor.Analysis.Progress;

internal sealed class VideoAnalysisProgressReporter(IMessagePublisher messagePublisher)
{
    public async Task<int> ReportAsync(
        Guid jobId,
        Guid? videoId,
        int progressPercent,
        int lastReportedProgress,
        string status,
        CancellationToken cancellationToken)
    {
        progressPercent = Math.Clamp(progressPercent, 0, 100);

        if (progressPercent == lastReportedProgress)
            return lastReportedProgress;

        await PublishProgressAsync(jobId, videoId, progressPercent, status, cancellationToken);
        return progressPercent;
    }

    public async Task<int> ReportRangeAsync(
        Guid jobId,
        Guid? videoId,
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

        await PublishProgressAsync(jobId, videoId, progressPercent, status, cancellationToken);
        return progressPercent;
    }

    private Task PublishProgressAsync(
        Guid jobId,
        Guid? videoId,
        int? progressPercent,
        string status,
        CancellationToken cancellationToken)
    {
        return messagePublisher.PublishAsync(
            RabbitMQConstants.RoutingKeys.Progress,
            new VideoProcessingProgress(
                jobId,
                videoId,
                VideoAnalysisProgressRanges.Operation,
                progressPercent,
                status,
                DateTimeOffset.UtcNow),
            cancellationToken);
    }
}
