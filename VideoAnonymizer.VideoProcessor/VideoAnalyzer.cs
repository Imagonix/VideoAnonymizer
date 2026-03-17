using MassTransit;
using VideoAnonymizer.Contracts;

namespace VideoAnonymizer.VideoProcessor;

public class VideoAnalyzer(ILogger<VideoAnalyzer> logger, IServiceProvider serviceProvider) : SingleJobQueingWorker<AnalyzeVideo>(logger)
{
    protected override async Task HandleJob(AnalyzeVideo job, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
        var publishEndpoint = serviceProvider.CreateScope().ServiceProvider.GetService<IPublishEndpoint>();
        await publishEndpoint.Publish(new AnalyzedVideo(job.jobId, DateTime.Now));
    }
}
