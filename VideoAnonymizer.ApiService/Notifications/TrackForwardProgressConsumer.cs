using Microsoft.Extensions.Options;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.ApiService.Notifications;

public class TrackForwardProgressConsumer : MessageConsumer<TrackForwardProgress>
{
    private readonly IMessageHandler<TrackForwardProgress> _handler;

    public TrackForwardProgressConsumer(
        IMessageHandler<TrackForwardProgress> handler,
        IRabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<MessageConsumer<TrackForwardProgress>> logger)
        : base(connectionFactory, options, logger)
    {
        _handler = handler;
    }

    protected override string Queue => RabbitMQConstants.Queues.TrackForwardProgress;
    protected override string RoutingKey => RabbitMQConstants.RoutingKeys.TrackForwardProgress;

    public override async Task Consume(TrackForwardProgress message, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(message, cancellationToken);
    }
}
