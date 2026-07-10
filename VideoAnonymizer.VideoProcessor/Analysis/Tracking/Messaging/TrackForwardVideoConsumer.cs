using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.VideoProcessor.Analysis.Tracking.Messaging;

internal sealed class TrackForwardVideoConsumer : MessageConsumer<TrackForwardJob>
{
    private readonly IMessageHandler<TrackForwardJob> _handler;

    protected override string Queue => RabbitMQConstants.Queues.TrackForward;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.TrackForward;

    public TrackForwardVideoConsumer(
        IMessageHandler<TrackForwardJob> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<TrackForwardVideoConsumer> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    public override async Task Consume(TrackForwardJob message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
