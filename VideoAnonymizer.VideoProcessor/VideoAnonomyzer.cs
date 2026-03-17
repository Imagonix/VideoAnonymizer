using MassTransit;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnonomyzer(ILogger<VideoAnonomyzer> logger, IPublishEndpoint publishEndpoint) : SingleJobQueingWorker<AnonomyzeVideo>(logger)
{
    protected override Task HandleJob(AnonomyzeVideo job, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
        publishEndpoint.Publish(new AnonomyzedVideo(job.jobId, DateTime.Now));
    }
}
