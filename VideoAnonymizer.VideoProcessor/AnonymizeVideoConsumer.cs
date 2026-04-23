using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor;

internal sealed class AnonymizeVideoConsumer : MessageConsumer<AnonymizeVideo>
{
    private readonly VideoAnonymizer _worker;

    protected override string Queue => RabbitMQConstants.Queues.Anonymize;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.Anonymize;

    public AnonymizeVideoConsumer(
        VideoAnonymizer worker,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<AnonymizeVideoConsumer> logger)
        : base(connectionFactory, options, logger)
    {
        _worker = worker;
    }

    public override async Task Consume(AnonymizeVideo message, CancellationToken cancellationToken)
    {
        await _worker.EnqueueAsync(message);
    }
}