using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.VideoProcessor.VideoAnalyzer;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class AnalyzeVideoConsumer : MessageConsumer<AnalyzeVideo>
{
    private readonly VideoAnalysisWorker _worker;

    protected override string Queue => RabbitMQConstants.Queues.VideoProcessing;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Analyze;

    public AnalyzeVideoConsumer(
        VideoAnalysisWorker worker,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<AnalyzeVideoConsumer> logger)
        : base(connectionFactory, options, logger)
    {
        _worker = worker;
    }

    public override async Task Consume(AnalyzeVideo message, CancellationToken cancellationToken)
    {
        await _worker.EnqueueAsync(message);
    }
}