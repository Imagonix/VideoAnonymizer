using MassTransit;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnalyzer(ILogger<VideoAnalyzer> logger, IPublishEndpoint publishEndpoint) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    protected override Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
        publishEndpoint.Publish(new AnalyzedVideo(job.jobId, DateTime.Now));
    }
}
